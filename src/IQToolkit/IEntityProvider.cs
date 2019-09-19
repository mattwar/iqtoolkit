// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit
{
    /// <summary>
    /// An <see cref="IQueryProvider"/> for database entities.
    /// </summary>
    public interface IEntityProvider : IQueryProvider
    {
        /// <summary>
        /// Gets a <see cref="IEntityTable{TEntity}"/> for the entity type.
        /// </summary>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        IEntityTable<TEntity> GetTable<TEntity>(string entityId = null);

        /// <summary>
        /// Gets a <see cref="IEntityTable{TEntity}"/> for the entity type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        IEntityTable GetTable(Type entityType, string entityId = null);

        /// <summary>
        /// True if the expression can be evaluated locally (client-side)
        /// </summary>
        bool CanBeEvaluatedLocally(Expression expression);

        /// <summary>
        /// True if the expression can be isolated as a parameter.
        /// </summary>
        bool CanBeParameter(Expression expression);
    }

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
        /// The ID of the table. Used for determining mapping.
        /// </summary>
        string EntityId { get; }

        /// <summary>
        /// Gets an instance of an entity that corresponds to the specific id.
        /// </summary>
        object GetById(object id);

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
    public interface IEntityTable<T> : IQueryable<T>, IEntityTable, IUpdatable<T>
    {
        /// <summary>
        /// Gets an instance of an entity that corresponds to the specific id.
        /// </summary>
        new T GetById(object id);

        /// <summary>
        /// Inserts the entity instance into the table.
        /// </summary>
        int Insert(T instance);

        /// <summary>
        /// Update an instance that already exists in the table with the 
        /// values found in the specified instance.
        /// </summary>
        int Update(T instance);

        /// <summary>
        /// Delete the entity instance from the table that corresponds to the id of
        /// the entity specified.
        /// </summary>
        int Delete(T instance);

        /// <summary>
        /// Insert the entity into the table if an entity with the same id does not already exists,
        /// otherwise update the existing entitiy to have the same values as the specified instance.
        /// </summary>
        int InsertOrUpdate(T instance);
    }
}