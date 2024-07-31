// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Entities.Mapping
{
    using Utils;

    /// <summary>
    /// An attribute used to define information to help map between CLR types/members and database tables/columns.
    /// </summary>
    public abstract class MappingAttribute : Attribute
    {
    }

    /// <summary>
    /// Describes information about an entity class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class EntityAttribute : MappingAttribute
    {
        /// <summary>
        /// The ID associated with the entity mapping.
        /// If not specified, the entity id will be the entity type's simple name.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The type that is constructed when the entity is returned as the result of a query.
        /// If not specified it is the same as the entity type, the type the attribute is placed on.
        /// </summary>
        public Type? RuntimeType { get; set; }
    }

    /// <summary>
    /// A base type for mapping attributes that describe table-like mapping.
    /// </summary>
    public abstract class TableBaseAttribute : MappingAttribute
    {
        /// <summary>
        /// The name of the table in the database. 
        /// If not specified, the table's name will be the name of the member or type the attribute is placed on.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The ID to use for this table in advanced multi-table mapping.
        /// If not specified, the <see cref="Id"/> will be the table's name.
        /// </summary>
        public string? Id { get; set; }
    }

    /// <summary>
    /// Describes the mapping between at database table and an entity type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct|AttributeTargets.Interface|AttributeTargets.Property|AttributeTargets.Field, AllowMultiple = false)]
    public class TableAttribute : TableBaseAttribute
    {
    }

    /// <summary>
    /// Describes the mapping between additional database tables and an entity type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class ExtensionTableAttribute : TableBaseAttribute
    {
        /// <summary>
        /// The columns in the extension table that correspond to columns in the primary table.
        /// Must be specified.
        /// </summary>
        public string? KeyColumns { get; set; }

        /// <summary>
        /// The id of the primary table used in advanced multi-table mapping.
        /// If not specified, the related table ID is the primary table's ID.
        /// </summary>
        public string? RelatedTableId { get; set; }

        /// <summary>
        /// The columns in the primary table that correspond to the key columns in the extension table.
        /// If not specified, it is assumed the column names from both tables are the same.
        /// </summary>
        public string? RelatedKeyColumns { get; set; }
    }

    /// <summary>
    /// A base class for member mapping information.
    /// </summary>
    public abstract class MemberAttribute : MappingAttribute
    {
        /// <summary>
        /// The member for the mapping.
        /// If not specified it is inferred to be the member the attribute is placed on.
        /// </summary>
        public string? Member { get; set; }
    }

    /// <summary>
    /// Denotes the entity as a 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class NestedEntityAttribute : MemberAttribute
    {
        /// <summary>
        /// The type that is constructed when the entity is returned as the result of a query.
        /// If not specified it is the same as the entity type, the type of the class or element type of the member the attribute is placed on.
        /// </summary>
        public Type? RuntimeType { get; set; }
    }

    /// <summary>
    /// Describes the mapping between an entity type member and a database column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class ColumnAttribute : MemberAttribute
    {
        /// <summary>
        /// The name of the column in the database.
        /// If not specified, the name of the <see cref="MemberAttribute.Member"/> is used.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The ID of the table the column belongs to in advanced multi-table mapping.
        /// If not specified, the table ID will be the primary table's ID.
        /// </summary>
        public string? TableId { get; set; }

        /// <summary>
        /// The type of the column as describe in the database language.
        /// If not specified, the column type is inferred from the member's type.
        /// This value is used to determine the appropriate database type when sending data 
        /// during insert and update commands or to properly encode parameters.
        /// </summary>
        public string? DbType { get; set; }

        /// <summary>
        /// True if the column is computed by the database on insert/update.
        /// </summary>
        public bool IsComputed { get; set; }

        /// <summary>
        /// True if the column is part of the primary key of the table.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// True if the value of the column is generated by the database on insert.
        /// </summary>
        public bool IsGenerated { get; set; }

        /// <summary>
        /// True if the column is read-only. 
        /// Changes made on the client will be ignored during update.
        /// </summary>
        public bool IsReadOnly { get; set; }  
    }

    /// <summary>
    /// A <see cref="MappingAttribute"/> that describes an association between two entities via related column
    /// values in the tables underlying each. This is often the same as a foreign key relationship in the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class AssociationAttribute : MemberAttribute
    {
        /// <summary>
        /// The members of the entity that are used to associate this entity with the other related entities.
        /// This property must be specified.
        /// </summary>
        public string? KeyMembers { get; set; }

        /// <summary>
        /// The mapping ID of the related entity.
        /// If not specified, it is inferred to be the entity id of the related entity type.
        /// </summary>
        public string? RelatedEntityId { get; set; }

        /// <summary>
        /// The members in the related entity type that form the association key.
        /// If not specified, the related key members are inferred to have the same names as the key members. 
        /// </summary>
        public string? RelatedKeyMembers { get; set; }

        /// <summary>
        /// True if the association's <see cref="KeyMembers"/> correpsonding columns are foreign keys (constrained to the related table's primary key).
        /// This information is important to correctly order inserts, updates and deletes without violating foreign key constraints in the database.
        /// </summary>
        public bool IsForeignKey { get; set; }
    }

    /// <summary>
    /// An <see cref="AdvancedEntityMapping"/> that is defined by <see cref="MappingAttribute"/>'s
    /// on either the entity types or on the query/table properties of a context class.
    /// </summary>
    public class AttributeEntityMapping : AdvancedEntityMapping
    {
        private readonly Type? _contextType;
        private ImmutableDictionary<string, MappingEntity> _nameToEntityMap;

        /// <summary>
        /// Constructs a new instance of a <see cref="AttributeEntityMapping"/> where mapping attributes are
        /// discovered on a context class (instead of from the entity types).
        /// </summary>
        /// <param name="contextType">The type of the context class that encodes the mapping attributes.
        /// If not spefied, the mapping attributes are assumed to be defined on the individual entity types.</param>
        public AttributeEntityMapping(Type? contextType = null)
        {
            _contextType = contextType;
            _nameToEntityMap = ImmutableDictionary<string, MappingEntity>.Empty;
        }

        /// <summary>
        /// Gets the <see cref="MappingEntity"/> for the member (property/field) of a context type.
        /// </summary>
        public override MappingEntity GetEntity(MemberInfo contextMember)
        {
            Type elementType = TypeHelper.GetSequenceElementType(TypeHelper.GetMemberType(contextMember));
            return this.GetEntity(elementType, contextMember.Name);
        }

        /// <summary>
        /// Gets the <see cref="MappingEntity"/> associated with a type and entity-id
        /// </summary>
        public override MappingEntity GetEntity(Type entityType, string? entityId)
        {
            return this.GetEntity(entityType, entityId ?? this.GetEntityId(entityType), null);
        }

        /// <summary>
        /// Gets the <see cref="MappingEntity"/> associated with an entity-id, where the entity-type may different 
        /// from the element-type exposed via the entity collection.
        /// </summary>
        private MappingEntity GetEntity(Type entityType, string entityId, ParentEntity? parent)
        {
            if (!_nameToEntityMap.TryGetValue(entityId, out var entity))
            {
                var tmp = this.CreateEntity(entityType, entityId, parent);
                entity = ImmutableInterlocked.GetOrAdd(ref _nameToEntityMap, entityId, tmp);
            }

            return entity;
        }

        /// <summary>
        /// Gets all the mapping attributes for the entity type, even ones inferred (non-strict mode)
        /// </summary>
        private IEnumerable<MappingAttribute> GetMappingAttributes(Type entityType, string entityId, ParentEntity? parent)
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
            var entityTypeInfo = entityType.GetTypeInfo();

            var dataMembers = entityTypeInfo.DeclaredProperties.Where(p => p.GetMethod.IsPublic && !p.GetMethod.IsStatic).Cast<MemberInfo>()
                            .Concat(entityTypeInfo.DeclaredFields.Where(f => f.IsPublic && !f.IsStatic));
                            
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

            return list;
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
        /// Gets the mapping attributes declared by the user for the entity type.
        /// </summary>
        protected virtual void GetDeclaredMappingAttributes(Type entityType, string entityId, ParentEntity? parent, List<MappingAttribute> list)
        {
            if (parent != null)
            {
                this.GetNestedDeclaredMappingAttributes(parent, null, list);
            }
            else if (_contextType != null)
            { 
                var contextMember = this.GetContextCollectionMember(entityType, entityId);
                this.GetMemberMappingAttributes(contextMember, list);
                this.RemoveMembersNotInPath(contextMember.Name, list);
            }

            this.GetTypeMappingAttributes(entityType, list);
            this.RemoveMembersNotInPath("", list);
        }

        private void GetNestedDeclaredMappingAttributes(ParentEntity parent, string? nestedPath, List<MappingAttribute> list)
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
            else if (_contextType != null)
            {
                nestedList.Clear();
                var contextMember = this.GetContextCollectionMember(parent.EntityType, parent.EntityId);
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
            foreach (var ma in entityType.GetTypeInfo().GetCustomAttributes<MappingAttribute>())
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

            foreach (var member in TypeHelper.GetFieldsAndProperties(entityType, includeNonPublic: true))
            {
                this.GetMemberMappingAttributes(member, list);
            }
        }

        /// <summary>
        /// Gets the mapping entity id associated with an entity type.
        /// </summary>
        public override string GetEntityId(Type entityType)
        {
            if (_contextType != null)
            {
                // look for entity id specified on table attribute of corresponding context member
                var members = this.GetContextCollectionMembers(entityType).ToList();

                foreach (var mi in members)
                {
                    var entityAttr = mi.GetCustomAttribute<EntityAttribute>();
                    if (entityAttr != null && entityAttr.Id != null)
                    {
                        return entityAttr.Id;
                    }
                }
            }
            else
            {
                // look for entity id specified on table attribute 
                var entityAttr = entityType.GetTypeInfo().GetCustomAttribute<EntityAttribute>();
                if (entityAttr != null && entityAttr.Id != null)
                {
                    return entityAttr.Id;
                }
            }

            // use the entity type name as the entity id
            return entityType.Name;
        }

        private MemberInfo GetContextCollectionMember(Type entityType, string entityId)
        {
            entityId = entityId ?? this.GetEntityId(entityType);

            // look for collection member with the specified entity-id
            var members = this.GetContextCollectionMembers(entityType).ToList();

            foreach (var mi in members)
            {
                var entityAttr = mi.GetCustomAttribute<EntityAttribute>();
                if (entityAttr != null)
                {
                    if (entityAttr.Id != null && entityAttr.Id == entityId)
                    {
                        return mi;
                    }
                    else if (entityAttr.RuntimeType != null && entityAttr.RuntimeType == entityType)
                    {
                        return mi;
                    }
                }
            }

            foreach (var mi in members)
            {
                if (TypeHelper.GetSequenceElementType(TypeHelper.GetMemberType(mi)) == entityType)
                {
                    return mi;
                }
            }

            throw new InvalidOperationException(string.Format("The matching member on the context type '{0}' with the entity id '{0}' cannot be found.", entityId));
        }

        /// <summary>
        /// Get all the members on the context type that are entity collections with compatible element types.
        /// </summary>
        private IEnumerable<MemberInfo> GetContextCollectionMembers(Type entityType)
        {
            return this.GetContextCollectionMembers()
                .Where(mi => TypeHelper.GetSequenceElementType(TypeHelper.GetMemberType(mi)) == entityType);
        }

        private static bool IsContextCollectionMember(MemberInfo member)
        {
            return typeof(IQueryable).IsAssignableFrom(TypeHelper.GetMemberType(member));
        }

        /// <summary>
        /// Get all the members on the context type that are entity collections.
        /// </summary>
        private IReadOnlyList<MemberInfo> GetContextCollectionMembers()
        {
            if (_contextCollectionMembers == null)
            {
                var tmp = _contextType != null
                    ? TypeHelper.GetFieldsAndProperties(_contextType, includeNonPublic: true)
                        .Where(IsContextCollectionMember)
                        .ToList()
                    : ReadOnlyList<MemberInfo>.Empty;
                System.Threading.Interlocked.CompareExchange(ref _contextCollectionMembers, tmp, null);
            }

            return _contextCollectionMembers;
        }

        private IReadOnlyList<MemberInfo>? _contextCollectionMembers;



        protected class ParentEntity
        {
            public ParentEntity? Parent { get; }
            public MemberInfo Member { get; }
            public Type EntityType { get; }
            public string EntityId { get; }

            public ParentEntity(ParentEntity? parent, MemberInfo member, Type entityType, string entityId)
            {
                this.Parent = parent;
                this.Member = member;
                this.EntityType = entityType;
                this.EntityId = entityId;
            }

            public ParentEntity Root
            {
                get
                {
                    var p = this;

                    while(p.Parent != null)
                    {
                        p = p.Parent;
                    }

                    return p; 
                }
            }
        }

        /// <summary>
        /// Create a <see cref="MappingEntity"/>.
        /// </summary>
        /// <param name="entityType">The entity type this mapping is for.</param>
        /// <param name="entityId">The mapping id of the entity type.</param>
        /// <param name="parent"></param>
        private MappingEntity CreateEntity(Type entityType, string entityId, ParentEntity? parent)
        {
            var members = new HashSet<string>();
            var mappingMembers = new List<AttributeMappingMember>();

            // get the attributes given the type that has the mappings (root type, in case of nested entity)
            var mappingAttributes = this.GetMappingAttributes(entityType, entityId, parent);

            var tableAttributes = mappingAttributes.OfType<TableBaseAttribute>()
                .OrderBy(ta => ta.Name);

            var staticType = entityType;
            var runtimeType = entityType;

            // check to see if mapping attributes tell us about a different runtime.
            var entityAttr = mappingAttributes.OfType<EntityAttribute>().FirstOrDefault();
            if (entityAttr != null && entityAttr.RuntimeType != null && parent == null)
            {
                runtimeType = entityAttr.RuntimeType;
            }

            var memberAttributes = mappingAttributes.OfType<MemberAttribute>()
                .OrderBy(ma => ma.Member);

            foreach (var attr in memberAttributes)
            {
                if (string.IsNullOrEmpty(attr.Member))
                    continue;

                var memberName = attr.Member;

                if (attr is NestedEntityAttribute nestedEntity)
                {
                    members.Add(memberName);
                    if (this.FindMember(entityType, memberName) is { } member)
                    {
                        var nestedEntityId = entityId + "." + memberName;
                        var nestedEntityType = TypeHelper.GetMemberType(member);
                        var nested = (AttributeMappingEntity)this.GetEntity(nestedEntityType, nestedEntityId, new ParentEntity(parent, member, entityType, entityId));
                        mappingMembers.Add(new AttributeMappingMember(member, attr, nested));
                    }
                }
                else 
                {
                    if (members.Contains(memberName))
                    {
                        throw new InvalidOperationException(string.Format("AttributeMapping: more than one mapping attribute specified for member '{0}' on type '{1}'", memberName, entityType.Name));
                    }

                    if (this.FindMember(entityType, memberName) is { } member)
                    {
                        mappingMembers.Add(new AttributeMappingMember(member, attr, null));
                    }
                }
            }

            return new AttributeMappingEntity(staticType, runtimeType, entityId, tableAttributes, mappingMembers);
        }

        private static readonly char[] dotSeparator = new char[] { '.' };

        private MemberInfo? FindMember(Type type, string path)
        {
            MemberInfo? member = null;
            string[] names = path.Split(dotSeparator);

            foreach (string name in names)
            {
                member = TypeHelper.FindFieldOrProperty(type, name, includeNonPublic: true);

                if (member == null)
                {
                    throw new InvalidOperationException(string.Format("AttributMapping: the member '{0}' does not exist on type '{1}'", name, type.Name));
                }

                type = TypeHelper.GetSequenceElementType(TypeHelper.GetMemberType(member));
            }

            return member;
        }

        public override string GetTableName(MappingEntity entity)
        {
            AttributeMappingEntity en = (AttributeMappingEntity)entity;
            var table = en.Tables.FirstOrDefault();
            return this.GetTableName(table);
        }

        private string GetTableName(MappingEntity entity, TableBaseAttribute attr)
        {
            string name = (attr != null && !string.IsNullOrEmpty(attr.Name))
                ? attr.Name
                : entity.EntityId;
            return name;
        }

        public override IReadOnlyList<MemberInfo> GetMappedMembers(MappingEntity entity)
        {
            return entity is AttributeMappingEntity ame
                ? ame.Members
                : Array.Empty<MemberInfo>();
        }

        public override bool IsMapped(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { };
        }

        public override bool IsColumn(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Column != null;
        }

        public override bool IsComputed(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Column != null
                && mm.Column.IsComputed;
        }

        public override bool IsGenerated(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Column != null
                && mm.Column.IsGenerated;
        }
        
        public override bool IsReadOnly(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Column != null
                && mm.Column.IsReadOnly;
        }

        public override bool IsPrimaryKey(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Column != null
                && mm.Column.IsPrimaryKey;
        }

        public override string GetColumnName(MappingEntity entity, MemberInfo member)
        {
            if (entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Column != null
                && !string.IsNullOrEmpty(mm.Column.Name))
            {
                return mm.Column.Name;
            }

            return base.GetColumnName(entity, member);
        }

        public override string? GetColumnDbType(MappingEntity entity, MemberInfo member)
        {
            if (entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Column != null
                && !string.IsNullOrEmpty(mm.Column.DbType))
            {
                return mm.Column.DbType;
            }

            return null;
        }

        public override bool IsAssociationRelationship(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Association != null;
        }

        public override bool IsRelationshipSource(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Association != null
                && mm.Association.IsForeignKey
                && !typeof(IEnumerable).IsAssignableFrom(TypeHelper.GetMemberType(member));
        }

        public override bool IsRelationshipTarget(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Association != null
                && (!mm.Association.IsForeignKey
                    || typeof(IEnumerable).IsAssignableFrom(TypeHelper.GetMemberType(member)));
        }

        public override bool IsNestedEntity(MappingEntity entity, MemberInfo member)
        {
            return entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.NestedEntity != null;
        }

        public override MappingEntity GetRelatedEntity(MappingEntity entity, MemberInfo member)
        {
            if (entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm)
            {
                if (mm.Association != null
                    && !string.IsNullOrEmpty(mm.Association.RelatedEntityId))
                {
                    Type relatedEntityType = TypeHelper.GetSequenceElementType(TypeHelper.GetMemberType(member));
                    return this.GetReferencedEntity(relatedEntityType, mm.Association.RelatedEntityId, "Association.RelatedEntityI");
                }
                else if (mm.NestedEntity != null)
                {
                    return mm.NestedEntity;
                }
            }

            return base.GetRelatedEntity(entity, member);
        }

        private static readonly char[] separators = new char[] {' ', ',', '|' };

        public override IReadOnlyList<MemberInfo> GetAssociationKeyMembers(MappingEntity entity, MemberInfo member)
        {
            if (entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Association != null
                && mm.Association.KeyMembers != null)
            {
                return this.GetReferencedMembers(ame, mm.Association.KeyMembers, "Association.KeyMembers", ame.StaticType);
            }

            return base.GetAssociationKeyMembers(entity, member);
        }

        public override IReadOnlyList<MemberInfo> GetAssociationRelatedKeyMembers(MappingEntity entity, MemberInfo member)
        {
            if (entity is AttributeMappingEntity thisEntity
                && thisEntity.GetMappingMember(member.Name) is { } mm
                && mm.Association != null
                && (mm.Association.RelatedKeyMembers != null || mm.Association.KeyMembers != null)
                && this.GetRelatedEntity(entity, member) is AttributeMappingEntity relatedEntity)
            {
                return this.GetReferencedMembers(relatedEntity, mm.Association.RelatedKeyMembers ?? mm.Association.KeyMembers ?? "", "Association.RelatedKeyMembers", thisEntity.StaticType);
            }

            return base.GetAssociationRelatedKeyMembers(entity, member);
        }

        private IReadOnlyList<MemberInfo> GetReferencedMembers(AttributeMappingEntity entity, string names, string source, Type sourceType)
        {
            return names
                .Split(separators)
                .Select(n => this.GetReferencedMember(entity, n, source, sourceType))
                .ToReadOnly();
        }

        private MemberInfo GetReferencedMember(AttributeMappingEntity entity, string name, string source, Type sourceType)
        {
            var mm = entity.GetMappingMember(name);
            if (mm == null)
            {
                throw new InvalidOperationException(string.Format("AttributeMapping: The member '{0}.{1}' referenced in {2} for '{3}' is not mapped or does not exist", entity.StaticType.Name, name, source, sourceType.Name));
            }

            return mm.Member;
        }

        private MappingEntity GetReferencedEntity(Type entityType, string entityId, string source)
        {
            var entity = this.GetEntity(entityType, entityId);

            if (entity == null)
            {
                throw new InvalidOperationException(string.Format("The entity '{0}' referenced in {1} of '{2}' does not exist", entityId, source, entityType.Name));
            }

            return entity;
        }

        public override IReadOnlyList<MappingTable> GetTables(MappingEntity entity)
        {
            return (entity is AttributeMappingEntity ame)
                ? ame.Tables
                : Array.Empty<MappingTable>();
        }

        public override MappingEntity GetEntity(MappingTable table)
        {
            return ((AttributeMappingTable)table).Entity;
        }

        public override string GetTableId(MappingTable table)
        {
            // look for id specified by the table attribute
            var mappingTable = (AttributeMappingTable)table;
            if (mappingTable.Attribute != null && !string.IsNullOrEmpty(mappingTable.Attribute.Id))
            {
                return mappingTable.Attribute.Id;
            }
            else
            {
                // no alias specified, use the table's name.
                return this.GetTableName(table);
            }
        }

        public override string GetTableId(MappingEntity entity, MemberInfo member)
        {
            // look for id specified with column attribute
            if (entity is AttributeMappingEntity ame
                && ame.GetMappingMember(member.Name) is { } mm
                && mm.Column != null
                && !string.IsNullOrEmpty(mm.Column.TableId))
            {
                return mm.Column.TableId;
            }
            else
            {
                // no id is specified, use the id of the primary table.
                return this.GetTableId(this.GetPrimaryTable(entity));
            }
        }

        public override string GetTableName(MappingTable table)
        {
            var amt = (AttributeMappingTable)table;
            return this.GetTableName(amt.Entity, amt.Attribute);
        }

        public override bool IsExtensionTable(MappingTable table)
        {
            return ((AttributeMappingTable)table).Attribute is ExtensionTableAttribute;
        }

        public override string GetExtensionRelatedTableId(MappingTable table)
        {
            var attr = ((AttributeMappingTable)table).Attribute as ExtensionTableAttribute;
            if (attr != null && !string.IsNullOrEmpty(attr.RelatedTableId))
            {
                return attr.RelatedTableId;
            }
            else
            {
                // use the primary table's id
                return this.GetTableId(this.GetPrimaryTable(this.GetEntity(table)));
            }
        }

        public override IReadOnlyList<string> GetExtensionKeyColumnNames(MappingTable table)
        {
            if (table is AttributeMappingTable amt
                && amt.Attribute is ExtensionTableAttribute attr
                && !string.IsNullOrEmpty(attr.KeyColumns))
            {
                return attr.KeyColumns.Split(separators);
            }
            else
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Gets all extension tables (not the primary table) of a multi-table mapping.
        /// </summary>
        public override IReadOnlyList<MappingTable> GetExtensionTables(MappingEntity entity)
        {
            return this.GetTables(entity).Where(t => this.IsExtensionTable(t)).ToReadOnly();
        }

        public override IReadOnlyList<MemberInfo> GetExtensionRelatedMembers(MappingTable table)
        {
            var amt = (AttributeMappingTable)table;
            var attr = amt.Attribute as ExtensionTableAttribute;
            if (attr == null) return new MemberInfo[] { };
            var relatedKeyColumns = attr.RelatedKeyColumns ?? attr.KeyColumns;
            if (!string.IsNullOrEmpty(relatedKeyColumns))
            {
                return relatedKeyColumns
                    .Split(separators)
                    .Select(n => this.GetMemberForColumn(amt.Entity, n))
                    .OfType<MemberInfo>()
                    .ToReadOnly();
            }
            else
            {
                return Array.Empty<MemberInfo>();
            }
        }

        private MemberInfo? GetMemberForColumn(MappingEntity entity, string columnName)
        {
            foreach (var m in this.GetMappedMembers(entity))
            {
                if (this.IsNestedEntity(entity, m))
                {
                    var m2 = this.GetMemberForColumn(this.GetRelatedEntity(entity, m), columnName);
                    if (m2 != null)
                        return m2;
                }
                else if (this.IsColumn(entity, m) && string.Compare(this.GetColumnName(entity, m), columnName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return m;
                }
            }
            return null;
        }

        private class AttributeMappingMember
        {
            private readonly MemberInfo _member;
            private readonly MemberAttribute _attribute;
            private readonly AttributeMappingEntity? _nested;

            public AttributeMappingMember(MemberInfo member, MemberAttribute attribute, AttributeMappingEntity? nested)
            {
                _member = member;
                _attribute = attribute;
                _nested = nested;
            }

            public MemberInfo Member => _member;

            public ColumnAttribute? Column => 
                _attribute as ColumnAttribute;

            public AssociationAttribute? Association => 
                _attribute as AssociationAttribute;

            public AttributeMappingEntity? NestedEntity => 
                _nested;
        }

        private class AttributeMappingTable : MappingTable
        {
            public AttributeMappingEntity Entity { get; }
            public TableBaseAttribute Attribute { get; }

            public AttributeMappingTable(AttributeMappingEntity entity, TableBaseAttribute attribute)
            {
                this.Entity = entity;
                this.Attribute = attribute;
            }
        }

        private class AttributeMappingEntity : MappingEntity
        {
            public override Type StaticType { get; }
            public override Type RuntimeType { get; }
            public override string EntityId { get; }
            internal IReadOnlyList<MappingTable> Tables { get; }
            internal IReadOnlyList<MemberInfo> Members { get; }

            private readonly Dictionary<string, AttributeMappingMember> _nameToMemberMap;

            public AttributeMappingEntity(
                Type staticType, 
                Type runtimeType, 
                string entityId, 
                IEnumerable<TableBaseAttribute> tableAttributes, 
                IEnumerable<AttributeMappingMember> mappingMembers)
            {
                this.StaticType = staticType;
                this.RuntimeType = runtimeType;
                this.EntityId = entityId;
                this.Tables = tableAttributes.Select(a => (MappingTable)new AttributeMappingTable(this, a)).ToReadOnly();
                this.Members = mappingMembers.Select(mm => mm.Member).ToReadOnly();
                _nameToMemberMap = mappingMembers.ToDictionary(mm => mm.Member.Name);
            }

            internal AttributeMappingMember? GetMappingMember(string name)
            {
                _nameToMemberMap.TryGetValue(name, out var mm);
                return mm;
            }
        }
    }
}
