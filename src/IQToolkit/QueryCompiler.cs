// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit
{
    using Expressions;
    using Utils;

    /// <summary>
    /// Creates a reusable, parameterized representation of a query that caches the execution plan
    /// </summary>
    public static class QueryCompiler
    {
        /// <summary>
        /// Convert a query into a delegate that will execute the query when invoked.
        /// </summary>
        public static Delegate Compile(LambdaExpression query)
        {
            CompiledQuery cq = new CompiledQuery(query);
            return StrongDelegate.CreateDelegate(query.Type, (Func<object?[], object?>)cq.Invoke);
        }

        /// <summary>
        /// Convert a query into a delegate that will execute the query when invoked.
        /// </summary>
        public static D Compile<D>(Expression<D> query)
        {
            return (D)(object)Compile((LambdaExpression)query);
        }

        /// <summary>
        /// Convert a query into a delegate that will execute the query when invoked.
        /// </summary>
        public static Func<TResult> Compile<TResult>(Expression<Func<TResult>> query)
        {
            return new CompiledQuery(query).Invoke<TResult>;
        }

        /// <summary>
        /// Convert a query into a delegate that will execute the query when invoked.
        /// </summary>
        public static Func<T1, TResult> Compile<T1, TResult>(Expression<Func<T1, TResult>> query)
        {
            return new CompiledQuery(query).Invoke<T1, TResult>;
        }

        /// <summary>
        /// Convert a query into a delegate that will execute the query when invoked.
        /// </summary>
        public static Func<T1, T2, TResult> Compile<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> query)
        {
            return new CompiledQuery(query).Invoke<T1, T2, TResult>;
        }

        /// <summary>
        /// Convert a query into a delegate that will execute the query when invoked.
        /// </summary>
        public static Func<T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> query)
        {
            return new CompiledQuery(query).Invoke<T1, T2, T3, TResult>;
        }

        /// <summary>
        /// Convert a query into a delegate that will execute the query when invoked.
        /// </summary>
        public static Func<T1, T2, T3, T4, TResult> Compile<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> query)
        {
            return new CompiledQuery(query).Invoke<T1, T2, T3, T4, TResult>;
        }

        /// <summary>
        /// Convert a query into a delegate that will execute the query when invoked.
        /// </summary>
        public static Func<IEnumerable<T>> Compile<T>(this IQueryable<T> source)
        {
            return Compile<IEnumerable<T>>(
                Expression.Lambda<Func<IEnumerable<T>>>(((IQueryable)source).Expression)
                );
        }

        public class CompiledQuery
        {
            private readonly LambdaExpression _query;
            private Delegate? _fnQuery;

            internal CompiledQuery(LambdaExpression query)
            {
                _query = query;
            }

            public LambdaExpression Query => _query;

            internal void Compile(params object?[] args)
            {
                if (_fnQuery == null)
                {
                    // first identify the query provider being used
                    Expression body = _query.Body;

                    // ask the query provider to compile the query by 'executing' the lambda expression
                    var provider = this.FindProvider(body, args);
                    if (provider == null)
                    {
                        throw new InvalidOperationException("Could not find query provider");
                    }

                    Delegate result = (Delegate)provider.Execute(_query);
                    System.Threading.Interlocked.CompareExchange(ref _fnQuery, result, null);
                }
            }

            internal IQueryProvider? FindProvider(Expression expression, object?[] args)
            {
                Expression? root = this.FindProviderInExpression(expression) as ConstantExpression;
                
                if (root == null && args != null && args.Length > 0)
                {
                    Expression replaced = ExpressionReplacer.ReplaceAll(
                        expression,
                        _query.Parameters.ToArray(),
                        args.Select((a, i) => Expression.Constant(a, _query.Parameters[i].Type)).ToArray()
                        );
                    root = this.FindProviderInExpression(replaced);
                }

                if (root != null) 
                {
                    var cex = root as ConstantExpression;
                    if (cex == null)
                    {
                        cex = PartialEvaluator.Eval(root) as ConstantExpression;
                    }
                    if (cex != null)
                    {
                        var provider = cex.Value as IQueryProvider;
                        if (provider == null)
                        {
                            var query = cex.Value as IQueryable;
                            if (query != null)
                            {
                                provider = query.Provider;
                            }
                        }

                        return provider;
                    }
                }

                return null;
            }

            private Expression? FindProviderInExpression(Expression expression)
            {
                var root = TypedSubtreeFinder.Find(expression, typeof(IQueryProvider));
                
                if (root == null)
                {
                    root = TypedSubtreeFinder.Find(expression, typeof(IQueryable));            
                }

                return root;
            }

            public object? Invoke(object?[] args)
            {
                this.Compile(args);
                if (_invoker == null)
                {
                    _invoker = GetInvoker();
                }
                if (_invoker != null)
                {
                    return _invoker(args);
                }
                else
                {
                    try
                    {
                        return _fnQuery!.DynamicInvoke(args);
                    }
                    catch (TargetInvocationException tie)
                    {
                        throw tie.InnerException;
                    }
                }
            }

            Func<object?[], object?>? _invoker;
            bool checkedForInvoker;

            private Func<object?[], object?>? GetInvoker()
            {
                if (_fnQuery != null && _invoker == null && !checkedForInvoker)
                {
                    this.checkedForInvoker = true;
                    Type fnType = _fnQuery.GetType();

                    if (fnType.FullName.StartsWith("System.Func`"))
                    {
                        var typeArgs = fnType.GetTypeInfo().GenericTypeArguments;
                        MethodInfo method = this.GetType().GetTypeInfo().GetDeclaredMethod("FastInvoke"+typeArgs.Length);
                        if (method != null)
                        {
                            _invoker = (Func<object?[], object?>)method.MakeGenericMethod(typeArgs).CreateDelegate(typeof(Func<object[], object>), this);
                        }
                    }
                }
                return _invoker;
            }

            public object? FastInvoke1<R>(object?[] args)
            {
                return ((Func<R>)_fnQuery!)();
            }

            public object? FastInvoke2<A1, R>(object?[] args)
            {
                return ((Func<A1, R>)_fnQuery!)((A1)args[0]!);
            }

            public object? FastInvoke3<A1, A2, R>(object?[] args)
            {
                return ((Func<A1, A2, R>)_fnQuery!)((A1)args[0]!, (A2)args[1]!);
            }

            public object? FastInvoke4<A1, A2, A3, R>(object?[] args)
            {
                return ((Func<A1, A2, A3, R>)_fnQuery!)((A1)args[0]!, (A2)args[1]!, (A3)args[2]!);
            }

            public object? FastInvoke5<A1, A2, A3, A4, R>(object?[] args)
            {
                return ((Func<A1, A2, A3, A4, R>)_fnQuery!)((A1)args[0]!, (A2)args[1]!, (A3)args[2]!, (A4)args[3]!);
            }

            private static object?[] _noArgs = new object?[0];

            internal TResult Invoke<TResult>()
            {
                this.Compile(_noArgs);
                return ((Func<TResult>)_fnQuery!)();
            }

            internal TResult Invoke<T1, TResult>(T1 arg)
            {
                this.Compile(arg);
                return ((Func<T1, TResult>)_fnQuery!)(arg);
            }

            internal TResult Invoke<T1, T2, TResult>(T1 arg1, T2 arg2)
            {
                this.Compile(arg1, arg2);
                return ((Func<T1, T2, TResult>)_fnQuery!)(arg1, arg2);
            }

            internal TResult Invoke<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3)
            {
                this.Compile(arg1, arg2, arg3);
                return ((Func<T1, T2, T3, TResult>)_fnQuery!)(arg1, arg2, arg3);
            }

            internal TResult Invoke<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                this.Compile(arg1, arg2, arg3, arg4);
                return ((Func<T1, T2, T3, T4, TResult>)_fnQuery!)(arg1, arg2, arg3, arg4);
            }
        }
    }
}