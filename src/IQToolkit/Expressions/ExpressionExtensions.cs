// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Expressions
{
    using System;
    using Utils;

    /// <summary>
    /// Common helper extension methods for construction expressions.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Walks the entire expression tree top-down and bottom-up.
        /// </summary>
        /// <param name="root">The expression at the top of the sub-tree.</param>
        /// <param name="fnBefore">The optional callback called for each expression when first encountered walking down.</param>
        /// <param name="fnAfter">The optional callback called for each expression when next enountered walking back up.</param>
        /// <param name="fnDescend">The optional callback used to determine if the walking should descend in the children.</param>
        public static void Walk(
            this Expression root,
            Action<Expression>? fnBefore = null,
            Action<Expression>? fnAfter = null,
            Func<Expression, bool>? fnDescend = null)
        {
            var walker = new Walker(fnBefore, fnAfter, fnDescend);
            walker.Visit(root);
        }

        private class Walker : ExpressionVisitor
        {
            private readonly Action<Expression>? _fnBefore;
            private readonly Action<Expression>? _fnAfter;
            private readonly Func<Expression, bool>? _fnDescend;

            public Walker(
                Action<Expression>? fnBefore,
                Action<Expression>? fnAfter,
                Func<Expression, bool>? fnDescend)
            {
                _fnBefore = fnBefore;
                _fnAfter = fnAfter;
                _fnDescend = fnDescend;
            }

            public override Expression Visit(Expression exp)
            {
                if (exp == null)
                    return null!;

                _fnBefore?.Invoke(exp);

                if (_fnDescend == null || _fnDescend(exp))
                {
                    base.Visit(exp);
                }

                _fnAfter?.Invoke(exp);

                return exp;
            }
        }

        /// <summary>
        /// Returns all the matching expressions in the subtree under this expression.
        /// </summary>
        public static IReadOnlyList<TExpression> FindAll<TExpression>(
            this Expression root,
            Func<TExpression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
            where TExpression : Expression
        {
            List<TExpression>? list = null;

            root.Walk(
                expression =>
                {
                    if (expression is TExpression tex
                        && (fnMatch == null || fnMatch(tex)))
                    {
                        if (list == null)
                            list = new List<TExpression>();
                        list.Add(tex);
                    }
                },
                fnDescend: fnDescend);

            return list.ToReadOnly();
        }

        /// <summary>
        /// Returns all the matching expressions in the subtree under this expression.
        /// </summary>
        public static IReadOnlyList<Expression> FindAll(
            this Expression root,
            Func<Expression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
        {
            return FindAll<Expression>(root, fnMatch, fnDescend);
        }

        /// <summary>
        /// Returns the first matching expression in the subtree under this expression 
        /// on the walk down
        /// or null if no expression matches.
        /// </summary>
        public static TExpression? FindFirstDownOrDefault<TExpression>(
            this Expression root,
            Func<TExpression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
            where TExpression : Expression
        {
            TExpression? found = null;

            root.Walk(
                fnBefore: expression =>
                {
                    if (found == null
                        && expression is TExpression tex
                        && (fnMatch == null || fnMatch(tex)))
                    {
                        found = tex;
                    }
                },

                fnDescend: e => found == null && (fnDescend == null || fnDescend(e))
                );

            return found;
        }

        /// <summary>
        /// Returns the first matching expression in the subtree under this expression 
        /// on the walk down
        /// or null if no expression matches.
        /// </summary>
        public static Expression? FindFirstDownOrDefault(
            this Expression root,
            Func<Expression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
        {
            return FindFirstDownOrDefault<Expression>(root, fnMatch, fnDescend);
        }

        /// <summary>
        /// Returns the first matching expression in the subtree under this expression 
        /// on the walk back up
        /// or null if no expression matches.
        /// </summary>
        public static TExpression? FindFirstUpOrDefault<TExpression>(
            this Expression root,
            Func<TExpression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
            where TExpression : Expression
        {
            TExpression? found = null;

            root.Walk(
                fnAfter: expression =>
                {
                    if (found == null
                        && expression is TExpression tex
                        && (fnMatch == null || fnMatch(tex)))
                    {
                        found = tex;
                    }
                },

                fnDescend: e => found == null && (fnDescend == null || fnDescend(e))
                );

            return found;
        }

        /// <summary>
        /// Returns the deepest matching expression in the subtree under this expression 
        /// on the walk back up
        /// or null if no expression matches.
        /// </summary>
        public static Expression? FindFirstUpOrDefault(
            this Expression root,
            Func<Expression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
        {
            return FindFirstUpOrDefault<Expression>(root, fnMatch, fnDescend);
        }

        /// <summary>
        /// Replace any number of expressions in the subtree under this expression,
        /// returning the new subtree with the expression replaced.
        /// </summary>
        public static TExpression Replace<TExpression>(
            this TExpression root,
            Func<Expression, Expression> fnReplacer,
            Func<Expression, bool>? fnDescend = null)
            where TExpression : Expression
        {
            var replacer = new Replacer(fnReplacer, fnDescend);
            return (TExpression)replacer.Visit(root);
        }

        /// <summary>
        /// Replaces one expression for another in the subtree under this expression,
        /// returning the new subtree with the expression replaced.
        /// </summary>
        public static TExpression Replace<TExpression>(
            this TExpression root,
            Expression searchFor,
            Expression replaceWith)
            where TExpression : Expression
        {
            return Replace(root,
                exp => exp == searchFor ? replaceWith : exp
                );
        }

        /// <summary>
        /// Replace all corresponding expressions in the subtree under this expression,
        /// returning the new subtree with the expressions replaced.
        /// </summary>
        public static TExpression ReplaceAll<TExpression>(
            this TExpression root,
            IReadOnlyList<Expression> searchFor,
            IReadOnlyList<Expression> replaceWith)
            where TExpression : Expression
        {
            var map = new Dictionary<Expression, Expression>(
                Enumerable.Zip(searchFor, replaceWith, (s, r) => KeyValuePair.Create(s, r))
            );

            return Replace(root, exp =>
            {
                if (map.TryGetValue(exp, out var replacement))
                    return replacement;
                return exp;
            });
        }

        private class Replacer : ExpressionVisitor
        {
            private readonly Func<Expression, Expression> _fnReplacer;
            private readonly Func<Expression, bool>? _fnDescend;

            public Replacer(
                Func<Expression, Expression> fnReplacer,
                Func<Expression, bool>? fnDescend)
            {
                _fnReplacer = fnReplacer;
                _fnDescend = fnDescend;
            }

            public override Expression Visit(Expression exp)
            {
                if (exp == null)
                    return null!;

                var replaced = _fnReplacer(exp);
                if (replaced != exp)
                {
                    // this expression needs to be replaced, don't bother with the sub-tree.
                    return replaced;
                }
                else if (_fnDescend == null || _fnDescend(exp))
                {
                    // look down the sub-tree
                    return base.Visit(exp);
                }
                else
                {
                    return exp;
                }
            }
        }

        public static Expression Equal(this Expression left, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.Equal(left, right);
        }

        public static Expression NotEqual(this Expression left, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.NotEqual(left, right);
        }

        public static Expression GreaterThan(this Expression left, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.GreaterThan(left, right);
        }

        public static Expression GreaterThanOrEqual(this Expression left, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.GreaterThanOrEqual(left, right);
        }

        public static Expression LessThan(this Expression left, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.LessThan(left, right);
        }

        public static Expression LessThanOrEqual(this Expression left, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.LessThanOrEqual(left, right);
        }

        public static Expression And(this Expression left, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.And(left, right);
        }

        public static Expression Or(this Expression left, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.Or(left, right);
        }

        /// <summary>
        /// Constructs a binary operator expression from the two expressions and an operator type.
        /// </summary>
        public static Expression Binary(this Expression left, ExpressionType op, Expression right)
        {
            ConvertExpressions(ref left, ref right);
            return Expression.MakeBinary(op, left, right);
        }

        /// <summary>
        /// Converts left and right expressions to the same type.
        /// </summary>
        private static void ConvertExpressions(ref Expression left, ref Expression right)
        {
            if (left.Type != right.Type)
            {
                var isNullable1 = TypeHelper.IsNullableType(left.Type);
                var isNullable2 = TypeHelper.IsNullableType(right.Type);
                if (isNullable1 || isNullable2)
                {
                    if (TypeHelper.GetNonNullableType(left.Type) == TypeHelper.GetNonNullableType(right.Type))
                    {
                        if (!isNullable1)
                        {
                            left = Expression.Convert(left, right.Type);
                        }
                        else if (!isNullable2)
                        {
                            right = Expression.Convert(right, left.Type);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds all the expressions at the leaves of a tree of binary operations.
        /// </summary>
        public static Expression[] Split(this Expression expression, params ExpressionType[] binarySeparators)
        {
            var list = new List<Expression>();
            Split(expression, list, binarySeparators);
            return list.ToArray();
        }

        /// <summary>
        /// Finds all the expressions at the leaves of a tree of binary operations.
        /// </summary>
        private static void Split(Expression expression, List<Expression> list, ExpressionType[] binarySeparators)
        {
            if (expression != null)
            {
                if (binarySeparators.Contains(expression.NodeType))
                {
                    var bex = expression as BinaryExpression;
                    if (bex != null)
                    {
                        Split(bex.Left, list, binarySeparators);
                        Split(bex.Right, list, binarySeparators);
                    }
                }
                else
                {
                    list.Add(expression);
                }
            }
        }

        /// <summary>
        /// Converts a list of expression into a tree of binary operations.
        /// </summary>
        public static Expression? Combine(this IEnumerable<Expression> list, ExpressionType binarySeparator)
        {
            var array = list.ToList();
            return array.Count > 0
                ? array.Aggregate((x1, x2) => Expression.MakeBinary(binarySeparator, x1, x2))
                : null;
        }

        /// <summary>
        /// Rewrites the expression list using the <see cref="ExpressionVisitor"/>.
        /// </summary>
        public static IReadOnlyList<Expression> Rewrite(this IReadOnlyList<Expression> list, ExpressionVisitor visitor) =>
            list.Rewrite(visitor.Visit);
    }
}