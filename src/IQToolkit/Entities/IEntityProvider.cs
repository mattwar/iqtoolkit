// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Entities
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
        /// The <see cref="QueryLanguage"/>.
        /// </summary>
        public QueryLanguage Language { get; }

        /// <summary>
        /// The <see cref="EntityMapping"/>.
        /// </summary>
        public EntityMapping Mapping { get; }

        /// <summary>
        /// The <see cref="QueryPolicy"/>.
        /// </summary>
        public QueryPolicy Policy { get; }

        /// <summary>
        /// The <see cref="QueryExecutor"/> that executes the queries.
        /// </summary>
        public QueryExecutor Executor { get; }

        /// <summary>
        /// The <see cref="TextWriter"/> used for logging messages.
        /// </summary>
        public TextWriter? Log { get; }

        /// <summary>
        /// The <see cref="QueryCache"/> used to cache queries.
        /// </summary>
        public QueryCache? Cache { get; }

        /// <summary>
        /// The <see cref="QueryOptions"/>.
        /// </summary>
        public QueryOptions Options { get; }

        /// <summary>
        /// Returns a new <see cref="IEntityProvider"/> with the <see cref="Language"/> property assigned.
        /// </summary>
        public IEntityProvider WithLanguage(QueryLanguage language);

        /// <summary>
        /// Returns a new <see cref="IEntityProvider"/> with the <see cref="Mapping"/> property assigned.
        /// </summary>
        public IEntityProvider WithMapping(EntityMapping mapping);

        /// <summary>
        /// Returns a new <see cref="IEntityProvider"/> with the <see cref="Policy"/> property assigned.
        /// </summary>
        public IEntityProvider WithPolicy(QueryPolicy policy);

        /// <summary>
        /// Returns a new <see cref="IEntityProvider"/> with the <see cref="Log"/> property assigned.
        /// </summary>
        public IEntityProvider WithLog(TextWriter? log);

        /// <summary>
        /// Returns a new <see cref="IEntityProvider"/> with the <see cref="Cache"/> property assigned.
        /// </summary>
        public IEntityProvider WithCache(QueryCache? cache);

        /// <summary>
        /// Returns a new <see cref="IEntityProvider"/> with the <see cref="Options"/> property assigned.
        /// </summary>
        public IEntityProvider WithOptions(QueryOptions options);

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
        /// Gets the query plan for executing the query expression,
        /// including the translated queries.
        /// </summary>
        QueryPlan GetQueryPlan(Expression expression);
    }
}