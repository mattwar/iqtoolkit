// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Entities
{
    using IQToolkit.Expressions;
    using IQToolkit.Utils;
    using Mapping;

    /// <summary>
    /// An implementation of <see cref="IEntityTable{T}"/> for an <see cref="EntityProvider"/>.
    /// </summary>
    public class EntityTable<TEntity> 
        : Query<TEntity>, IUpdatableEntityTable<TEntity>, IHaveMappingEntity
        where TEntity : class
    {
        private readonly MappedEntity _entity;
        private readonly EntityProvider _provider;

        /// <summary>
        /// Construct an <see cref="EntityTable{T}"/>
        /// </summary>
        public EntityTable(EntityProvider provider, MappedEntity entity)
            : base(provider, typeof(IEntityTable<TEntity>))
        {
            _provider = provider;
            _entity = entity;
        }

        /// <summary>
        /// The <see cref="MappedEntity"/> corresponding to this table.
        /// </summary>
        public MappedEntity Entity => _entity;

        /// <summary>
        /// The <see cref="IEntityProvider"/> associated with this table.
        /// </summary>
        new public IEntityProvider Provider => _provider;

        /// <summary>
        /// The type of the table's entities.
        /// </summary>
        public Type EntityType => typeof(TEntity);

        /// <summary>
        /// The ID of the database table.
        /// </summary>
        public string EntityId => _entity.EntityId;

        /// <summary>
        /// Gets the entity from the database by its id (primary key value).
        /// </summary>
        public TEntity? GetById(object id)
        {
            var dbProvider = this.Provider;
            if (dbProvider != null)
            {
                var keys = id as IEnumerable<object>;
                if (keys == null)
                    keys = new object[] { id };
                var query = this.GetPrimaryKeyQuery(_entity, this.Expression, keys.Select(v => Expression.Constant(v)).ToArray());
                return this.Provider.Execute<TEntity>(query);
            }
            return null;
        }

        object? IEntityTable.GetById(object id)
        {
            return this.GetById(id);
        }

        private Expression GetPrimaryKeyQuery(MappedEntity entity, Expression source, Expression[] keys)
        {
            // make predicate
            var p = Expression.Parameter(entity.Type, "p");
            Expression? pred = null;

            var idMembers = entity.PrimaryKeyMembers;
            if (idMembers.Count != keys.Length)
            {
                throw new InvalidOperationException("Incorrect number of primary key values");
            }

            for (int i = 0, n = keys.Length; i < n; i++)
            {
                var mem = idMembers[i];
                var memberType = TypeHelper.GetMemberType(mem.Member);

                if (keys[i] != null 
                    && TypeHelper.GetNonNullableType(keys[i].Type) != TypeHelper.GetNonNullableType(memberType))
                {
                    throw new InvalidOperationException("Primary key value is wrong type");
                }

                var eq = Expression.MakeMemberAccess(p, mem.Member).Equal(keys[i]);
                pred = (pred == null) ? eq : pred.And(eq);
            }

            var predLambda = Expression.Lambda(pred, p);

            return Expression.Call(typeof(Queryable), "SingleOrDefault", new Type[] { entity.Type }, source, predLambda);
        }

        /// <summary>
        /// Inserts the entity instance into the database.
        /// </summary>
        public int Insert(TEntity instance)
        {
            return Updatable.Insert(this, instance);
        }

        int IUpdatableEntityTable.Insert(object instance)
        {
            return this.Insert((TEntity)instance);
        }

        /// <summary>
        /// Deletes the entity from the database.
        /// </summary>
        public int Delete(TEntity instance)
        {
            return Updatable.Delete(this, instance);
        }

        int IUpdatableEntityTable.Delete(object instance)
        {
            return this.Delete((TEntity)instance);
        }

        /// <summary>
        /// Updates the entity within the database.
        /// </summary>
        public int Update(TEntity instance)
        {
            return Updatable.Update(this, instance);
        }

        int IUpdatableEntityTable.Update(object instance)
        {
            return this.Update((TEntity)instance);
        }

        /// <summary>
        /// Inserts the entity into the database or update if it already exits.
        /// </summary>
        public int InsertOrUpdate(TEntity instance)
        {
            return Updatable.InsertOrUpdate(this, instance);
        }

        int IUpdatableEntityTable.InsertOrUpdate(object instance)
        {
            return this.InsertOrUpdate((TEntity)instance);
        }
    }
}