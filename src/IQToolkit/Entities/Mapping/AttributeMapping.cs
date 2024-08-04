// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Entities.Mapping
{
    using Utils;

    /// <summary>
    /// An <see cref="EntityMapping"/> that uses attributes on entity or context types
    /// to define the mapping.
    /// </summary>
    public class AttributeMapping : StandardMapping
    {
        private ImmutableDictionary<string, IReadOnlyList<MappingAttribute>> _idToAttributes;
        private ImmutableDictionary<MemberInfo, string> _contextMemberEntityIds;

        /// <summary>
        /// Constructs a new instance of a <see cref="AttributeMapping"/> where mapping attributes are
        /// discovered on a context class (instead of from the entity types).
        /// </summary>
        /// <param name="contextType">The type of the context class that encodes the mapping attributes.
        /// If not spefied, the mapping attributes are assumed to be defined on the individual entity types.</param>
        public AttributeMapping(Type? contextType = null)
            : base(contextType)
        {
            _idToAttributes = ImmutableDictionary<string, IReadOnlyList<MappingAttribute>>.Empty;
            _contextMemberEntityIds = ImmutableDictionary<MemberInfo, string>.Empty;

            if (contextType != null)
                this.InitializeContextMembers();
        }

        /// <summary>
        /// Gets the entity id for the entity type.
        /// </summary>
        protected override string GetEntityId(Type entityType)
        {
            if (this.ContextType != null
                && this.TryGetContextMember(entityType, out var contextMember))
            {
                return GetEntityId(contextMember);
            }
            else
            {
                // look for entity id specified on type itself
                var attr = entityType.GetCustomAttribute<EntityAttribute>();
                if (attr != null && attr.Id != null)
                    return attr.Id;
            }

            // use the entity type name as the entity id
            return entityType.Name;
        }

        /// <summary>
        /// Gets the entity id for the context member.
        /// </summary>
        protected override string GetEntityId(MemberInfo contextMember)
        {
            if (!_contextMemberEntityIds.TryGetValue(contextMember, out var id))
            {
                var entityAttr = contextMember.GetCustomAttribute<EntityAttribute>();

                var tmp = (entityAttr != null && !string.IsNullOrEmpty(entityAttr.Id))
                    ? entityAttr.Id
                    : id = base.GetEntityId(contextMember);

                id = ImmutableInterlocked.GetOrAdd(ref _contextMemberEntityIds, contextMember, tmp);
            }

            return id;
        }

        /// <summary>
        /// Create a <see cref="MappedEntity"/> from attributes on the entity or context type.
        /// </summary>
        protected override MappedEntity CreateEntity(
            Type entityType, string entityId, ParentEntity? parent)
        {
            return new StandardEntity(
                this,
                entityId,
                entityType,
                GetEntityRuntimeType(entityType, entityId, parent),
                me => CreateEntityTables(me, parent),
                me => CreateMembers(me, parent)
                );
        }

        protected virtual Type GetEntityRuntimeType(
            Type entityType, string entityId, ParentEntity? parent)
        {
            var attr = this.GetMappingAttributes(entityType, entityId, parent)
                .OfType<EntityAttribute>()
                .FirstOrDefault();

            return attr != null && attr.RuntimeType != null && parent == null
                ? attr.RuntimeType
                : entityType;
        }

        protected virtual IReadOnlyList<MappedTable> CreateEntityTables(
            MappedEntity entity, ParentEntity? parent)
        {
            var attrs = GetMappingAttributes(entity.Type, entity.EntityId, parent)
                .OfType<TableBaseAttribute>();

            return attrs
                .Select(ta => GetOrCreateTable(entity, ta.Id ?? entity.EntityId, parent))
                .ToReadOnly();
        }

        protected virtual IReadOnlyList<MappedMember> CreateMembers(
            MappedEntity entity, 
            ParentEntity? parent)
        {
            var attrs = GetMappingAttributes(entity.Type, entity.EntityId, parent)
                .OfType<MemberAttribute>()
                .OrderBy(ma => ma.Member)
                .ToReadOnly();

            var mappedMembers = new List<MappedMember>();

            foreach (var attr in attrs)
            {
                if (string.IsNullOrEmpty(attr.Member))
                    continue;

                var memberName = attr.Member;
                if (entity.Type.FindDeclaredFieldOrPropertyFromPath(memberName) is { } member)
                {
                    var mappedMember = CreateMember(entity, member, attr, parent);
                    mappedMembers.Add(mappedMember);
                }
            }

            return mappedMembers.ToReadOnly();
        }

        protected virtual MappedMember CreateMember(
            MappedEntity entity, MemberInfo member, MemberAttribute attr, ParentEntity? parent)
        {
            switch (attr)
            {
                case ColumnAttribute columnAttr:
                    var columnName = !string.IsNullOrEmpty(columnAttr.Name)
                        ? columnAttr.Name
                        : member.Name;

                    var columnTable = (!string.IsNullOrEmpty(columnAttr.TableId)
                            && this.GetOrCreateTable(entity, columnAttr.TableId, parent) is { } tableInAttr)
                        ? tableInAttr
                        : entity.PrimaryTable;

                    var columnType = !string.IsNullOrEmpty(columnAttr.DbType)
                        ? columnAttr.DbType
                        : null;

                    return new StandardColumnMember(
                        entity,
                        member,
                        columnTable,
                        columnName,
                        columnType,
                        isPrimaryKey: columnAttr.IsPrimaryKey,
                        isReadOnly: columnAttr.IsReadOnly,
                        isComputed: columnAttr.IsComputed,
                        isGenerated: columnAttr.IsGenerated
                        );

                case AssociationAttribute assocAttr:
                    return new StandardAssociationMember(
                        entity,
                        member,
                        assocAttr.IsForeignKey,
                        me => GetAssociationKeyMembers(me, assocAttr),
                        me => GetAssociationRelatedEntity(me, assocAttr),
                        me => GetAssociationRelatedKeyMembers(me, assocAttr)
                        );

                case NestedEntityAttribute nestedAttr:
                    var nestedEntityId = entity.EntityId + "." + member.Name;
                    var nestedEntityType = TypeHelper.GetMemberType(member);

                    var nestedEntity = this.GetOrCreateEntity(
                        nestedEntityType,
                        nestedEntityId,
                        new ParentEntity(parent, member, entity.Type, entity.EntityId)
                        );

                    return new StandardNestedEntityMember(
                        nestedEntity,
                        member,
                        me => entity
                        );

                default:
                    throw new InvalidOperationException($"AttributeMapping: The member '{entity.Type.Name}.{member.Name}' has an unknown mapping attribute '{attr.GetType().Name}'");
            }
        }

        private MappedEntity GetAssociationRelatedEntity(MappedRelationshipMember member, AssociationAttribute attr)
        {
            var relatedEntityType = TypeHelper.GetSequenceElementType(TypeHelper.GetMemberType(member.Member));
            var entityId = !string.IsNullOrEmpty(attr.RelatedEntityId) ? attr.RelatedEntityId : this.GetEntityId(relatedEntityType);
            return this.GetReferencedEntity(relatedEntityType, entityId, "Assocation.RelatedEntityId");
        }

        private IReadOnlyList<MappedColumnMember> GetAssociationKeyMembers(
            MappedAssociationMember member,
            AssociationAttribute attr)
        {
            return !string.IsNullOrEmpty(attr.KeyMembers)
                ? this.GetReferencedColumnMembers(member.Entity, attr.KeyMembers, "Association.KeyMembers", member.Entity.Type)
                : ReadOnlyList<MappedColumnMember>.Empty;
        }

        private IReadOnlyList<MappedColumnMember> GetAssociationRelatedKeyMembers(
            MappedAssociationMember member, 
            AssociationAttribute attr)
        {
            return !string.IsNullOrEmpty(attr.RelatedKeyMembers)
                ? this.GetReferencedColumnMembers(member.RelatedEntity, attr.RelatedKeyMembers, "Association.RelatedKeyMembers", member.Entity.Type)
                : ReadOnlyList<MappedColumnMember>.Empty;
        }

        private IReadOnlyList<MappedColumnMember> GetReferencedColumnMembers(
            MappedEntity entity, string memberNames, string source, Type sourceType)
        {
            return memberNames
                .Split(_nameListSeparators)
                .Select(n => this.GetReferencedColumnMember(entity, n, source, sourceType))
                .ToReadOnly();
        }

        private MappedColumnMember GetReferencedColumnMember(
            MappedEntity entity, string memberName, string source, Type sourceType)
        {
            var mm = entity.ColumnMembers.FirstOrDefault(cm => cm.Member.Name == memberName);
            if (mm == null)
            {
                throw new InvalidOperationException($"AttributeMapping: The member '{entity.Type.Name}.{memberName}' referenced in {source} for '{sourceType.Name}' is not mapped or does not exist");
            }

            return mm;
        }

        private MappedEntity GetReferencedEntity(Type entityType, string entityId, string source)
        {
            var entity = this.GetEntity(entityType, entityId);

            if (entity == null)
            {
                throw new InvalidOperationException(string.Format("The entity '{0}' referenced in {1} of '{2}' does not exist", entityId, source, entityType.Name));
            }

            return entity;
        }

        protected override MappedTable CreateTable(
            MappedEntity entity, string tableId, ParentEntity? parent)
        {
            var attr =
                this.GetMappingAttributes(entity.Type, entity.EntityId, parent)
                .OfType<TableBaseAttribute>()
                .FirstOrDefault(ta => (ta.Id ?? entity.EntityId) == tableId);

            if (attr is ExtensionTableAttribute exAttr)
            {
                return new StandardExtensionTable(
                    entity,
                    attr.Id ?? attr.Name ?? entity.EntityId,
                    attr.Name ?? entity.EntityId,
                    () => GetExtensionRelatedTable(entity.PrimaryTable, exAttr),
                    me => GetExtensionKeyColumnNames(me, exAttr),
                    me => GetExtensionRelatedMembers(me, exAttr)
                    );
            }
            else
            {
                return new StandardTable(
                    entity,
                    attr.Id ?? attr.Name ?? entity.EntityId,
                    attr.Name ?? entity.EntityId
                    );
            }
        }

        private static readonly char[] _nameListSeparators = new char[] { ' ', ',', '|' };

        private MappedTable GetExtensionRelatedTable(
            MappedTable primaryTable, ExtensionTableAttribute attr)
        {
            if (!string.IsNullOrEmpty(attr.RelatedTableId)
                && this.GetOrCreateTable(primaryTable.Entity, attr.RelatedTableId, null) is { } table)
            {
                return table;
            }
            else
            {
                return primaryTable;
            }
        }

        private IReadOnlyList<string> GetExtensionKeyColumnNames(
            MappedExtensionTable table, ExtensionTableAttribute attr)
        {
            if (!string.IsNullOrEmpty(attr.KeyColumns))
            {
                return attr.KeyColumns.Split(_nameListSeparators).ToReadOnly();
            }
            else
            {
                return ReadOnlyList<string>.Empty;
            }
        }

        private IReadOnlyList<MappedColumnMember> GetExtensionRelatedMembers(
            MappedExtensionTable table, ExtensionTableAttribute attr)
        {
            var relatedKeyColumns = attr.RelatedKeyColumns ?? attr.KeyColumns;
            if (!string.IsNullOrEmpty(relatedKeyColumns))
            {
                return relatedKeyColumns
                    .Split(_nameListSeparators)
                    .Select(memberName => table.RelatedTable.Entity.MappedMembers.OfType<MappedColumnMember>().First(cm => cm.Member.Name == memberName))
                    .ToReadOnly();
            }
            else
            {
                return ReadOnlyList<MappedColumnMember>.Empty;
            }
        }

        #region Mapping Attributes

        protected virtual IReadOnlyList<MappingAttribute> GetMappingAttributes(
            Type entityType, string entityId, ParentEntity? parent)
        {
            if (!_idToAttributes.TryGetValue(entityId, out var attrs))
            {
                var tmp = CreateMappingAttributes(entityType, entityId, parent);
                attrs = ImmutableInterlocked.GetOrAdd(ref _idToAttributes, entityId, tmp);
            }

            return attrs;
        }

        /// <summary>
        /// Creates the list of <see cref="MappingAttribute"/> for the entity.
        /// </summary>
        protected virtual IReadOnlyList<MappingAttribute> CreateMappingAttributes(
            Type entityType, string entityId, ParentEntity? parent)
        {
            var list = new List<MappingAttribute>();

            this.GetDeclaredMappingAttributes(entityType, entityId, parent, list);

            if (parent == null)
            {
                // if no entity attribute is mentioned, add one
                if (!list.OfType<EntityAttribute>().Any())
                {
                    list.Add(new EntityAttribute { RuntimeType = entityType });
                }

                // if no table attribute is mentioned, add one based on the entity type name
                if (!list.OfType<TableAttribute>().Any())
                {
                    list.Add(new TableAttribute { Name = entityType.Name });
                }
            }

            var membersAlreadyMapped = new HashSet<string>(
                list.OfType<MemberAttribute>().Select(m => m.Member).OfType<string>());

            // look for members that are not explicitly mapped and create column mappings for them.
            var dataMembers = TypeHelper.GetDeclaredFieldsAndProperties(entityType);
                            
            foreach (var member in dataMembers)
            {
                 // member already declared explicity - don't infer an attribute
                if (membersAlreadyMapped.Contains(member.Name))
                {
                    continue;
                }

                var memberType = TypeHelper.GetMemberType(member);
                if (IsScalar(memberType))
                {
                    // members with scalar type are assumed to be columns
                    list.Add(new ColumnAttribute { Member = member.Name });
                }
                else if (!TypeHelper.IsSequenceType(memberType))
                {
                    // members with non-sequence/non-scalar types are assumed to be nested entities
                    list.Add(new NestedEntityAttribute { Member = member.Name });
                }
            }

            return list.ToReadOnly();
        }

        private static bool IsScalar(Type type)
        {
            type = TypeHelper.GetNonNullableType(type);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                    return false;
                case TypeCode.Object:
                    return
                        type == typeof(DateTimeOffset) ||
                        type == typeof(TimeSpan) ||
                        type == typeof(Guid) ||
                        type == typeof(byte[]);
                default:
                    return true;
            }
        }

        /// <summary>
        /// Gets the <see cref="MappingAttribute"/> declared by the user for the entity type.
        /// </summary>
        protected virtual void GetDeclaredMappingAttributes(
            Type entityType, string entityId, ParentEntity? parent, List<MappingAttribute> list)
        {
            if (parent != null)
            {
                this.GetNestedDeclaredMappingAttributes(parent, null, list);
            }
            else if (this.ContextType != null
                && this.TryGetContextMember(entityId, out var contextMember))
            { 
                this.GetMemberMappingAttributes(contextMember, list);
                this.RemoveMembersNotInPath(contextMember.Name, list);
            }

            this.GetTypeMappingAttributes(entityType, list);
            this.RemoveMembersNotInPath("", list);
        }

        private void GetNestedDeclaredMappingAttributes(
            ParentEntity parent, string? nestedPath, List<MappingAttribute> list)
        {
            var nestedList = new List<MappingAttribute>();

            var pathInParent = nestedPath != null ? parent.Member.Name + "." + nestedPath : parent.Member.Name;

            if (parent.Member != null)
            {
                this.GetMemberMappingAttributes(parent.Member, nestedList);
            }
            else
            {
                this.GetTypeMappingAttributes(parent.EntityType, nestedList);
            }

            this.RemoveNonMembers(nestedList);
            this.RemoveMembersNotInPath(pathInParent, nestedList);

            list.AddRange(nestedList);

            if (parent.Parent != null)
            {
                this.GetNestedDeclaredMappingAttributes(parent.Parent, pathInParent, list);
            }
            else if (this.ContextType != null
                && this.TryGetContextMember(parent.EntityId, out var contextMember))
            {
                nestedList.Clear();
                this.GetMemberMappingAttributes(contextMember, nestedList);
                var pathInContext = contextMember.Name + "." + pathInParent;
                this.RemoveNonMembers(nestedList);
                this.RemoveMembersNotInPath(pathInContext, nestedList);
                list.AddRange(nestedList);
            }
        }

        private void RemoveNonMembers(List<MappingAttribute> list)
        {
            // remove all non-nested attributes
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var ma = list[i] as MemberAttribute;
                if (ma == null)
                {
                    list.RemoveAt(i);
                    continue;
                }
            }
        }

        private void RemoveMembersNotInPath(string memberPath, List<MappingAttribute> list)
        {
            memberPath = memberPath.Length > 0 ? memberPath + "." : memberPath;

            // remove all non-nested attributes
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var ma = list[i] as MemberAttribute;
                if (ma != null && ma.Member != null)
                {
                    if (!ma.Member.StartsWith(memberPath))
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    var name = memberPath.Length > 0 ? ma.Member.Substring(memberPath.Length) : ma.Member;
                    if (name.Contains("."))
                    {
                        // further nested members not included
                        list.RemoveAt(i);
                        continue;
                    }

                    if (ma.Member != name)
                    {
                        // adjust member name so it refers to the correct member in parent
                        ma.Member = name;
                    }
                }
            }
        }

        private void GetMemberMappingAttributes(MemberInfo member, List<MappingAttribute> list)
        {
            Type memberType = TypeHelper.GetMemberType(member);
            var path = member.Name + ".";

            foreach (var ma in (MappingAttribute[])member.GetCustomAttributes(typeof(MappingAttribute)))
            {
                var entity = ma as EntityAttribute;
                if (entity != null && entity.RuntimeType == null)
                {
                    entity.RuntimeType = TypeHelper.GetSequenceElementType(memberType);
                }

                var table = ma as TableAttribute;
                if (table != null && string.IsNullOrEmpty(table.Name))
                {
                    table.Name = member.Name;
                }

                var memattr = ma as MemberAttribute;
                if (memattr != null)
                {
                    if (memattr.Member == null)
                    {
                        memattr.Member = member.Name;
                    }
                    else if (memattr.Member != member.Name && !memattr.Member.StartsWith(path))
                    {
                        // adjust any member names if they do not start with the known member name 
                        // assume it is a nested entity member name
                        memattr.Member = path + memattr.Member;
                    }
                }

                list.Add(ma);
            }
        }

        private void GetTypeMappingAttributes(Type entityType, List<MappingAttribute> list)
        {
            // get attributes from entity type itself
            foreach (var ma in entityType.GetCustomAttributes<MappingAttribute>())
            {
                var entity = ma as EntityAttribute;
                if (entity != null && entity.RuntimeType == null)
                {
                    entity.RuntimeType = entityType;
                }

                var table = ma as TableAttribute;
                if (table != null && string.IsNullOrEmpty(table.Name))
                {
                    table.Name = entityType.Name;
                }

                list.Add(ma);
            }

            foreach (var member in TypeHelper.GetDeclaredFieldsAndProperties(entityType, includeNonPublic: true))
            {
                this.GetMemberMappingAttributes(member, list);
            }
        }
        #endregion
    }
}
