// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data
{
    using Common;
    using Mapping;

    /// <summary>
    /// A base type for LINQ IQueryable query providers that executes translated queries against a database.
    /// </summary>
    public abstract class EntityProvider : QueryProvider, IEntityProvider, IQueryExecutorFactory
    {
        QueryLanguage language;
        QueryMapping mapping;
        QueryPolicy policy;
        TextWriter log;
        Dictionary<MappingEntity, IEntityTable> tables;
        QueryCache cache;

        public EntityProvider(QueryLanguage language, QueryMapping mapping, QueryPolicy policy)
        {
            if (language == null)
                throw new InvalidOperationException("Language not specified");
            if (mapping == null)
                throw new InvalidOperationException("Mapping not specified");
            if (policy == null)
                throw new InvalidOperationException("Policy not specified");
            this.language = language;
            this.mapping = mapping;
            this.policy = policy;
            this.tables = new Dictionary<MappingEntity, IEntityTable>();
        }

        public QueryMapping Mapping
        {
            get { return this.mapping; }
        }

        public QueryLanguage Language
        {
            get { return this.language; }
        }

        public QueryPolicy Policy
        {
            get { return this.policy; }

            set
            {
                if (value == null)
                {
                    this.policy = QueryPolicy.Default;
                }
                else
                {
                    this.policy = value;
                }
            }
        }

        public TextWriter Log
        {
            get { return this.log; }
            set { this.log = value; }
        }

        public QueryCache Cache
        {
            get { return this.cache; }
            set { this.cache = value; }
        }

        public IEntityTable GetTable(MappingEntity entity)
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
                typeof(EntityTable<>).MakeGenericType(entity.ElementType), 
                new object[] { this, entity }
                );
        }

        public virtual IEntityTable<T> GetTable<T>()
        {
            return GetTable<T>(null);
        }

        public virtual IEntityTable<T> GetTable<T>(string tableId)
        {
            return (IEntityTable<T>)this.GetTable(typeof(T), tableId);
        }

        public virtual IEntityTable GetTable(Type type)
        {
            return GetTable(type, null);
        }

        public virtual IEntityTable GetTable(Type type, string tableId)
        {
            return this.GetTable(this.Mapping.GetEntity(type, tableId));
        }

        public bool CanBeEvaluatedLocally(Expression expression)
        {
            return this.Mapping.CanBeEvaluatedLocally(expression);
        }

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

        protected abstract QueryExecutor CreateExecutor();

        QueryExecutor IQueryExecutorFactory.CreateExecutor()
        {
            return this.CreateExecutor();
        }

        public class EntityTable<T> : Query<T>, IEntityTable<T>, IHaveMappingEntity
        {
            MappingEntity entity;
            EntityProvider provider;

            public EntityTable(EntityProvider provider, MappingEntity entity)
                : base(provider, typeof(IEntityTable<T>))
            {
                this.provider = provider;
                this.entity = entity;
            }

            public MappingEntity Entity
            {
                get { return this.entity; }
            }

            new public IEntityProvider Provider
            {
                get { return this.provider; }
            }

            public string TableId
            {
                get { return this.entity.TableId; }
            }

            public Type EntityType
            {
                get { return this.entity.EntityType; }
            }

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

            public int Insert(T instance)
            {
                return Updatable.Insert(this, instance);
            }

            int IEntityTable.Insert(object instance)
            {
                return this.Insert((T)instance);
            }

            public int Delete(T instance)
            {
                return Updatable.Delete(this, instance);
            }

            int IEntityTable.Delete(object instance)
            {
                return this.Delete((T)instance);
            }

            public int Update(T instance)
            {
                return Updatable.Update(this, instance);
            }

            int IEntityTable.Update(object instance)
            {
                return this.Update((T)instance);
            }

            public int InsertOrUpdate(T instance)
            {
                return Updatable.InsertOrUpdate(this, instance);
            }

            int IEntityTable.InsertOrUpdate(object instance)
            {
                return this.InsertOrUpdate((T)instance);
            }
        }

        public override string GetQueryText(Expression expression)
        {
            Expression plan = this.GetExecutionPlan(expression);
            var commands = CommandGatherer.Gather(plan).Select(c => c.CommandText).ToArray();
            return string.Join("\n\n", commands);
        }

        class CommandGatherer : DbExpressionVisitor
        {
            List<QueryCommand> commands = new List<QueryCommand>();

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

        public string GetQueryPlan(Expression expression)
        {
            Expression plan = this.GetExecutionPlan(expression);
            return DbExpressionWriter.WriteToString(this.Language, plan);
        }

        protected virtual QueryTranslator CreateTranslator()
        {
            return new QueryTranslator(this.language, this.mapping, this.policy);
        }

        public abstract void DoTransacted(Action action);
        public abstract void DoConnected(Action action);
        public abstract int ExecuteCommand(string commandText);

        /// <summary>
        /// Execute the query expression (does translation, etc.)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public override object Execute(Expression expression)
        {
            LambdaExpression lambda = expression as LambdaExpression;

            if (lambda == null && this.cache != null && expression.NodeType != ExpressionType.Constant)
            {
                return this.cache.Execute(expression);
            }

            Expression plan = this.GetExecutionPlan(expression);

            if (lambda != null)
            {
                // compile & return the execution plan so it can be used multiple times
                LambdaExpression fn = Expression.Lambda(lambda.Type, plan, lambda.Parameters);
#if NOREFEMIT
                    return ExpressionEvaluator.CreateDelegate(fn);
#else
                return fn.Compile();
#endif
            }
            else
            {
                // compile the execution plan and invoke it
                Expression<Func<object>> efn = Expression.Lambda<Func<object>>(Expression.Convert(plan, typeof(object)));
#if NOREFEMIT
                    return ExpressionEvaluator.Eval(efn, new object[] { });
#else
                Func<object> fn = efn.Compile();
                return fn();
#endif
            }
        }

        /// <summary>
        /// Convert the query expression into an execution plan
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
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
                provider = Expression.Property(rootQueryable, typeof(IQueryable).GetProperty("Provider"));
            }

            return translator.Police.BuildExecutionPlan(translation, provider);
        }

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

        /// <summary>
        /// Create a <see cref="QueryMapping"/> from the specified mapping string.
        /// </summary>
        /// <param name="mapping">This is typically the type name of a context class with <see cref="MappingAttribute"/>'s 
        /// or the path to a mapping file, but may have custom meaning for the provider.</param>
        protected virtual QueryMapping CreateMapping(string mapping)
        {
            if (mapping != null)
            {
                if (mapping == "Attribute" || mapping == "AttributeMapping")
                {
                    return new AttributeMapping();
                }

                Type type = FindLoadedType(mapping);
                if (type != null)
                {
                    if (type.IsSubclassOf(typeof(QueryMapping)))
                    {
                        // the type of a QueryMapping?
                        return (QueryMapping)Activator.CreateInstance(type);
                    }
                    else
                    {
                        // assume this is a context type for attribute mapping
                        return new AttributeMapping(type);
                    }
                }

                // if this is a file name, try to load it as an XmlMapping.
                if (File.Exists(mapping))
                {
                    return XmlMapping.FromXml(File.ReadAllText(mapping));
                }
            }

            // otherwise, use implicit mapping
            return new ImplicitMapping();
        }

        /// <summary>
        /// Get the <see cref="Type"/> of the <see cref="EntityProvider"/> given the name of the provider assembly.
        /// </summary>
        /// <param name="providerAssemblyName">The name of the provider assembly.</param>
        protected static Type GetProviderType(string providerAssemblyName)
        {
            if (!string.IsNullOrEmpty(providerAssemblyName))
            {
                var providers = FindInstancesInAssembly(typeof(EntityProvider), providerAssemblyName).ToList();

                Type type = null;

                if (providers.Count == 1)
                {
                    type = providers[0];
                }
                else if (providers.Count > 1)
                {
                    // if more than one, pick the provider in the same namespace that matches the assembly name
                    type = providers.Where(t => string.Compare(t.Namespace, providerAssemblyName, true) == 0)
                            .FirstOrDefault();
                }

                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException(string.Format("Unable to find query provider '{0}'", providerAssemblyName));
        }

        private static Type FindLoadedType(string typeName)
        {
            foreach (var assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assem.GetType(typeName, false, true);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static IEnumerable<Type> FindInstancesInAssembly(Type type, string assemblyName)
        {
            Assembly assembly = GetOrLoadAssembly(assemblyName);
            if (assembly != null)
            {
                foreach (var atype in assembly.GetTypes())
                {
                    // find types in the same namespace 
                    if (type.IsAssignableFrom(atype))
                    {
                        yield return atype;
                    }
                }
            }
        }

        private static Assembly GetOrLoadAssembly(string assemblyName)
        {
            foreach (var assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assem.FullName.Contains(assemblyName))
                {
                    return assem;
                }
            }

            return Load(assemblyName + ".dll");
        }

        private static Assembly Load(string assemblyName)
        {
            // try to load it.
            try
            {
                var fullName = Path.GetFullPath(assemblyName);
                return Assembly.LoadFrom(fullName);
            }
            catch
            {
            }
            return null;
        }
    }
}
