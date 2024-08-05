// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

namespace IQToolkit.Entities
{
    using Factories;
    using Mapping;
    using Utils;

    /// <summary>
    /// An abstract base for an implementation of <see cref="IEntityProvider"/>
    /// </summary>
    public class EntityProvider : QueryProvider, IEntityProvider
    {
        /// <summary>
        /// The <see cref="QueryLanguage"/> used by the provider.
        /// </summary>
        public QueryLanguage Language { get; }

        /// <summary>
        /// The <see cref="EntityMapping"/> used by the provider.
        /// </summary>
        public EntityMapping Mapping { get; }

        /// <summary>
        /// The <see cref="QueryPolicy"/> used by the provider.
        /// </summary>
        public QueryPolicy Policy { get; }

        /// <summary>
        /// The <see cref="QueryExecutor"/> used by the provider.
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
        /// The <see cref="QueryOptions"/> used.
        /// </summary>
        public QueryOptions Options { get; }

        private readonly Dictionary<MappedEntity, IUpdatableEntityTable> _entityToEntityTableMap;

        protected EntityProvider(
            QueryExecutor executor,
            QueryLanguage? language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache,
            QueryOptions? options)
        {
            this.Language = language ?? AnsiSql.AnsiSqlLanguage.Singleton;
            this.Mapping = mapping ?? new AttributeMapping();
            this.Policy = policy ?? EntityPolicy.Default;
            this.Executor = executor;
            this.Log = log;
            this.Cache = cache;
            this.Options = options ?? QueryOptions.Default;

            _entityToEntityTableMap = new Dictionary<MappedEntity, IUpdatableEntityTable>();
        }

        /// <summary>
        /// Creates a new <see cref="EntityProvider"/> with the <see cref="Executor"/> property assigned.
        /// </summary>
        public EntityProvider WithExecutor(QueryExecutor executor) =>
            With(executor: executor);

        /// <summary>
        /// Creates a new <see cref="EntityProvider"/> with the <see cref="Language"/> property assigned.
        /// </summary>
        public EntityProvider WithLanguage(QueryLanguage language) =>
            With(language: language);

        /// <summary>
        /// Creates a new <see cref="EntityProvider"/> with the <see cref="Mapping"/> property assigned.
        /// </summary>
        public EntityProvider WithMapping(EntityMapping mapping) =>
            With(mapping: mapping);

        /// <summary>
        /// Creates a new <see cref="EntityProvider"/> with the <see cref="Policy"/> property assigned.
        /// </summary>
        public EntityProvider WithPolicy(QueryPolicy policy) =>
            With(policy: policy);

        /// <summary>
        /// Creates a new <see cref="EntityProvider"/> with the <see cref="Log"/> property assigned.
        /// </summary>
        public EntityProvider WithLog(TextWriter? log) =>
            With(log: log);

        /// <summary>
        /// Creates a new <see cref="EntityProvider"/> with the <see cref="Cache"/> property assigned.
        /// </summary>
        public EntityProvider WithCache(QueryCache? cache) =>
            With(cache: cache);

        /// <summary>
        /// Creates a new <see cref="EntityProvider"/> with the <see cref="Option"/> property assigned.
        /// </summary>
        public EntityProvider WithOptions(QueryOptions options) =>
            With(options: options);

        #region IEntityProvider
        IEntityProvider IEntityProvider.WithLanguage(QueryLanguage language) =>
            With(language: language);

        IEntityProvider IEntityProvider.WithMapping(EntityMapping mapping) =>
            With(mapping: mapping);

        IEntityProvider IEntityProvider.WithPolicy(QueryPolicy policy) =>
            With(policy: policy);

        IEntityProvider IEntityProvider.WithLog(TextWriter? log) =>
            With(log: log);

        IEntityProvider IEntityProvider.WithCache(QueryCache? cache) =>
            With(cache: cache);

        IEntityProvider IEntityProvider.WithOptions(QueryOptions options) =>
            With(options: options);
        #endregion

        protected virtual EntityProvider Construct(
            QueryExecutor executor,
            QueryLanguage language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache,
            QueryOptions? options)
        {
            return new EntityProvider(executor, language, mapping, policy, log, cache, options);
        }

