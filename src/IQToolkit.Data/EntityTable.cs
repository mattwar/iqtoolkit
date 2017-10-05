// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data
{
    using Common;

    /// <summary>
    /// An implementation of <see cref="IEntityTable{T}"/> for an <see cref="EntityProvider"/>.
    /// </summary>
    public class EntityTable<T> : Query<T>, IEntityTable<T>, IHaveMappingEntity
    {
        private readonly MappingEntity entity;
        private readonly EntityProvider provider;

        /// <summary>
        /// Construct an <see cref="EntityTable{T}"/>
        /// </summary>
        public EntityTable(EntityProvider provider, MappingEntity entity)
            : base(provider, typeof(IEntityTable<T>))
        {
            this.provider = provider;
            this.entity = entity;
        }

        /// <summary>
        /// The <see cref="MappingEntity"/> corresponding to this table.
        /// </summary>
        public MappingEntity Entity
        {
            get { return this.entity; }
        }

        /// <summary>
        /// The <see cref="IEntityProvider"/> associated with this table.
        /// </summary>
        new public IEntityProvider Provider
        {
            get { return this.provider; }
        }

        /// <summary>
        /// The ID of the database table.
        /// </summary>
        public string EntityId
        {
            get { return this.entity.EntityId; }
        }

        /// <summary>
        /// Gets the entity from the database by its id (primary key value).
        /// </summary>
        public T GetById(object id)
        {
            var dbProvider = this.Provider;
            if (dbProvider != null)
            {
                IEnumerable<object> keys = id as IEnumerable<object>;
                if (keys == null)
                    keys = new object[] { id };
                Expression query = ((EntityProvider)dbProvider).Mapping.GetPrimaryKeyQuery(this.entity, this.Expression, keys.Select(v => Expression.Constant(v)).ToArray());
                return this.Provider.Execute<T>(query);
            }
            return default(T);
        }

        object IEntityTable.GetById(object id)
        {
            return this.GetById(id);
        }

        /// <summary>
        /// Inserts the entity instance into the database.
        /// </summary>
        public int Insert(T instance)
        {
            return Updatable.Insert(this, instance);
        }

        int IEntityTable.Insert(object instance)
        {
            return this.Insert((T)instance);
        }

        /// <summary>
        /// Deletes the entity from the database.
        /// </summary>
        public int Delete(T instance)
        {
            return Updatable.Delete(this, instance);
        }

        int IEntityTable.Delete(object instance)
        {
            return this.Delete((T)instance);
        }

        /// <summary>
        /// Updates the entity within the database.
        /// </summary>
        public int Update(T instance)
        {
            return Updatable.Update(this, instance);
        }

        int IEntityTable.Update(object instance)
        {
            return this.Update((T)instance);
        }

        /// <summary>
        /// Inserts the entity into the database or update if it already exits.
        /// </summary>
        public int InsertOrUpdate(T instance)
        {
            return Updatable.InsertOrUpdate(this, instance);
        }

        int IEntityTable.InsertOrUpdate(object instance)
        {
            return this.InsertOrUpdate((T)instance);
        }
    }
}