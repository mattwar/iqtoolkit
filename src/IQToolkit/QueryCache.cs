using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit
{
    using Data;
    using Utils;

    /// <summary>
    /// A cache of compiled queries.
    /// </summary>
    public class QueryCache
    {
        private readonly MostRecentlyUsedCache<QueryCompiler.CompiledQuery> _cache;

        public QueryCache(int maxSize)
        {
            _cache = new MostRecentlyUsedCache<QueryCompiler.CompiledQuery>(maxSize, _fnCompareQueries);
        }

        private static readonly Func<QueryCompiler.CompiledQuery, QueryCompiler.CompiledQuery, bool> _fnCompareQueries = CompareQueries;
        private static readonly Func<object?, object?, bool> _fnCompareValues = CompareConstantValues;
        private static readonly ExpressionComparer _comparer = ExpressionComparer.Default.WithValueComparer(_fnCompareValues);

        private static bool CompareQueries(QueryCompiler.CompiledQuery x, QueryCompiler.CompiledQuery y)
        {
            return _comparer.Equals(x.Query, y.Query);
        }

        private static bool CompareConstantValues(object? x, object? y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;
            if (x is IQueryable && y is IQueryable && x.GetType() == y.GetType()) return true;
            return object.Equals(x, y);
        }

        /// <summary>
        /// Executes a cached query.
        /// </summary>
        public object? Execute(Expression query)
        {
            object[] args;
            var cached = this.Find(query, true, out args);
            return cached.Invoke(args);
        }

        /// <summary>
        /// Executes a cached query.
        /// </summary>
        public object? Execute(IQueryable query)
        {
            return this.Execute(query.Expression);
        }

        /// <summary>
        /// Executes a cached query.
        /// </summary>
        public IEnumerable<T> Execute<T>(IQueryable<T> query)
        {
            return (IEnumerable<T>)this.Execute(query.Expression)!;
        }

        /// <summary>
        /// The number of queries currently cached.
        /// </summary>
        public int Count
        {
            get { return this._cache.Count; }
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            this._cache.Clear();
        }

        /// <summary>
        /// True if the expression corresponds to a query already cached.
        /// </summary>
        public bool Contains(Expression query)
        {
            object[] args;
            return this.Find(query, false, out args) != null;
        }

        /// <summary>
        /// True if the <see cref="IQueryable"/> is already cached.
        /// </summary>
        public bool Contains(IQueryable query)
        {
            return this.Contains(query.Expression);
        }

        private QueryCompiler.CompiledQuery Find(Expression query, bool add, out object[] args)
        {
            var pq = this.Parameterize(query, out args);
            var cq = new QueryCompiler.CompiledQuery(pq);
            QueryCompiler.CompiledQuery cached;
            this._cache.Lookup(cq, add, out cached);
            return cached;
        }

        private LambdaExpression Parameterize(Expression query, out object[] arguments)
        {
            var provider = this.FindProvider(query);
            if (provider == null)
            {
                throw new ArgumentException("Cannot deduce query provider from query");
            }

            var ep = provider as IEntityProvider;
            var fn = ep != null ? (Func<Expression, bool>)ep.CanBeEvaluatedLocally : null;
            var parameters = new List<ParameterExpression>();
            var values = new List<object>();

            var body = PartialEvaluator.Eval(query, fn, c =>
            {
                bool isQueryRoot = c.Value is IQueryable;
                if (!isQueryRoot && ep != null && !ep.CanBeParameter(c))
                    return c;
                var p = Expression.Parameter(c.Type, "p" + parameters.Count);
                parameters.Add(p);
                values.Add(c.Value);
                // if query root then parameterize but don't replace in the tree 
                if (isQueryRoot)
                    return c;
                return p;
            });

            if (body.Type != typeof(object))
                body = Expression.Convert(body, typeof(object));

            arguments = values.ToArray();
            if (arguments.Length < 5)
            {
                return Expression.Lambda(body, parameters.ToArray());
            }
            else
            {
                arguments = new object[] { arguments };
                return ExplicitToObjectArray.Rewrite(body, parameters);
            }
        }

        private IQueryProvider? FindProvider(Expression expression)
        {
            var root = TypedSubtreeFinder.Find(expression, typeof(IQueryProvider)) as ConstantExpression;
            if (root == null)
            {
                root = TypedSubtreeFinder.Find(expression, typeof(IQueryable)) as ConstantExpression;
            }

            if (root != null)
            {
                var provider = root.Value as IQueryProvider;
                if (provider == null)
                {
                    var query = root.Value as IQueryable;
                    if (query != null)
                    {
                        provider = query.Provider;
                    }
                }

                return provider;
            }

            return null;
        }

        private class ExplicitToObjectArray : ExpressionRewriter
        {
            private readonly IList<ParameterExpression> parameters;
            private readonly ParameterExpression array = Expression.Parameter(typeof(object[]), "array");

            private ExplicitToObjectArray(IList<ParameterExpression> parameters)
            {
                this.parameters = parameters;
            }

            internal static LambdaExpression Rewrite(Expression body, IList<ParameterExpression> parameters)
            {
                var visitor = new ExplicitToObjectArray(parameters);
                return Expression.Lambda(visitor.Rewrite(body), visitor.array);                  
            }

            protected override Expression RewriteParameter(ParameterExpression p)
            {
                for (int i = 0, n = this.parameters.Count; i < n; i++)
                {
                    if (this.parameters[i] == p)
                    {
                        return Expression.Convert(Expression.ArrayIndex(this.array, Expression.Constant(i)), p.Type);
                    }
                }
                return p;
            }
        }
    }
}