        protected virtual EntityProvider With(
            QueryLanguage? language = null,
            EntityMapping? mapping = null,
            QueryPolicy? policy = null,
            QueryExecutor? executor = null,
            Optional<TextWriter?> log = default,
            Optional<QueryCache?> cache = default,
            QueryOptions? options = null)
        {
            var newLanguage = language ?? this.Language;
            var newMapping = mapping ?? this.Mapping;
            var newPolicy = policy ?? this.Policy;
            var newExecutor = executor ?? this.Executor;
            var newLog = log.HasValue ? log.Value : this.Log;
            var newCache = cache.HasValue ? cache.Value : this.Cache;
            var newOptions = options ?? this.Options;

            if (newLanguage != this.Language
                || newMapping != this.Mapping
                || newPolicy != this.Policy
                || newLog != this.Log
                || newCache != this.Cache
                || newOptions != this.Options)
            {
                return Construct(
                    newExecutor.WithLog(newLog),
                    newLanguage,
                    newMapping,
                    newPolicy,
                    newLog,
                    newCache,
                    newOptions
                    );
            }

            return this;
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable"/> for the entity.
        /// </summary>
        protected IUpdatableEntityTable GetTable(MappedEntity entity)
        {
            IUpdatableEntityTable table;

            if (!_entityToEntityTableMap.TryGetValue(entity, out table))
            {
                table = this.CreateTable(entity);
                _entityToEntityTableMap.Add(entity, table);
            }

            return table;
        }

        /// <summary>
        /// Creates the <see cref="IEntityTable"/> for the entity.
        /// </summary>
        protected virtual IUpdatableEntityTable CreateTable(MappedEntity entity)
        {
            return (IUpdatableEntityTable)Activator.CreateInstance(
                typeof(EntityTable<>).MakeGenericType(entity.Type),
                new object[] { this, entity }
                );
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable{T}"/> for the database table corresponding to the entity type.
        /// </summary>
        public virtual IUpdatableEntityTable<TEntity> GetTable<TEntity>()
            where TEntity : class
        {
            return GetTable<TEntity>(null);
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable{TEntity}"/> for the entity type.
        /// </summary>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        public virtual IUpdatableEntityTable<TEntity> GetTable<TEntity>(string? entityId = null)
            where TEntity : class
        {
            return (IUpdatableEntityTable<TEntity>)this.GetTable(typeof(TEntity), entityId);
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable"/> for entity type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        public virtual IUpdatableEntityTable GetTable(Type entityType, string? entityId = null)
        {
            return this.GetTable(this.Mapping.GetEntity(entityType, entityId));
        }

        /// <summary>
        /// True if the expression can be evaluated locally.
        /// </summary>
        public bool CanBeEvaluatedLocally(Expression expression)
        {
            return this.Language.CanBeEvaluatedLocally(expression);
        }

        /// <summary>
        /// True if the expression can be encoded as a parameter.
        /// </summary>
        public virtual bool CanBeParameter(Expression expression)
        {
            Type type = TypeHelper.GetNonNullableType(expression.Type);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (expression.Type == typeof(Byte[]) ||
                        expression.Type == typeof(Char[]))
                        return true;
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Execute the query expression and return the result.
        /// This API will cause the connection to be opened, and then closed after the action is complete.
        /// </summary>
        public override object? Execute(Expression expression)
        {
            if (expression is LambdaExpression lambda
                && this.Cache != null
                && expression.NodeType != ExpressionType.Constant)
            {
                return this.Cache.Execute(expression);
            }

            var plan = this.GetQueryPlan(expression);
            Console.WriteLine(plan.QueryText);
            return this.ExecutePlan(plan);
        }

        /// <summary>
        /// Execute the <see cref="QueryPlan"/> and return the result.
        /// </summary>
        public virtual object? ExecutePlan(
            QueryPlan plan)
        {
#if DEBUG
            var dbgText = plan.ExecutorDebugText;
#endif

            if (plan.Diagnostics.Count == 0)
            {
                if (plan.Executor is LambdaExpression execLambda)
                {
                    // compile & return the execution plan so it can be used multiple times
                    //var fn = Expression.Lambda(execLambda.Type, plan.Executor, execLambda.Parameters);
                    return execLambda.Compile();
                }
                else
                {
                    // compile the execution plan
                    var fnLambda = Expression.Lambda<Func<object>>(
                        Expression.Convert(plan.Executor, typeof(object))
                        );
                    var fn = fnLambda.Compile();
                    return fn();
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"The query contains errors: {plan.Diagnostics[0].Message}"
                    );
            }
        }

        /// <summary>
        /// Gets the query plan for the query.
        /// </summary>
        public virtual QueryPlan GetQueryPlan(Expression query)
        {
            return this.Language.GetQueryPlan(query, this);
        }

        #region Factory Nonsense
        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the connection string.
        /// </summary>
        public static IEntityProvider CreateForConnection(string connectionString) =>
            EntityProviderFactoryRegistry.Singleton.CreateProviderForConnection(connectionString);

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the file path.
        /// </summary>
        public static IEntityProvider CreateForFilePath(string filePath) =>
            EntityProviderFactoryRegistry.Singleton.CreateProviderForFilePath(filePath);

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the connection string, 
        /// if the connection string is compatible.
        /// </summary>
        public static bool TryCreateForConnection(string connectionString, out IEntityProvider provider) =>
            EntityProviderFactoryRegistry.Singleton.TryCreateProviderForConnection(connectionString, out provider);

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the file path, 
        /// if the file path is compatible.
        /// </summary>
        public static bool TryCreateForFilePath(string filePath, out IEntityProvider provider) =>
            EntityProviderFactoryRegistry.Singleton.TryCreateProviderForFilePath(filePath, out provider);
        #endregion
    }
}
