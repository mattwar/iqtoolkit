// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq;

namespace IQToolkit.Entities
{
    /// <summary>
    /// A table of entities.
    /// </summary>
    public interface IEntityTable : IQueryable, IUpdatable
    {
        /// <summary>
        /// The <see cref="IEntityProvider"/> underlying the table.
        /// </summary>
        new IEntityProvider Provider { get; }

        /// <summary>
        /// The type of the entities in the table
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// The ID of the table. Used for determining mapping.
        /// </summary>
        string EntityId { get; }

        /// <summary>
        /// Gets an instance of an entity that corresponds to the specific id.
        /// </summary>
        object? GetById(object id);

        /// <summary>
        /// Inserts the entity instance into the table.
        /// </summary>
        int Insert(object instance);

        /// <summary>
        /// Update an instance that already exists in the table with the 
        /// values found in the specified instance.
        /// </summary>
        int Update(object instance);

        /// <summary>
        /// Delete the entity instance from the table that corresponds to the id of
        /// the entity specified.
        /// </summary>
        int Delete(object instance);

        /// <summary>
        /// Insert the entity into the table if an entity with the same id does not already exists,
        /// otherwise update the existing entitiy to have the same values as the specified instance.
        /// </summary>
        int InsertOrUpdate(object instance);
    }

    /// <summary>
    /// A table of entities.
    /// </summary>
    public interface IEntityTable<TEntity> : IQueryable<TEntity>, IEntityTable, IUpdatable<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Gets an instance of an entity that corresponds to the specific id.
        /// </summary>
        new TEntity? GetById(object id);

        /// <summary>
        /// Inserts the entity instance into the table.
        /// </summary>
        int Insert(TEntity instance);

        /// <summary>
        /// Update an instance that already exists in the table with the 
        /// values found in the specified instance.
        /// </summary>
        int Update(TEntity instance);

        /// <summary>
        /// Delete the entity instance from the table that corresponds to the id of
        /// the entity specified.
        /// </summary>
        int Delete(TEntity instance);

        /// <summary>
        /// Insert the entity into the table if an entity with the same id does not already exists,
        /// otherwise update the existing entitiy to have the same values as the specified instance.
        /// </summary>
        int InsertOrUpdate(TEntity instance);
    }
}