// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit
{
    /// <summary>
    /// Common helper extension methods for construction expressions.
    /// </summary>
    public static class ExpressionExtensions
    {
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
        public static Expression Join(this IEnumerable<Expression> list, ExpressionType binarySeparator)
        {
            if (list != null)
            {
                var array = list.ToArray();
                if (array.Length > 0)
                {
                    return array.Aggregate((x1, x2) => Expression.MakeBinary(binarySeparator, x1, x2));
                }
            }

            return null;
        }
    }
}