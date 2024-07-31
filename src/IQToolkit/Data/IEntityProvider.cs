// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data
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
        IEntityTable<TEntity> GetTable<TEntity>(string? entityId = null)
            where TEntity : class;

        /// <summary>
        /// Gets a <see cref="IEntityTable{TEntity}"/> for the entity type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        IEntityTable GetTable(Type entityType, string? entityId = null);

        /// <summary>
        /// The current mapping used by the provider.
        /// </summary>
        public EntityMapping Mapping { get; }

        /// <summary>
        /// The current policy used by the provider.
        /// </summary>
        public QueryPolicy Policy { get; }

        /// <summary>
        /// Returns a new <see cref="IEntityProvider"/> with the <see cref="Policy"/> property assigned.
        /// </summary>
        public IEntityProvider WithPolicy(QueryPolicy policy);

        /// <summary>
        /// Returns a new <see cref="IEntityProvider"/> with the <see cref="Mapping"/> property assigned.
        /// </summary>
        public IEntityProvider WithMapping(EntityMapping mapping);

        /// <summary>
        /// True if the expression can be evaluated locally (client-side)
        /// </summary>
        bool CanBeEvaluatedLocally(Expression expression);

        /// <summary>
        /// True if the expression can be isolated as a parameter.
        /// </summary>
        bool CanBeParameter(Expression expression);

        /// <summary>
        /// Executes the <see cref="QueryPlan"/>.
        /// </summary>
        object? ExecutePlan(QueryPlan plan);

        /// <summary>
        /// Gets the execution plan for the query expression,
        /// including the translated queries.
        /// </summary>
        QueryPlan GetExecutionPlan(Expression expression);
    }
}