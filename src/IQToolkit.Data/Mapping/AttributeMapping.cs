// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Data.Mapping
{
    using Common;
    using System.Collections.Concurrent;

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
        public string Id { get; set; }

        /// <summary>
        /// The type that is constructed when the entity is returned as the result of a query.
        /// If not specified it is the same as the entity type, the type the attribute is placed on.
        /// </summary>
        public Type RuntimeType { get; set; }
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
        public string Name { get; set; }

        /// <summary>
        /// The ID to use for this table in advanced multi-table mapping.
        /// If not specified, the <see cref="Id"/> will be the table's name.
        /// </summary>
        public string Id { get; set; }
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
        public string KeyColumns { get; set; }

        /// <summary>
        /// The id of the primary table used in advanced multi-table mapping.
        /// If not specified, the related table ID is the primary table's ID.
        /// </summary>
        public string RelatedTableId { get; set; }

        /// <summary>
        /// The columns in the primary table that correspond to the key columns in the extension table.
        /// If not specified, it is assumed the column names from both tables are the same.
        /// </summary>
        public string RelatedKeyColumns { get; set; }
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
        public string Member { get; set; }
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
        public Type RuntimeType { get; set; }
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
        public string Name { get; set; }

        /// <summary>
        /// The ID of the table the column belongs to in advanced multi-table mapping.
        /// If not specified, the table ID will be the primary table's ID.
        /// </summary>
        public string TableId { get; set; }

        /// <summary>
        /// The type of the column as describe in the database language.
        /// If not specified, the column type is inferred from the member's type.
        /// This value is used to determine the appropriate database type when sending data 
        /// during insert and update commands or to properly encode parameters.
        /// </summary>
        public string DbType { get; set; }

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
        public string KeyMembers { get; set; }

        /// <summary>
        /// The mapping ID of the related entity.
        /// If not specified, it is inferred to be the entity id of the related entity type.
        /// </summary>
        public string RelatedEntityId { get; set; }

        /// <summary>
        /// The members in the related entity type that form the association key.
        /// If not specified, the related key members are inferred to have the same names as the key members. 
        /// </summary>
        public string RelatedKeyMembers { get; set; }

        /// <summary>
        /// True if the association's <see cref="KeyMembers"/> correpsonding columns are foreign keys (constrained to the related table's primary key).
        /// This information is important to correctly order inserts, updates and deletes without violating foreign key constraints in the database.
        /// </summary>
        public bool IsForeignKey { get; set; }
    }

    /// <summary>
    /// An <see cref="AdvancedMapping"/> that is defined by <see cref="MappingAttribute"/>'s
    /// on either the entity types or on the query/table properties of a context class.
    /// </summary>
    public class AttributeMapping : AdvancedMapping
    {
        private readonly Type contextType;
        private readonly ConcurrentDictionary<string, MappingEntity> entities;

        /// <summary>
        /// Constructs a new instance of a <see cref="AttributeMapping"/> where mapping attributes are
        /// discovered on a context class (instead of from the entity types).
        /// </summary>
        /// <param name="contextType">The type of the context class that encodes the mapping attributes.
        /// If not spefied, the mapping attributes are assumed to be defined on the individual entity types.</param>
        public AttributeMapping(Type contextType = null)
        {
            this.contextType = contextType;
            this.entities = new ConcurrentDictionary<string, MappingEntity>();
        }

        /// <summary>
        /// Gets the <see cref="MappingEntity"/> for the member (property/field) of a context type.
        /// </summary>
        public override MappingEntity GetEntity(MemberInfo contextMember)
        {
            Type elementType = TypeHelper.GetElementType(TypeHelper.GetMemberType(contextMember));
            return this.GetEntity(elementType, contextMember.Name);
        }

        /// <summary>
        /// Gets the <see cref="MappingEntity"/> associated with a type and entity-id
        /// </summary>
        public override MappingEntity GetEntity(Type entityType, string entityId)
        {
            return this.GetEntity(entityType, entityId ?? this.GetEntityId(entityType), null);
        }

        /// <summary>
        /// Gets the <see cref="MappingEntity"/> associated with an entity-id, where the entity-type may different 
        /// from the element-type exposed via the entity collection.
        /// </summary>
        private MappingEntity GetEntity(Type entityType, string entityId, ParentEntity parent)
        {
            MappingEntity entity;

            if (!entities.TryGetValue(entityId, out entity))
            {
                entity = entities.GetOrAdd(entityId, this.CreateEntity(entityType, entityId, parent));
            }

            return entity;
        }

        /// <summary>
        /// Gets all the mapping attributes for the entity type, even ones inferred (non-strict mode)
        /// </summary>
        private IEnumerable<MappingAttribute> GetMappingAttributes(Type entityType, string entityId, ParentEntity parent)
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

            var membersAlreadyMapped = new HashSet<string>(list.OfType<MemberAttribute>().Select(m => m.Member));

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
            switch (TypeHelper.GetTypeCode(type))
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
        protected virtual void GetDeclaredMappingAttributes(Type entityType, string entityId, ParentEntity parent, List<MappingAttribute> list)
        {
            if (parent != null)
            {
                this.GetNestedDeclaredMappingAttributes(parent, null, list);
            }
            else if (this.contextType != null)
            { 
                var contextMember = this.GetContextCollectionMember(entityType, entityId);
                this.GetMemberMappingAttributes(contextMember, list);
                this.RemoveMembersNotInPath(contextMember.Name, list);
            }

            this.GetTypeMappingAttributes(entityType, list);
            this.RemoveMembersNotInPath("", list);
        }

        private void GetNestedDeclaredMappingAttributes(ParentEntity parent, string nestedPath, List<MappingAttribute> list)
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
            else if (this.contextType != null)
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
                    entity.RuntimeType = TypeHelper.GetElementType(memberType);
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

            foreach (var member in TypeHelper.GetDataMembers(entityType, includeNonPublic: true))
            {
                this.GetMemberMappingAttributes(member, list);
            }
        }

        /// <summary>
        /// Gets the mapping entity id associated with an entity type.
        /// </summary>
        public override string GetEntityId(Type entityType)
        {
            if (contextType != null)
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
                if (TypeHelper.GetElementType(TypeHelper.GetMemberType(mi)) == entityType)
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
                .Where(mi => TypeHelper.GetElementType(TypeHelper.GetMemberType(mi)).IsAssignableFrom(entityType));
        }

        /// <summary>
        /// Get all the members on the context type that are entity collections.
        /// </summary>
        private IEnumerable<MemberInfo> GetContextCollectionMembers()
        {
            foreach (var mi in TypeHelper.GetDataMembers(this.contextType, includeNonPublic: true))
            {
                FieldInfo fi = mi as FieldInfo;
                if (fi != null && TypeHelper.IsSequenceType(fi.FieldType))
                    yield return mi;

                PropertyInfo pi = mi as PropertyInfo;
                if (pi != null && TypeHelper.IsSequenceType(pi.PropertyType))
                    yield return mi;
            }
        }

        protected class ParentEntity
        {
            public ParentEntity Parent { get; }
            public MemberInfo Member { get; }
            public Type EntityType { get; }
            public string EntityId { get; }

            public ParentEntity(ParentEntity parent, MemberInfo member, Type entityType, string entityId)
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
        private MappingEntity CreateEntity(Type entityType, string entityId, ParentEntity parent)
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
                MemberInfo member = null;
                MemberAttribute attribute = null;
                AttributeMappingEntity nested = null;

                var nestedEntity = attr as NestedEntityAttribute;
                if (nestedEntity != null)
                {
                    members.Add(memberName);
                    member = this.FindMember(entityType, memberName);
                    var nestedEntityId = entityId + "." + memberName;
                    var nestedEntityType = TypeHelper.GetMemberType(member);
                    nested = (AttributeMappingEntity)this.GetEntity(nestedEntityType, nestedEntityId, new ParentEntity(parent, member, entityType, entityId));
                }
                else 
                {
                    if (members.Contains(memberName))
                    {
                        throw new InvalidOperationException(string.Format("AttributeMapping: more than one mapping attribute specified for member '{0}' on type '{1}'", memberName, entityType.Name));
                    }

                    member = this.FindMember(entityType, memberName);
                    attribute = attr;
                }

                mappingMembers.Add(new AttributeMappingMember(member, attribute, nested));
            }

            return new AttributeMappingEntity(staticType, runtimeType, entityId, tableAttributes, mappingMembers);
        }

        private static readonly char[] dotSeparator = new char[] { '.' };

        private MemberInfo FindMember(Type type, string path)
        {
            MemberInfo member = null;
            string[] names = path.Split(dotSeparator);
            foreach (string name in names)
            {
                member = TypeHelper.GetDataMember(type, name, includeNonPublic: true);

                if (member == null)
                {
                    throw new InvalidOperationException(string.Format("AttributMapping: the member '{0}' does not exist on type '{1}'", name, type.Name));
                }

                type = TypeHelper.GetElementType(TypeHelper.GetMemberType(member));
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

        public override IEnumerable<MemberInfo> GetMappedMembers(MappingEntity entity)
        {
            return ((AttributeMappingEntity)entity).MappedMembers;
        }

        public override bool IsMapped(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            return mm != null;
        }

        public override bool IsColumn(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            return mm != null && mm.Column != null;
        }

        public override bool IsComputed(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            return mm != null && mm.Column != null && mm.Column.IsComputed;
        }

        public override bool IsGenerated(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            return mm != null && mm.Column != null && mm.Column.IsGenerated;
        }
        
        public override bool IsReadOnly(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            return mm != null && mm.Column != null && mm.Column.IsReadOnly;
        }

        public override bool IsPrimaryKey(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            return mm != null && mm.Column != null && mm.Column.IsPrimaryKey;
        }

        public override string GetColumnName(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            if (mm != null && mm.Column != null && !string.IsNullOrEmpty(mm.Column.Name))
                return mm.Column.Name;
            return base.GetColumnName(entity, member);
        }

        public override string GetColumnDbType(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            if (mm != null && mm.Column != null && !string.IsNullOrEmpty(mm.Column.DbType))
                return mm.Column.DbType;
            return null;
        }

        public override bool IsAssociationRelationship(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            return mm != null && mm.Association != null;        
        }

        public override bool IsRelationshipSource(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            if (mm != null && mm.Association != null)
            {
                if (mm.Association.IsForeignKey && !typeof(IEnumerable).IsAssignableFrom(TypeHelper.GetMemberType(member)))
                    return true;
            }
            return false;
        }

        public override bool IsRelationshipTarget(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            if (mm != null && mm.Association != null)
            {
                if (!mm.Association.IsForeignKey || typeof(IEnumerable).IsAssignableFrom(TypeHelper.GetMemberType(member)))
                    return true;
            }
            return false;
        }

        public override bool IsNestedEntity(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            return mm != null && mm.NestedEntity != null;
        }

        public override MappingEntity GetRelatedEntity(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingEntity thisEntity = (AttributeMappingEntity)entity;
            AttributeMappingMember mm = thisEntity.GetMappingMember(member.Name);

            if (mm != null)
            {
                if (mm.Association != null)
                {
                    Type relatedEntityType = TypeHelper.GetElementType(TypeHelper.GetMemberType(member));
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

        public override IEnumerable<MemberInfo> GetAssociationKeyMembers(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingEntity thisEntity = (AttributeMappingEntity)entity;
            AttributeMappingMember mm = thisEntity.GetMappingMember(member.Name);

            if (mm != null && mm.Association != null)
            {
                return this.GetReferencedMembers(thisEntity, mm.Association.KeyMembers, "Association.KeyMembers", thisEntity.StaticType);
            }

            return base.GetAssociationKeyMembers(entity, member);
        }

        public override IEnumerable<MemberInfo> GetAssociationRelatedKeyMembers(MappingEntity entity, MemberInfo member)
        {
            AttributeMappingEntity thisEntity = (AttributeMappingEntity)entity;
            AttributeMappingEntity relatedEntity = (AttributeMappingEntity)this.GetRelatedEntity(entity, member);
            AttributeMappingMember mm = thisEntity.GetMappingMember(member.Name);

            if (mm != null && mm.Association != null)
            {
                return this.GetReferencedMembers(relatedEntity, mm.Association.RelatedKeyMembers ?? mm.Association.KeyMembers, "Association.RelatedKeyMembers", thisEntity.StaticType);
            }

            return base.GetAssociationRelatedKeyMembers(entity, member);
        }

        private IEnumerable<MemberInfo> GetReferencedMembers(AttributeMappingEntity entity, string names, string source, Type sourceType)
        {
            return names.Split(separators).Select(n => this.GetReferencedMember(entity, n, source, sourceType));
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

        public override IList<MappingTable> GetTables(MappingEntity entity)
        {
            return ((AttributeMappingEntity)entity).Tables;
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
            AttributeMappingMember mm = ((AttributeMappingEntity)entity).GetMappingMember(member.Name);
            if (mm != null && mm.Column != null && !string.IsNullOrEmpty(mm.Column.TableId))
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

        public override IEnumerable<string> GetExtensionKeyColumnNames(MappingTable table)
        {
            var attr = ((AttributeMappingTable)table).Attribute as ExtensionTableAttribute;
            if (attr == null) return new string[] { };
            return attr.KeyColumns.Split(separators);
        }

        public override IEnumerable<MemberInfo> GetExtensionRelatedMembers(MappingTable table)
        {
            var amt = (AttributeMappingTable)table;
            var attr = amt.Attribute as ExtensionTableAttribute;
            if (attr == null) return new MemberInfo[] { };
            var relatedKeyColumns = attr.RelatedKeyColumns ?? attr.KeyColumns;
            return relatedKeyColumns.Split(separators).Select(n => this.GetMemberForColumn(amt.Entity, n));
        }

        private MemberInfo GetMemberForColumn(MappingEntity entity, string columnName)
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

        public override QueryMapper CreateMapper(QueryTranslator translator)
        {
            return new AttributeMapper(this, translator);
        }

        private class AttributeMapper : AdvancedMapper
        {
            private readonly AttributeMapping mapping;

            public AttributeMapper(AttributeMapping mapping, QueryTranslator translator)
                : base(mapping, translator)
            {
                this.mapping = mapping;
            }
        }

        private class AttributeMappingMember
        {
            private readonly MemberInfo member;
            private readonly MemberAttribute attribute;
            private readonly AttributeMappingEntity nested;

            public AttributeMappingMember(MemberInfo member, MemberAttribute attribute, AttributeMappingEntity nested)
            {
                this.member = member;
                this.attribute = attribute;
                this.nested = nested;
            }

            internal MemberInfo Member
            {
                get { return this.member; }
            }

            internal ColumnAttribute Column
            {
                get { return this.attribute as ColumnAttribute; }
            }

            internal AssociationAttribute Association
            {
                get { return this.attribute as AssociationAttribute; }
            }

            internal AttributeMappingEntity NestedEntity
            {
                get { return this.nested; }
            }
        }

        private class AttributeMappingTable : MappingTable
        {
            private readonly AttributeMappingEntity entity;
            private readonly TableBaseAttribute attribute;

            public AttributeMappingTable(AttributeMappingEntity entity, TableBaseAttribute attribute)
            {
                this.entity = entity;
                this.attribute = attribute;
            }

            public AttributeMappingEntity Entity
            {
                get { return this.entity; }
            }

            public TableBaseAttribute Attribute
            {
                get { return this.attribute; }
            }
        }

        private class AttributeMappingEntity : MappingEntity
        {
            private readonly Type staticType;
            private readonly Type runtimeType;
            private readonly string entityId;
            private readonly ReadOnlyCollection<MappingTable> tables;
            private readonly Dictionary<string, AttributeMappingMember> mappingMembers;

            public AttributeMappingEntity(Type staticType, Type runtimeType, string entityId, IEnumerable<TableBaseAttribute> tableAttributes, IEnumerable<AttributeMappingMember> mappingMembers)
            {
                this.staticType = staticType;
                this.runtimeType = runtimeType;
                this.entityId = entityId;
                this.tables = tableAttributes.Select(a => (MappingTable)new AttributeMappingTable(this, a)).ToReadOnly();
                this.mappingMembers = mappingMembers.ToDictionary(mm => mm.Member.Name);
            }

            public override Type StaticType
            {
                get { return this.staticType; }
            }

            public override Type RuntimeType
            {
                get { return this.runtimeType; }
            }

            public override string EntityId
            {
                get { return this.entityId; }
            }

            internal ReadOnlyCollection<MappingTable> Tables
            {
                get { return this.tables; }
            }

            internal AttributeMappingMember GetMappingMember(string name)
            {
                AttributeMappingMember mm = null;
                this.mappingMembers.TryGetValue(name, out mm);
                return mm;
            }

            internal IEnumerable<MemberInfo> MappedMembers
            {
                get { return this.mappingMembers.Values.Select(mm => mm.Member); }
            }
        }
    }
}
