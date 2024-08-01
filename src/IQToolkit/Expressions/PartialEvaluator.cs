// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Expressions
{
    using Utils;

    /// <summary>
    /// Rewrites an expression tree so that locally isolatable sub-expressions are evaluated 
    /// and converted into <see cref="ConstantExpression"> nodes.
    /// </summary>
    public static class PartialEvaluator
    {
        /// <summary>
        /// Performs evaluation and replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <param name="fnPostEval">A function to apply to each newly formed <see cref="ConstantExpression"/>.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression Eval(
            Expression expression, 
            Func<Expression, bool>? fnCanBeEvaluated = null, 
            Func<ConstantExpression, Expression>? fnPostEval = null)
        {
            if (fnCanBeEvaluated == null)
                fnCanBeEvaluated = PartialEvaluator.CanBeEvaluatedLocally;
            return SubtreeEvaluator.Eval(Nominator.Nominate(fnCanBeEvaluated, expression), fnPostEval, expression);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }

        /// <summary>
        /// Evaluates and replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        class SubtreeEvaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression> _candidates;
            private readonly Func<ConstantExpression, Expression>? _fnOnEval;

            private SubtreeEvaluator(
                HashSet<Expression> candidates, 
                Func<ConstantExpression, Expression>? fnOnEval)
            {
                _candidates = candidates;
                _fnOnEval = fnOnEval;
            }

            internal static Expression Eval(
                HashSet<Expression> candidates, 
                Func<ConstantExpression, Expression>? fnOnEval, 
                Expression exp)
            {
                return new SubtreeEvaluator(candidates, fnOnEval).Visit(exp);
            }

            public override Expression Visit(Expression exp)
            {
                if (exp != null && _candidates.Contains(exp))
                {
                    return this.Evaluate(exp);
                }

                return base.Visit(exp);
            }

            protected override Expression VisitConditional(ConditionalExpression c)
            {
                // if the conditional test can be evaluated locally,
                // rewrite expression to the matching branch
                if (_candidates.Contains(c.Test))
                {
                    var test = Evaluate(c.Test);
                    if (test is ConstantExpression cex && cex.Value is bool bValue)
                    {
                        if (bValue)
                        {
                            return this.Visit(c.IfTrue);
                        }
                        else
                        {
                            return this.Visit(c.IfFalse);
                        }
                    }
                }

                return base.VisitConditional(c);
            }

            private Expression PostEval(ConstantExpression e)
            {
                if (_fnOnEval != null)
                {
                    return _fnOnEval(e);
                }

                return e;
            }

            private Expression Evaluate(Expression e)
            {
                Type type = e.Type;

                if (e.NodeType == ExpressionType.Convert)
                {
                    // check for unnecessary convert & strip them
                    var u = (UnaryExpression)e;
                    if (TypeHelper.GetNonNullableType(u.Operand.Type) == TypeHelper.GetNonNullableType(type))
                    {
                        e = ((UnaryExpression)e).Operand;
                    }
                }

                if (e.NodeType == ExpressionType.Constant)
                {
                    // in case we actually threw out a nullable conversion above, simulate it here
                    // don't post-eval nodes that were already constants
                    if (e.Type == type)
                    {
                        return e;
                    }
                    else if (TypeHelper.GetNonNullableType(e.Type) == TypeHelper.GetNonNullableType(type))
                    {
                        return Expression.Constant(((ConstantExpression)e).Value, type);
                    }
                }

                var me = e as MemberExpression;
                if (me != null)
                {
                    // member accesses off of constant's are common, and yet since these partial evals
                    // are never re-used, using reflection to access the member is faster than compiling  
                    // and invoking a lambda
                    var ce = me.Expression as ConstantExpression;
                    if (ce != null)
                    {
                        return this.PostEval(Expression.Constant(me.Member.GetValue(ce.Value), type));
                    }
                }

                if (type.IsValueType)
                {
                    e = Expression.Convert(e, typeof(object));
                }

                var lambda = Expression.Lambda<Func<object>>(e);
                var fn = lambda.Compile();

                return this.PostEval(Expression.Constant(fn(), type));
            }
        }

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        class Nominator : ExpressionVisitor
        {
            private readonly Func<Expression, bool> _fnCanBeEvaluated;
            private readonly HashSet<Expression> _candidates;
            private bool _cannotBeEvaluated;

            private Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                _candidates = new HashSet<Expression>();
                _fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal static HashSet<Expression> Nominate(Func<Expression, bool> fnCanBeEvaluated, Expression expression)
            {
                var nominator = new Nominator(fnCanBeEvaluated);
                nominator.Visit(expression);
                return nominator._candidates;
            }

            public override Expression Visit(Expression expression)
            {
                if (expression == null)
                    return null!;

                bool saveCannotBeEvaluated = _cannotBeEvaluated;
                _cannotBeEvaluated = false;
                
                base.Visit(expression);
                
                if (!_cannotBeEvaluated)
                {
                    if (_fnCanBeEvaluated(expression))
                    {
                        _candidates.Add(expression);
                    }
                    else
                    {
                        _cannotBeEvaluated = true;
                    }
                }

                _cannotBeEvaluated |= saveCannotBeEvaluated;
                return expression;
            }
        }
    }
}