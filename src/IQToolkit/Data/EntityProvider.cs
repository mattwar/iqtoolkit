// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using IQToolkit.Expressions;

namespace IQToolkit.Data
{
    using Execution;
    using Factories;
    using Mapping;
    using Translation;
    using Utils;

    /// <summary>
    /// A base type for LINQ IQueryable query providers that executes translated queries against a database.
    /// </summary>
    public class EntityProvider : QueryProvider, IEntityProvider, IHaveExecutor
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

        private readonly Dictionary<MappingEntity, IEntityTable> _entityToEntityTableMap;

        public EntityProvider(
            QueryExecutor executor,
            QueryLanguage? language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log = null,
            QueryCache? cache = null)
        {
            this.Language = language ?? AnsiSql.AnsiSqlLanguage.Singleton;
            this.Mapping = mapping ?? new AttributeEntityMapping();
            this.Policy = policy ?? QueryPolicy.Default;
            this.Executor = executor;
            this.Log = log;
            this.Cache = cache;

            _entityToEntityTableMap = new Dictionary<MappingEntity, IEntityTable>();
        }

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the connection string.
        /// </summary>
        public static EntityProvider CreateForConnection(string connectionString) =>
            EntityProviderFactoryRegistry.Singleton.CreateProviderForConnection(connectionString);

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the file path.
        /// </summary>
        public static EntityProvider CreateForFilePath(string filePath) =>
            EntityProviderFactoryRegistry.Singleton.CreateProviderForFilePath(filePath);

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the connection string, 
        /// if the connection string is compatible.
        /// </summary>
        public static bool TryCreateForConnection(string connectionString, out EntityProvider provider) =>
            EntityProviderFactoryRegistry.Singleton.TryCreateProviderForConnection(connectionString, out provider);

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the file path, 
        /// if the file path is compatible.
        /// </summary>
        public static bool TryCreateForFilePath(string filePath, out EntityProvider provider) =>
            EntityProviderFactoryRegistry.Singleton.TryCreateProviderForFilePath(filePath, out provider);

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

        IEntityProvider IEntityProvider.WithMapping(EntityMapping mapping) =>
            this.WithMapping(mapping);

        EntityMapping IEntityProvider.Mapping => this.Mapping;

        /// <summary>
        /// Creates a new <see cref="EntityProvider"/> with the <see cref="Policy"/> property assigned.
        /// </summary>
        public EntityProvider WithPolicy(QueryPolicy policy) =>
            With(policy: policy);

        QueryPolicy IEntityProvider.Policy => this.Policy;

        IEntityProvider IEntityProvider.WithPolicy(QueryPolicy policy) =>
            this.WithPolicy(policy);

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

        protected virtual EntityProvider Construct(
            QueryExecutor executor,
            QueryLanguage language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache)
        {
            return new EntityProvider(executor, language, mapping, policy, log, cache);
        }

