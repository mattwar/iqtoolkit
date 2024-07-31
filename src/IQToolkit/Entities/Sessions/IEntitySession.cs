// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Entities.Sessions
{
    /// <summary>
    /// Sessions track changes to entity instances 
    /// and enable submiting changes to multiple entities to the database in one atomic action. 
    /// </summary>
    public interface IEntitySession
    {
        /// <summary>
        /// The underlying <see cref="IEntityProvider"/> for this session.
        /// </summary>
        IEntityProvider Provider { get; }

        /// <summary>
        /// Gets the <see cref="ISessionTable{TEntity}"/> for the corresponding logical database table.
        /// </summary>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        ISessionTable<TEntity> GetTable<TEntity>(string? entityId = null)
            where TEntity : class;

        /// <summary>
        /// Gets the <see cref="ISessionTable"/> for the corresponding logical database table.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        ISessionTable GetTable(Type entityType, string? entityId = null);

        /// <summary>
        /// Submit all changes to the database as a single transaction.
        /// </summary>
        void SubmitChanges();
    }
}