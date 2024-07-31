// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data
{
    using Mapping;
    using Utils;

    /// <summary>
    /// Defines mapping information and rules for the entity provider.
    /// </summary>
    public abstract class EntityMapping
    {
        /// <summary>
        /// Determines the entity Id based on the type of the entity alone.
        /// </summary>
        public virtual string GetEntityId(Type entityType)
        {
            return entityType.Name;
        }

        /// <summary>
        /// Gets the <see cref="MappingEntity"/> given the table
        /// </summary>
        public virtual MappingEntity GetEntity(IEntityTable table)
        {
            return this.GetEntity(table.EntityType, table.EntityId);
        }

        /// <summary>
        /// Get the <see cref="MappingEntity"/> corresponding to the CLR type
        /// </summary>
        public virtual MappingEntity GetEntity(Type entityType)
        {
            return this.GetEntity(entityType, this.GetEntityId(entityType));
        }

        /// <summary>
        /// Get the meta entity that maps between the CLR type 'entityType' and the database table, yet
        /// is represented publicly as 'elementType'.
        /// </summary>
        public abstract MappingEntity GetEntity(Type elementType, string? entityId);

        /// <summary>
        /// Get the meta entity represented by the IQueryable context member
        /// </summary>
        public abstract MappingEntity GetEntity(MemberInfo contextMember);

        /// <summary>
        /// Gets the members mapped by the <see cref="MappingEntity"/>
        /// </summary>
        public abstract IReadOnlyList<MemberInfo> GetMappedMembers(MappingEntity entity);

        /// <summary>
        /// True if the member is part of the entity's primary key.
        /// </summary>
        public abstract bool IsPrimaryKey(MappingEntity entity, MemberInfo member);

        /// <summary>
        /// Gets all members that make up the primary key of the entity.
        /// </summary>
        public virtual IReadOnlyList<MemberInfo> GetPrimaryKeyMembers(MappingEntity entity)
        {
            if (_primaryKeyMembers == null)
            {
                var tmp = this.GetMappedMembers(entity).Where(m => this.IsPrimaryKey(entity, m)).ToReadOnly();
                System.Threading.Interlocked.CompareExchange(ref _primaryKeyMembers, tmp, null);
            }

            return _primaryKeyMembers;
        }

        private IReadOnlyList<MemberInfo>? _primaryKeyMembers;

        /// <summary>
        /// Determines if a property is mapped as a relationship
        /// </summary>
        public abstract bool IsRelationship(MappingEntity entity, MemberInfo member);

        /// <summary>
        /// Determines if a relationship property refers to a single entity (as opposed to a collection.)
        /// </summary>
        public virtual bool IsSingletonRelationship(MappingEntity entity, MemberInfo member)
        {
            if (!this.IsRelationship(entity, member))
                return false;
            return TypeHelper.IsSequenceType(TypeHelper.GetMemberType(member));
        }

        /// <summary>
        /// True if a property is computed after insert or update.
        /// </summary>
        public virtual bool IsComputed(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        /// True if a property value is generated on the server during insert.
        /// </summary>
        public virtual bool IsGenerated(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        /// True if a property should not be updated.
        /// </summary>
        public virtual bool IsReadOnly(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        /// True if a property can be part of an update operation
        /// </summary>
        public virtual bool IsUpdatable(MappingEntity entity, MemberInfo member)
        {
            return !this.IsPrimaryKey(entity, member) && !this.IsReadOnly(entity, member);
        }

        /// <summary>
        /// Determines whether a given expression can be executed locally. 
        /// (It contains no parts that should be translated to the target environment.)
        /// </summary>
        public virtual bool CanBeEvaluatedLocally(Expression expression)
        {
            // any operation on a query can't be done locally
            if (expression is ConstantExpression cex
                && cex.Value is IQueryable query 
                && query.Provider == this)
            {
                return false;
            }

            if (expression is MethodCallExpression mc
                && (mc.Method.DeclaringType == typeof(Enumerable) 
                    || mc.Method.DeclaringType == typeof(Queryable) 
                    || mc.Method.DeclaringType == typeof(Updatable)))
            {
                return false;
            }

            if (expression.NodeType == ExpressionType.Convert 
                && expression.Type == typeof(object))
            {
                return true;
            }

            return expression.NodeType != ExpressionType.Parameter 
                && expression.NodeType != ExpressionType.Lambda;
        }

        /// <summary>
        /// Gets a value representing the primary key of the entity instance.
        /// </summary>
        public abstract object? GetPrimaryKey(MappingEntity entity, object instance);

        /// <summary>
        /// Gets a query that selects the primary key of an entity as an array of values.
        /// </summary>
        public abstract Expression? GetPrimaryKeyQuery(MappingEntity entity, Expression source, Expression[] keys);

        /// <summary>
        /// Gets the entity instances that this instance depends on.
        /// </summary>
        public abstract IEnumerable<EntityInfo> GetDependentEntities(MappingEntity entity, object instance);

        /// <summary>
        /// Gets the entity instances that depend on this instance.
        /// </summary>
        public abstract IEnumerable<EntityInfo> GetDependingEntities(MappingEntity entity, object instance);

        /// <summary>
        /// Create a shallow copy of an entity instance.
        /// </summary>
        public abstract object CloneEntity(MappingEntity entity, object instance);

        /// <summary>
        /// Returns true if the entity instance has been changed relative to the original instance.
        /// </summary>
        public abstract bool IsModified(MappingEntity entity, object instance, object original);
    }
}