        protected virtual EntityProvider With(
            QueryLanguage? language = null,
            EntityMapping? mapping = null,
            QueryPolicy? policy = null,
            QueryExecutor? executor = null,
            Optional<TextWriter?> log = default,
            Optional<QueryCache?> cache = default)
        {
            var newLanguage = language ?? this.Language;
            var newMapping = mapping ?? this.Mapping;
            var newPolicy = policy ?? this.Policy;
            var newExecutor = executor ?? this.Executor;
            var newLog = log.HasValue ? log.Value : this.Log;
            var newCache = cache.HasValue ? cache.Value : this.Cache;

            if (newLanguage != this.Language
                || newMapping != this.Mapping
                || newPolicy != this.Policy
                || newLog != this.Log
                || newCache != this.Cache)
            {
                return Construct(
                    newExecutor.WithLog(newLog),
                    newLanguage,
                    newMapping,
                    newPolicy,
                    newLog,
                    newCache
                    );
            }

            return this;
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable"/> for the entity.
        /// </summary>
        protected IEntityTable GetTable(MappingEntity entity)
        {
            IEntityTable table;

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
        protected virtual IEntityTable CreateTable(MappingEntity entity)
        {
            return (IEntityTable)Activator.CreateInstance(
                typeof(EntityTable<>).MakeGenericType(entity.StaticType),
                new object[] { this, entity }
                );
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable{T}"/> for the database table corresponding to the entity type.
        /// </summary>
        public virtual IEntityTable<TEntity> GetTable<TEntity>()
            where TEntity : class
        {
            return GetTable<TEntity>(null);
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable{TEntity}"/> for the entity type.
        /// </summary>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        public virtual IEntityTable<TEntity> GetTable<TEntity>(string? entityId = null)
            where TEntity : class
        {
            return (IEntityTable<TEntity>)this.GetTable(typeof(TEntity), entityId);
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable"/> for entity type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        public virtual IEntityTable GetTable(Type entityType, string? entityId = null)
        {
            return this.GetTable(this.Mapping.GetEntity(entityType, entityId));
        }

        /// <summary>
        /// True if the expression can be evaluated locally.
        /// </summary>
        public bool CanBeEvaluatedLocally(Expression expression)
        {
            return this.Mapping.CanBeEvaluatedLocally(expression);
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
        /// Create a <see cref="QueryTranslator"/> used to translate a query into
        /// an execution plan with with parts both executed on the server and client.
        /// </summary>
        protected virtual QueryTranslator CreateTranslator()
        {
            return new QueryTranslator(
                t => CreateLanguageTranslator(t),
                t => CreateMappingTranslator(t),
                t => CreatePolicyTranslator(t)
                );
        }

        protected virtual QueryLanguageRewriter CreateLanguageTranslator(QueryTranslator translator)
        {
            if (this.Language is ICreateLanguageRewriter clt)
                return clt.CreateLanguageTranslator(translator);

            return new QueryLanguageRewriter(translator, this.Language);
        }

        protected virtual QueryMappingRewriter CreateMappingTranslator(QueryTranslator translator)
        {
            if (this.Mapping is ICreateMappingRewriter cmt)
                return cmt.CreateMappingTranslator(translator);

            if (this.Mapping is AdvancedEntityMapping advMapping)
            {
                return new AdvancedMappingRewriter(advMapping, translator);
            }
            else if (this.Mapping is BasicEntityMapping basicMapping)
            {
                return new BasicMappingRewriter(basicMapping, translator);
            }
            else
            {
                // TODO: create unknown mapping translator...
                throw new InvalidOperationException(
                    string.Format("Unhandled mapping kind: {0}", this.Mapping.GetType().Name)
                    );
            }
        }

        protected virtual QueryPolicyRewriter CreatePolicyTranslator(QueryTranslator translator)
        {
            if (this.Policy is ICreatePolicyRewriter cpt)
                return cpt.CreatePolicyTranslator(translator);

            if (this.Policy is EntityPolicy entityPolicy)
            {
                return new EntityPolicyRewritter(translator, entityPolicy);
            }
            else
            {
                return new QueryPolicyRewriter(translator, this.Policy);
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

            var plan = this.GetExecutionPlan(expression);
            Console.WriteLine(plan.QueryText);
            return this.ExecutePlan(plan);
        }

        /// <summary>
        /// Execute the <see cref="QueryPlan"/> and return the result.
        /// </summary>
        public virtual object? ExecutePlan(QueryPlan plan)
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
        /// Gets the execution plan for the query expression.
        /// </summary>
        public virtual QueryPlan GetExecutionPlan(Expression query)
        {
            // remove possible lambda and add back later
            var lambda = query as LambdaExpression;
            if (lambda != null)
                query = lambda.Body;

            var translator = this.CreateTranslator();

            // translate query into client & server parts
            var translation = translator.Translate(query);

            var parameters = lambda?.Parameters;
            var provider = this.Find(query, parameters, typeof(EntityProvider));
            if (provider == null)
            {
                var rootQueryable = this.Find(query, parameters, typeof(IQueryable));
                var providerProperty = typeof(IQueryable).GetProperty("Provider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                provider = Expression.Property(rootQueryable, providerProperty);
            }

            // add back lambda
            if (lambda != null)
                translation = lambda.Update(lambda.Type, translation, lambda.Parameters);

            // build the plan
            return QueryPlanBuilder.Build(
                translator.LanguageRewriter, 
                this.FormattingOptions,
                this.Policy, 
                translation, 
                provider
                );
        }

        protected virtual FormattingOptions FormattingOptions =>
            FormattingOptions.Default;

        /// <summary>
        /// Find the expression of the specified type, either in the specified expression or parameters.
        /// </summary>
        private Expression? Find(Expression expression, IReadOnlyList<ParameterExpression>? parameters, Type type)
        {
            if (parameters != null)
            {
                Expression found = parameters.FirstOrDefault(p => type.IsAssignableFrom(p.Type));
                if (found != null)
                    return found;
            }

            return expression.FindFirstUpOrDefault(expr => type.IsAssignableFrom(expr.Type));
        }
    }
}
