// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data
{
    using Mapping;

    /// <summary>
    /// An implementation of <see cref="IEntityTable{T}"/> for an <see cref="EntityProvider"/>.
    /// </summary>
    public class EntityTable<TEntity> : Query<TEntity>, IEntityTable<TEntity>, IHaveMappingEntity
        where TEntity : class
    {
        private readonly MappingEntity _entity;
        private readonly EntityProvider _provider;

        /// <summary>
        /// Construct an <see cref="EntityTable{T}"/>
        /// </summary>
        public EntityTable(EntityProvider provider, MappingEntity entity)
            : base(provider, typeof(IEntityTable<TEntity>))
        {
            _provider = provider;
            _entity = entity;
        }

        /// <summary>
        /// The <see cref="MappingEntity"/> corresponding to this table.
        /// </summary>
        public MappingEntity Entity => _entity;

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
                var query = ((EntityProvider)dbProvider).Mapping.GetPrimaryKeyQuery(_entity, this.Expression, keys.Select(v => Expression.Constant(v)).ToArray());
                return this.Provider.Execute<TEntity>(query);
            }
            return null;
        }

        object? IEntityTable.GetById(object id)
        {
            return this.GetById(id);
        }

        /// <summary>
        /// Inserts the entity instance into the database.
        /// </summary>
        public int Insert(TEntity instance)
        {
            return Updatable.Insert(this, instance);
        }

        int IEntityTable.Insert(object instance)
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

        int IEntityTable.Delete(object instance)
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

        int IEntityTable.Update(object instance)
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

        int IEntityTable.InsertOrUpdate(object instance)
        {
            return this.InsertOrUpdate((TEntity)instance);
        }
    }
}