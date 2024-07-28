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
                var providerNode = this.FindProvider(expression);

                var providerConst = providerNode as ConstantExpression;

                // If no expression is found that refers to the provider
                // replace all parameter references with actual argument values and try again.
                if (providerConst == null
                    && args != null 
                    && args.Length > 0)
                {
                    var replaced = ExpressionReplacer.ReplaceAll(
                        expression,
                        _query.Parameters.ToArray(),
                        args.Select((a, i) => Expression.Constant(a, _query.Parameters[i].Type)).ToArray()
                        );

                    providerNode = this.FindProvider(replaced);
                    providerConst = providerNode as ConstantExpression;
                }

                // if we found a provider node but it is not a constant
                // try partial evaluating the node to get the value
                if (providerConst == null && providerNode != null)
                {
                    providerConst = PartialEvaluator.Eval(providerNode) as ConstantExpression;
                }

                if (providerConst != null)
                {
                    if (providerConst.Value is IQueryProvider provider)
                    {
                        return provider;
                    }
                    else if (providerConst.Value is IQueryable queryable)
                    {
                        return queryable.Provider;
                    }
                }

                return null;
            }

            /// <summary>
            /// Returns the expression that refers to the query provider.
            /// </summary>
            private Expression? FindProvider(Expression expression)
            {
                return TypedSubtreeFinder.Find(expression, typeof(IQueryProvider))
                    ?? TypedSubtreeFinder.Find(expression, typeof(IQueryable));
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

            private Func<object?[], object?>? _invoker;
            private bool _checkedForInvoker;

            private Func<object?[], object?>? GetInvoker()
            {
                if (_fnQuery != null && _invoker == null && !_checkedForInvoker)
                {
                    _checkedForInvoker = true;
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