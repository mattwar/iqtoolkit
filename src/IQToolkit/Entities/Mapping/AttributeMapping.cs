// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Entities.Mapping
{
    using System.Diagnostics.CodeAnalysis;
    using Utils;

    /// <summary>
    /// An <see cref="EntityMapping"/> that uses attributes on entity or context types
    /// to define the mapping.
    /// </summary>
    public class AttributeMapping : StandardMapping
    {
        private ImmutableDictionary<string, IReadOnlyList<MappingAttribute>> _idToAttributes;
        private ImmutableDictionary<MemberInfo, string> _contextMembertoEntityIdMap;
        private ImmutableDictionary<string, MemberInfo> _entityIdToContextMemberMap;

        /// <summary>
        /// Constructs a new instance of a <see cref="AttributeMapping"/> where mapping attributes are
        /// discovered on a context class (instead of from the entity types).
        /// </summary>
        /// <param name="contextType">The type of the context class that encodes the mapping attributes.
        /// If not specified, the mapping attributes are assumed to be defined on the individual entity types.</param>
        public AttributeMapping(Type? contextType = null)
        {
            this.ContextType = contextType;

            _idToAttributes = ImmutableDictionary<string, IReadOnlyList<MappingAttribute>>.Empty;
            _contextMembertoEntityIdMap = ImmutableDictionary<MemberInfo, string>.Empty;
            _entityIdToContextMemberMap = ImmutableDictionary<string, MemberInfo>.Empty;

            _contextMembers = new Lazy<IReadOnlyList<MemberInfo>>(() =>
                contextType != null
                    ? TypeHelper.GetDeclaredFieldsAndProperties(
                        contextType,
                        m => TypeHelper.IsAssignableToGeneric(TypeHelper.GetMemberType(m), typeof(IQueryable<>))
                        )
                    : ReadOnlyList<MemberInfo>.Empty
                );

            // pre-create all entities associated with context members
            foreach (var member in this.ContextMembers)
            {
                this.TryGetEntity(member, out var entity);
            }
        }

        public Type? ContextType { get; }

        /// <summary>
        /// The set of members that refer to entity tables on the context type.
        /// </summary>
        public IReadOnlyList<MemberInfo> ContextMembers => _contextMembers.Value;
        private readonly Lazy<IReadOnlyList<MemberInfo>> _contextMembers;

        /// <summary>
        /// Gets the <see cref="MappedEntity"/> for the entity id.
        /// </summary>
        public override bool TryGetEntity(
            Type entityType, 
            string? entityId,
            [NotNullWhen(true)] out MappedEntity? entity)
        {
            entityId = entityId ?? GetEntityId(entityType);
            entity = GetOrCreateEntity(entityType, entityId);
            return entity != null;
        }

        public override bool TryGetEntity(
            MemberInfo contextMember,
            [NotNullWhen(true)] out MappedEntity? entity)
        {
            if (contextMember is Type)
                throw new InvalidOperationException($"Context member '{contextMember.Name}' cannot be a type.");

            var type = TypeHelper.GetEntityType(contextMember);
            var id = GetEntityId(contextMember);
            return TryGetEntity(type, id, out entity);
        }

        /// <summary>
        /// Gets the entity id given the type.
        /// </summary>
        protected virtual string GetEntityId(Type type)
        {
            // look for the context member given the type
            if (TryGetContextMember(type, out var contextMember))
                return GetEntityId(contextMember);

            // infer id from type name.
            return type.Name;
        }

        /// <summary>
        /// Gets the entity id given a context member
        /// </summary>
        /// <param name="contextMember"></param>
        /// <returns></returns>
        protected virtual string GetEntityId(MemberInfo contextMember)
        {
            if (contextMember is Type type)
                return GetEntityId(type);

            if (!_contextMembertoEntityIdMap.TryGetValue(contextMember, out var id))
            {
                var entityAttr = contextMember.GetCustomAttribute<EntityAttribute>();

                var tmp = (entityAttr != null && !string.IsNullOrEmpty(entityAttr.Id))
                    ? entityAttr.Id
                    : contextMember.Name; // infer entity Id from context member name.

                id = ImmutableInterlocked.GetOrAdd(ref _contextMembertoEntityIdMap, contextMember, tmp);
                _entityIdToContextMemberMap = _entityIdToContextMemberMap.SetItem(id, contextMember);
            }

            return id;
        }

        /// <summary>
        /// Gets the context member associated with the entity id.
        /// </summary>
        public virtual bool TryGetContextMember(
            string entityId,
            [NotNullWhen(true)] out MemberInfo member)
        {
            member = this.ContextMembers.FirstOrDefault(m => GetEntityId(m) == entityId);
            return member != null;
        }

        /// <summary>
        /// Gets the context member associated with the entity type.
        /// </summary>
        public virtual bool TryGetContextMember(
            Type entityType,
            [NotNullWhen(true)] out MemberInfo member)
        {
            member = this.ContextMembers
                .FirstOrDefault(m => TypeHelper.GetEntityType(m) == entityType);
            return member != null;
        }

        protected override MappedEntity CreateEntity(
            Type entityType, string entityId)
        {
            var entityAttributes = GetOrCreateMappingAttributes(entityType, entityId);
            _entityIdToContextMemberMap.TryGetValue(entityId, out var contextMember);

            return new StandardEntity(
                this,
                entityId,
                entityType,
                GetEntityRuntimeType(entityType, entityId, entityAttributes),
                contextMember?.Name,
                me => CreateEntityTables(me, entityAttributes),
                me => CreateMembers(me, parent: null, entityAttributes),
                me => GetEntityDiagnostics(me)
                );
        }

        protected virtual Type GetEntityRuntimeType(
            Type entityType, 
            string entityId,
            IReadOnlyList<MappingAttribute> entityAttributes)
        {
            var attr = entityAttributes
                .OfType<EntityAttribute>()
                .FirstOrDefault();

            return attr != null && attr.ConstructedType != null
                ? attr.ConstructedType
                : entityType;
        }

        protected virtual IReadOnlyList<MappedTable> CreateEntityTables(
            MappedEntity entity,
            IReadOnlyList<MappingAttribute> entityAttributes)
        {
            var tableAttributes = entityAttributes
                .OfType<TableBaseAttribute>();

            var tableAttr = tableAttributes.OfType<TableAttribute>().FirstOrDefault();
            var extTableAttrs = tableAttributes.OfType<ExtensionTableAttribute>();

            var tables = new List<MappedTable>();
            tables.Add(CreateTable(entity, tableAttr, entityAttributes));
            tables.AddRange(extTableAttrs.Select(exTableAttr => CreateTable(entity, exTableAttr, entityAttributes)));
            return tables.ToReadOnly();
        }

        protected virtual IReadOnlyList<MappedMember> CreateMembers(
            MappedEntity entity,
            MappedMember? parent,
            IReadOnlyList<MappingAttribute> entityAttributes)
        {
            var declaringType = parent != null
                ? TypeHelper.GetSequenceElementType(parent.Type)
                : entity.Type;

            var mappedMembers = new List<MappedMember>();

            var memberAttributes = entityAttributes.OfType<MemberAttribute>().ToList();
            foreach (var memberAttribute in memberAttributes)
            {
                if (memberAttribute.Member != null 
                    && entity.Type.TryGetDeclaredFieldOrPropertyFromPath(memberAttribute.Member, out var member)
                    && member.DeclaringType == declaringType)
                {
                    var mappedMember = CreateMember(entity, parent, member, memberAttribute, entityAttributes);
                    mappedMembers.Add(mappedMember);
                }
            }

            return mappedMembers.ToReadOnly();
        }

        protected virtual MappedMember CreateMember(
            MappedEntity entity,
            MappedMember? parent,
            MemberInfo member, 
            MemberAttribute memberAttribute,
            IReadOnlyList<MappingAttribute> entityAttributes)
        {
            switch (memberAttribute)
            {
                case ColumnAttribute columnAttr:
                    return new StandardColumnMember(
                        entity,
                        parent,
                        member,
                        fnColumn: 
                            me => this.GetMemberColumn(me, columnAttr.Name, columnAttr.Table)
                        );

                case CompoundAttribute compoundAttr:
                    return new StandardCompoundMember(
                        entity,
                        parent,
                        member,
                        compoundAttr.ConstructedType ?? TypeHelper.GetMemberType(member),
                        me => CreateMembers(me.Entity, me, entityAttributes)
                        );
                case AssociationAttribute assocAttr:
                    return new StandardAssociationMember(
                        entity,
                        parent,
                        member,
                        assocAttr.IsForeignKey,
                        fnKeyColumns: 
                            me => this.GetAssociationKeyColumns(me, assocAttr.KeyColumns, null),
                        fnRelatedEntity: 
                            me => this.GetAssociationRelatedEntity(me, assocAttr.RelatedEntityId),
                        fnRelatedKeyColumns: 
                            me => this.GetAssociationRelatedKeyColumns(me, assocAttr.RelatedKeyColumns, assocAttr.KeyColumns, null)
                        );

                default:
                    throw new InvalidOperationException($"AttributeMapping: The member '{entity.Type.Name}.{member.Name}' has an unknown mapping attribute '{memberAttribute.GetType().Name}'");
            }
        }

        protected virtual MappedTable CreateTable(
            MappedEntity entity, 
            TableBaseAttribute tableAttribute,
            IReadOnlyList<MappingAttribute> entityAttributes)
        {
            var tableName = tableAttribute?.Name ?? entity.Id;

            if (tableAttribute is ExtensionTableAttribute exAttr)
            {
                return new StandardExtensionTable(
                    entity,
                    tableName,
                    fnColumns: 
                        me => CreateTableColumns(entity, me, entityAttributes),
                    fnKeyColumns:
                        me => this.GetTableKeyColumns(me, exAttr.KeyColumns),
                    fnRelatedTable: 
                        me => this.GetRelatedTable(me, exAttr.RelatedTableName),
                    fnRelatedKeyColumns:
                        me => this.GetRelatedTableKeyColumns(me, exAttr.RelatedKeyColumns, exAttr.KeyColumns, exAttr.RelatedTableName)
                    );
            }
            else
            {
                return new StandardPrimaryTable(
                    entity,
                    tableName,
                    me => CreateTableColumns(entity, me, entityAttributes)
                    );
            }
        }

        private IReadOnlyList<MappedColumn> CreateTableColumns(
            MappedEntity entity, 
            MappedTable table,
            IReadOnlyList<MappingAttribute> entityAttributes)
        {
            var columns = new List<MappedColumn>();
            var columnNameToColumnMap = new Dictionary<string, MappedColumn>();

            var columnAttrs = entityAttributes.OfType<ColumnAttribute>().ToList();

            // find all mapped member columns first
            foreach (var columnAttr in columnAttrs)
            {
                if (!string.IsNullOrEmpty(columnAttr.Table) && columnAttr.Table != table.Name)
                    continue;

                var memberName = columnAttr.Member;
                if (memberName != null
                    && entity.Type.TryGetDeclaredFieldOrPropertyFromPath(memberName, out var member))
                {
                    var columnName = !string.IsNullOrEmpty(columnAttr.Name)
                        ? columnAttr.Name
                        : member.Name;

                    var columnType = !string.IsNullOrEmpty(columnAttr.DbType)
                        ? columnAttr.DbType
                        : null;

                    if (!columnNameToColumnMap.TryGetValue(columnName, out var column))
                    {
                        column = new StandardColumn(
                            table,
                            columnName,
                            columnType,
                            isPrimaryKey: columnAttr.IsPrimaryKey,
                            isReadOnly: columnAttr.IsReadOnly,
                            isComputed: columnAttr.IsComputed,
                            isGenerated: columnAttr.IsGenerated,
                            me => entity.Members.OfType<ColumnMember>().FirstOrDefault(cm => cm.Column == me)
                            )
                        {
                        };
                        columns.Add(column);
                        columnNameToColumnMap[column.Name] = column;
                    }
                }
            }

            // find columns listed in extension tables that refer to this table
            foreach (var tableAttr in entityAttributes.OfType<ExtensionTableAttribute>())
            {
                if (tableAttr.RelatedTableName == table.Name
                    || (string.IsNullOrEmpty(tableAttr.RelatedTableName) && table == entity.PrimaryTable))
                {
                    var names = !string.IsNullOrEmpty(tableAttr.RelatedKeyColumns) ? tableAttr.RelatedKeyColumns
                        : !string.IsNullOrEmpty(tableAttr.KeyColumns) ? tableAttr.KeyColumns
                        : "";

                    foreach (var keyColumnName in GetNames(names))
                    {
                        if (!columnNameToColumnMap.ContainsKey(keyColumnName))
                        {
                            // create column, but we don't know anything about it
                            var column = this.CreateInferredColumn(table, keyColumnName);
                            columns.Add(column);
                            columnNameToColumnMap[column.Name] = column;
                        }
                    }
                }
            }

            return columns.ToList();
        }

        #region Mapping Attributes

        protected virtual IReadOnlyList<MappingAttribute> GetOrCreateMappingAttributes(
            Type entityType, string entityId)
        {
            if (!_idToAttributes.TryGetValue(entityId, out var attrs))
            {
                var tmp = CreateMappingAttributes(entityType, entityId);
                attrs = ImmutableInterlocked.GetOrAdd(ref _idToAttributes, entityId, tmp);
            }

            return attrs;
        }

        /// <summary>
        /// Creates the list of <see cref="MappingAttribute"/> for the entity.
        /// </summary>
        protected virtual IReadOnlyList<MappingAttribute> CreateMappingAttributes(
            Type entityType, string entityId)
        {
            var attributes = new List<MappingAttribute>();

            _entityIdToContextMemberMap.TryGetValue(entityId, out var contextMember);

            this.GetDeclaredMappingAttributes(entityType, entityId, attributes);

            // if no entity attribute is mentioned, add one
            if (!attributes.OfType<EntityAttribute>().Any())
            {
                attributes.Add(new EntityAttribute { ConstructedType = entityType });
            }

            // if no table attribute is mentioned, add one based on the context member or entity type
            var tableAttr = attributes.OfType<TableAttribute>().FirstOrDefault();
            if (tableAttr == null)
            {
                attributes.Add(new TableAttribute { Name = contextMember?.Name ?? entityType.Name });
            }
            else if (string.IsNullOrEmpty(tableAttr.Name))
            {
                tableAttr.Name = contextMember?.Name ?? entityType.Name;
            }

            // add any implicit member mappings
            var memberToAttributeMap = new Dictionary<MemberInfo, MemberAttribute>();
            foreach (var attr in attributes)
            {
                if (attr is MemberAttribute ma 
                    && ma.Member != null
                    && entityType.TryGetDeclaredFieldOrPropertyFromPath(ma.Member, out var member))
                {
                    memberToAttributeMap[member] = ma;
                }
            }

            AddImplicitlyMappedMembers(entityType, memberToAttributeMap, attributes, "");

            return attributes.ToReadOnly();
        }

        /// <summary>
        /// Adds attributes for members that were not explicitly mapped, 
        /// but should be implicitly mapped.
        /// </summary>
        private void AddImplicitlyMappedMembers(
            Type type, 
            Dictionary<MemberInfo, MemberAttribute> memberToAttributeMap, 
            List<MappingAttribute> list,
            string path)
        {
            // look for members that are not explicitly mapped and create column mappings for them.
            var dataMembers = type.GetDeclaredFieldsAndProperties();

            foreach (var member in dataMembers)
            {
                var memberPath = CombinePath(path, member.Name);

                var memberType = TypeHelper.GetMemberType(member);
                if (IsPossibleColumnMember(member))
                {
                    // members with scalar type are assumed to be columns
                    if (!memberToAttributeMap.ContainsKey(member))
                    {
                        var attr = new ColumnAttribute { Member = memberPath };
                        list.Add(attr);
                        memberToAttributeMap.Add(member, attr);
                    }
                }
                else if (IsPossibleCompoundMember(member))
                {
                    // members with non-sequence/non-scalar types are assumed to be nested entities
                    if (!memberToAttributeMap.TryGetValue(member, out var attr))
                    {
                        attr = new CompoundAttribute { Member = memberPath };
                        list.Add(attr);
                        memberToAttributeMap.Add(member, attr);
                    }

                    if (attr is CompoundAttribute)
                    {
                        // look for unmapped members of the compound member
                        AddImplicitlyMappedMembers(memberType, memberToAttributeMap, list, memberPath);
                    }
                }
            }
        }

        private string CombinePath(string basePath, string member)
        {
            if (basePath == "")
                return member;
            return basePath + "." + member;
        }

        /// <summary>
        /// Gets the <see cref="MappingAttribute"/> declared by the user for the entity type.
        /// </summary>
        protected virtual void GetDeclaredMappingAttributes(
            Type entityType, string entityId, List<MappingAttribute> list)
        {
            if (this.ContextType != null
                && this.TryGetContextMember(entityId, out var contextMember))
            { 
                this.GetMemberMappingAttributes(contextMember, list);
            }

            this.GetTypeMappingAttributes(entityType, list);
        }

        private void GetMemberMappingAttributes(MemberInfo member, List<MappingAttribute> list)
        {
            foreach (var attr in member.GetCustomAttributes<MappingAttribute>())
            {
                if (attr is MemberAttribute ma && ma.Member == null)
                {
                    ma.Member = member.Name;
                }

                list.Add(attr);
            }
        }

        private void GetTypeMappingAttributes(Type entityType, List<MappingAttribute> list)
        {
            // get attributes from entity type itself
            foreach (var ma in entityType.GetCustomAttributes<MappingAttribute>())
            {
                var entity = ma as EntityAttribute;
                if (entity != null && entity.ConstructedType == null)
                {
                    entity.ConstructedType = entityType;
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
