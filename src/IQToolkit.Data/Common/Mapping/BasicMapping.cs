// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// A base class for <see cref="QueryMapping"/> that support basic table, column and association mappings.
    /// </summary>
    public abstract class BasicMapping : QueryMapping
    {
        protected BasicMapping()
        {
        }

        public override MappingEntity GetEntity(Type elementType, string entityId)
        {
            if (entityId == null)
                entityId = this.GetEntityId(elementType);

            return new BasicMappingEntity(elementType, entityId);
        }

        public override MappingEntity GetEntity(MemberInfo contextMember)
        {
            Type elementType = TypeHelper.GetElementType(TypeHelper.GetMemberType(contextMember));
            return this.GetEntity(elementType);
        }

        private class BasicMappingEntity : MappingEntity
        {
            string entityID;
            Type type;

            public BasicMappingEntity(Type type, string entityID)
            {
                this.entityID = entityID;
                this.type = type;
            }

            public override string EntityId
            {
                get { return this.entityID; }
            }

            public override Type StaticType
            {
                get { return this.type; }
            }

            public override Type RuntimeType
            {
                get { return this.type; }
            }
        }

        public override bool IsRelationship(MappingEntity entity, MemberInfo member)
        {
            return this.IsAssociationRelationship(entity, member);
        }

        /// <summary>
        /// Deterimines is a property is mapped onto a column or relationship
        /// </summary>
        public virtual bool IsMapped(MappingEntity entity, MemberInfo member)
        {
            return true;
        }

        /// <summary>
        /// Determines if a property is mapped onto a column
        /// </summary>
        public virtual bool IsColumn(MappingEntity entity, MemberInfo member)
        {
            //return this.mapping.IsMapped(entity, member) && this.translator.Linguist.Language.IsScalar(TypeHelper.GetMemberType(member));
            return this.IsMapped(entity, member);
        }

        /// <summary>
        /// The type declaration for the column in the provider's syntax
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns>a string representing the type declaration or null</returns>
        public virtual string GetColumnDbType(MappingEntity entity, MemberInfo member)
        {
            return null;
        }

        /// <summary>
        /// Determines if a property represents or is part of the entities unique identity (often primary key)
        /// </summary>
        public override bool IsPrimaryKey(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        /// The type of the entity on the other side of the relationship
        /// </summary>
        public virtual MappingEntity GetRelatedEntity(MappingEntity entity, MemberInfo member)
        {
            Type relatedType = TypeHelper.GetElementType(TypeHelper.GetMemberType(member));
            return this.GetEntity(relatedType);
        }

        /// <summary>
        /// Determines if the property is an assocation relationship.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool IsAssociationRelationship(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        /// Returns the key members on this side of the association
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual IEnumerable<MemberInfo> GetAssociationKeyMembers(MappingEntity entity, MemberInfo member)
        {            
            return new MemberInfo[] { };
        }

        /// <summary>
        /// Returns the key members on the other side (related side) of the association
        /// </summary>
        public virtual IEnumerable<MemberInfo> GetAssociationRelatedKeyMembers(MappingEntity entity, MemberInfo member)
        {
            return new MemberInfo[] { };
        }

        public abstract bool IsRelationshipSource(MappingEntity entity, MemberInfo member);

        public abstract bool IsRelationshipTarget(MappingEntity entity, MemberInfo member);

        /// <summary>
        /// The name of the corresponding database table
        /// </summary>
        public virtual string GetTableName(MappingEntity entity)
        {
            return entity.StaticType.Name;
        }

        /// <summary>
        /// The name of the corresponding table column
        /// </summary>
        public virtual string GetColumnName(MappingEntity entity, MemberInfo member)
        {
            return member.Name;
        }

        /// <summary>
        /// A sequence of all the mapped members
        /// </summary>
        public override IEnumerable<MemberInfo> GetMappedMembers(MappingEntity entity)
        {
            Type type = entity.StaticType;
            HashSet<MemberInfo> members = new HashSet<MemberInfo>(type.GetTypeInfo().DeclaredFields.Where(m => this.IsMapped(entity, m)));
            members.UnionWith(type.GetTypeInfo().DeclaredProperties.Where(m => this.IsMapped(entity, m)));
            return members.OrderBy(m => m.Name);
        }

        public override object CloneEntity(MappingEntity entity, object instance)
        {
            var clone = TypeHelper.GetUninitializedObject(entity.RuntimeType);

            foreach (var mi in this.GetMappedMembers(entity))
            {
                if (this.IsColumn(entity, mi))
                {
                    mi.SetValue(clone, mi.GetValue(instance));
                }
            }

            return clone;
        }

        public override bool IsModified(MappingEntity entity, object instance, object original)
        {
            foreach (var mi in this.GetMappedMembers(entity))
            {
                if (this.IsColumn(entity, mi))
                {
                    if (!object.Equals(mi.GetValue(instance), mi.GetValue(original)))
                        return true;
                }
            }
            return false;
        }

        public override object GetPrimaryKey(MappingEntity entity, object instance)
        {
            object firstKey = null;
            List<object> keys = null;
            foreach (var mi in this.GetPrimaryKeyMembers(entity))
            {
                if (firstKey == null)
                {
                    firstKey = mi.GetValue(instance);
                }
                else
                {
                    if (keys == null)
                    {
                        keys = new List<object>();
                        keys.Add(firstKey);
                    }
                    keys.Add(mi.GetValue(instance));
                }
            }
            if (keys != null)
            {
                return new CompoundKey(keys.ToArray());
            }
            return firstKey;
        }

        public override Expression GetPrimaryKeyQuery(MappingEntity entity, Expression source, Expression[] keys)
        {
            // make predicate
            ParameterExpression p = Expression.Parameter(entity.StaticType, "p");
            Expression pred = null;

            var idMembers = this.GetPrimaryKeyMembers(entity).ToList();
            if (idMembers.Count != keys.Length)
            {
                throw new InvalidOperationException("Incorrect number of primary key values");
            }

            for (int i = 0, n = keys.Length; i < n; i++)
            {
                MemberInfo mem = idMembers[i];
                Type memberType = TypeHelper.GetMemberType(mem);

                if (keys[i] != null && TypeHelper.GetNonNullableType(keys[i].Type) != TypeHelper.GetNonNullableType(memberType))
                {
                    throw new InvalidOperationException("Primary key value is wrong type");
                }

                Expression eq = Expression.MakeMemberAccess(p, mem).Equal(keys[i]);
                pred = (pred == null) ? eq : pred.And(eq);
            }

            var predLambda = Expression.Lambda(pred, p);

            return Expression.Call(typeof(Queryable), "SingleOrDefault", new Type[] { entity.StaticType }, source, predLambda);
        }

        public override IEnumerable<EntityInfo> GetDependentEntities(MappingEntity entity, object instance)
        {
            foreach (var mi in this.GetMappedMembers(entity))
            {
                if (this.IsRelationship(entity, mi) && this.IsRelationshipSource(entity, mi))
                {
                    MappingEntity relatedEntity = this.GetRelatedEntity(entity, mi);
                    var value = mi.GetValue(instance);
                    if (value != null)
                    {
                        var list = value as IList;
                        if (list != null)
                        {
                            foreach (var item in list)
                            {
                                if (item != null)
                                {
                                    yield return new EntityInfo(item, relatedEntity);
                                }
                            }
                        }
                        else
                        {
                            yield return new EntityInfo(value, relatedEntity);
                        }
                    }
                }
            }
        }

        public override IEnumerable<EntityInfo> GetDependingEntities(MappingEntity entity, object instance)
        {
            foreach (var mi in this.GetMappedMembers(entity))
            {
                if (this.IsRelationship(entity, mi) && this.IsRelationshipTarget(entity, mi))
                {
                    MappingEntity relatedEntity = this.GetRelatedEntity(entity, mi);

                    var value = mi.GetValue(instance);
                    if (value != null)
                    {
                        var list = value as IList;
                        if (list != null)
                        {
                            foreach (var item in list)
                            {
                                if (item != null)
                                {
                                    yield return new EntityInfo(item, relatedEntity);
                                }
                            }
                        }
                        else
                        {
                            yield return new EntityInfo(value, relatedEntity);
                        }
                    }
                }
            }
        }

        public override QueryMapper CreateMapper(QueryTranslator translator)
        {
            return new BasicMapper(this, translator);
        }
    }
}
