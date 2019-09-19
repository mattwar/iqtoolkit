// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data
{
    using Common;
    using Mapping;

    /// <summary>
    /// A base type for LINQ IQueryable query providers that executes translated queries against a database.
    /// </summary>
    public abstract class EntityProvider : QueryProvider, IEntityProvider, IQueryExecutorFactory
    {
        private readonly QueryLanguage language;
        private readonly QueryMapping mapping;
        private readonly QueryPolicy policy;
        private readonly Dictionary<MappingEntity, IEntityTable> tables;
        private QueryCache cache;
        private TextWriter log;

        public EntityProvider(QueryLanguage language, QueryMapping mapping, QueryPolicy policy)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            this.language = language;
            this.mapping = mapping ?? new AttributeMapping();
            this.policy = policy ?? QueryPolicy.Default;
            this.tables = new Dictionary<MappingEntity, IEntityTable>();
        }

        /// <summary>
        /// The <see cref="QueryMapping"/> used by the provider.
        /// </summary>
        public QueryMapping Mapping
        {
            get { return this.mapping; }
        }

        /// <summary>
        /// The <see cref="QueryLanguage"/> used by the provider.
        /// </summary>
        public QueryLanguage Language
        {
            get { return this.language; }
        }

        /// <summary>
        /// The <see cref="QueryPolicy"/> used by the provider.
        /// </summary>
        public QueryPolicy Policy
        {
            get { return this.policy; }
        }

        /// <summary>
        /// The <see cref="TextWriter"/> used for logging messages.
        /// </summary>
        public TextWriter Log
        {
            get { return this.log; }
            set { this.log = value; }
        }

        /// <summary>
        /// The <see cref="QueryCache"/> used to cache queries.
        /// </summary>
        public QueryCache Cache
        {
            get { return this.cache; }
            set { this.cache = value; }
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable"/> for the entity.
        /// </summary>
        protected IEntityTable GetTable(MappingEntity entity)
        {
            IEntityTable table;

            if (!this.tables.TryGetValue(entity, out table))
            {
                table = this.CreateTable(entity);
                this.tables.Add(entity, table);
            }

            return table;
        }

        protected virtual IEntityTable CreateTable(MappingEntity entity)
        {
            return (IEntityTable) Activator.CreateInstance(
                typeof(EntityTable<>).MakeGenericType(entity.StaticType), 
                new object[] { this, entity }
                );
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable{T}"/> for the database table corresponding to the entity type.
        /// </summary>
        public virtual IEntityTable<T> GetTable<T>()
        {
            return GetTable<T>(null);
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable{TEntity}"/> for the entity type.
        /// </summary>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        public virtual IEntityTable<TEntity> GetTable<TEntity>(string entityId = null)
        {
            return (IEntityTable<TEntity>)this.GetTable(typeof(TEntity), entityId);
        }

        /// <summary>
        /// Gets the <see cref="IEntityTable"/> for entity type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        public virtual IEntityTable GetTable(Type entityType, string entityId = null)
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
            switch (TypeHelper.GetTypeCode(type))
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
        /// Create a <see cref="QueryExecutor"/> used to execute commands against the server.
        /// </summary>
        protected abstract QueryExecutor CreateExecutor();

        QueryExecutor IQueryExecutorFactory.CreateExecutor()
        {
            return this.CreateExecutor();
        }

        /// <summary>
        /// Gets the text of the individual commands that would be sent to the database to execute the query.
        /// Does not include any client-side projection or orchestration logic.
        /// </summary>
        public override string GetQueryText(Expression expression)
        {
            Expression plan = this.GetExecutionPlan(expression);
            var commands = CommandGatherer.Gather(plan).Select(c => c.CommandText).ToArray();
            return string.Join("\n\n", commands);
        }

        /// <summary>
        /// Finds all the <see cref="QueryCommand"/>'s in the expression (query plan).
        /// </summary>
        private class CommandGatherer : DbExpressionVisitor
        {
            private readonly List<QueryCommand> commands = new List<QueryCommand>();

            public static ReadOnlyCollection<QueryCommand> Gather(Expression expression)
            {
                var gatherer = new CommandGatherer();
                gatherer.Visit(expression);
                return gatherer.commands.AsReadOnly();
            }

            protected override Expression VisitConstant(ConstantExpression c)
            {
                QueryCommand qc = c.Value as QueryCommand;
                if (qc != null)
                {
                    this.commands.Add(qc);
                }
                return c;
            }
        }

        /// <summary>
        /// Gets text representing the entire query execution plan, including both server-side commands
        /// and client-side project and execution logic. For debugging purposes.
        /// </summary>
        public string GetQueryPlan(Expression expression)
        {
            Expression plan = this.GetExecutionPlan(expression);
            return DbExpressionWriter.WriteToString(this.Language, plan);
        }

        /// <summary>
        /// Create a <see cref="QueryTranslator"/> used to translate a query into
        /// an execution plan with with parts both executed on the server and client.
        /// </summary>
        protected virtual QueryTranslator CreateTranslator()
        {
            return new QueryTranslator(this.language, this.mapping, this.policy);
        }

        /// <summary>
        /// Execute the <see cref="Action"/> under a database transaction.
        /// This API will cause the transaction to be started, and then commited after the action is complete.
        /// </summary>
        public abstract void DoTransacted(Action action);

        /// <summary>
        /// Execute the <see cref="Action"/> while the database connection is open.
        /// This API will cause the connection to be opened, and then closed after the action is complete.
        /// </summary>
        /// <param name="action"></param>
        public abstract void DoConnected(Action action);

        /// <summary>
        /// Execute the database command specified in the database's natural language.
        /// This API will cause the connection to be opened, and then closed after the action is complete.
        /// </summary>
        public abstract int ExecuteCommand(string commandText);

        /// <summary>
        /// Execute the query expression and return the result.
        /// This API will cause the connection to be opened, and then closed after the action is complete.
        /// </summary>
        public override object Execute(Expression expression)
        {
            LambdaExpression lambda = expression as LambdaExpression;

            if (lambda == null && this.cache != null && expression.NodeType != ExpressionType.Constant)
            {
                return this.cache.Execute(expression);
            }

            var compiled = this.Compile(expression);
            return compiled();
        }

        private Func<object> Compile(Expression expression)
        {
            LambdaExpression lambda = expression as LambdaExpression;

            Expression plan = this.GetExecutionPlan(expression);

            if (lambda != null)
            {
                // compile & return the execution plan so it can be used multiple times
                LambdaExpression fn = Expression.Lambda(lambda.Type, plan, lambda.Parameters);
                var d = fn.Compile();
                return () => d;
            }
            else
            {
                // compile the execution plan
                Expression<Func<object>> efn = Expression.Lambda<Func<object>>(Expression.Convert(plan, typeof(object)));
                return efn.Compile();
            }
        }

        /// <summary>
        /// Convert the query expression into an execution plan, a single <see cref="Expression"/>
        /// that contains all the <see cref="QueryCommand"/>'s to be issued against the server and the 
        /// logic to execute on the client to convert the returned data into the expected element types.
        /// </summary>
        public virtual Expression GetExecutionPlan(Expression expression)
        {
            // strip off lambda for now
            LambdaExpression lambda = expression as LambdaExpression;
            if (lambda != null)
                expression = lambda.Body;

            QueryTranslator translator = this.CreateTranslator();

            // translate query into client & server parts
            Expression translation = translator.Translate(expression);

            var parameters = lambda != null ? lambda.Parameters : null;
            Expression provider = this.Find(expression, parameters, typeof(EntityProvider));
            if (provider == null)
            {
                Expression rootQueryable = this.Find(expression, parameters, typeof(IQueryable));
                provider = Expression.Property(rootQueryable, typeof(IQueryable).GetTypeInfo().GetDeclaredProperty("Provider"));
            }

            return translator.Police.BuildExecutionPlan(translation, provider);
        }

        /// <summary>
        /// Find the expression of the specified type, either in the specified expression or parameters.
        /// </summary>
        private Expression Find(Expression expression, IList<ParameterExpression> parameters, Type type)
        {
            if (parameters != null)
            {
                Expression found = parameters.FirstOrDefault(p => type.IsAssignableFrom(p.Type));
                if (found != null)
                    return found;
            }

            return TypedSubtreeFinder.Find(expression, type);
        }
    }
}
