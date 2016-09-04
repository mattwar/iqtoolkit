using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IQToolkit
{
    /// <summary>
    /// A wrapper that converts a result enumerable into a IQueryable.
    /// Any additional query execution will occur locally.
    /// </summary>
    public class ResultQuery<T> : IQueryable<T>, IQueryable, IAsyncEnumerable<T>, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
    {
        private readonly IEnumerable<T> enumerable;

        public ResultQuery(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        public Expression Expression
        {
            get { return Expression.Constant(this); }
        }

        public Type ElementType
        {
            get { return typeof(T); }
        }

        public IQueryProvider Provider
        {
            get { return DefaultProvider; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public Task<IAsyncEnumerator<T>> GetEnumeratorAsync(CancellationToken cancellationToken)
        {
            return this.enumerable.ToAsync().GetEnumeratorAsync(cancellationToken);
        }

        public IEnumerable<T> UnderlyingEnumerable
        {
            get { return this.enumerable; }
        }

        private static readonly ResultProvider DefaultProvider = new ResultProvider();

        private class ResultProvider : QueryProvider
        {
            public override object Execute(Expression expression)
            {
                var localExpression = MethodCallRewriter.Instance.Rewrite(expression);
                Expression<Func<object>> efn = Expression.Lambda<Func<object>>(Expression.Convert(localExpression, typeof(object)));
                
#if NOREFEMIT
                return ExpressionEvaluator.Eval(efn, new object[] { });
#else
                Func<object> fn = efn.Compile();
                return fn();
#endif
            }

            public override string GetQueryText(Expression expression)
            {
                return ExpressionWriter.WriteToString(expression);
            }
        }

        private class MethodCallRewriter : ExpressionVisitor
        {
            public static readonly MethodCallRewriter Instance = new MethodCallRewriter();

            public Expression Rewrite(Expression expression)
            {
                return this.Visit(expression);
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Object == null)
                {
                    var rewrittenArgs = this.VisitExpressionList(m.Arguments);

                    if (m.Method.DeclaringType == typeof(Queryable))
                    {
                        // rebind static calls on LINQ Queryable type against LINQ Enumerable type
                        return Expression.Call(typeof(Enumerable), m.Method.Name, m.Method.GetGenericArguments(), rewrittenArgs.ToArray());
                    }
                    else if (rewrittenArgs != m.Arguments)
                    {
                        // otherwise, if there has been a change, try rebinding static calls against same declaring type
                        return Expression.Call(m.Method.DeclaringType, m.Method.Name, m.Method.GetGenericArguments(), rewrittenArgs.ToArray());
                    }
                    else
                    {
                        return m;
                    }
                }
                else
                {
                    return base.VisitMethodCall(m);
                }
            }
        }
    }
}
