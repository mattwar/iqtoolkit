// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Expressions.Sql
{
    /// <summary>
    /// A static factory for constructing <see cref="SqlExpression"/> nodes.
    /// </summary>
    public static class SqlExpressionFactory
    {
        public static ScalarBinaryExpression Concat(this Expression left, Expression right) =>
           Add(left, right, typeof(string));

        public static ScalarBinaryExpression Add(this Expression left, Expression right, Type type) =>
            new ScalarBinaryExpression(type, false, left, "+", right);

        public static ScalarBinaryExpression Subtract(this Expression left, Expression right, Type type) =>
            new ScalarBinaryExpression(type, false, left, "-", right);

        public static ScalarBinaryExpression Like(this Expression left, Expression right) =>
            new ScalarBinaryExpression(typeof(bool), true, left, "LIKE", right);

        public static IsNullExpression IsNull(this Expression left) =>
            new IsNullExpression(left);

        public static Expression IsNotNull(this Expression left) =>
            Expression.Not(left.IsNull());

        public static LiteralExpression Literal(Type type, string literalText) =>
            new LiteralExpression(type, literalText);

        public static ScalarFunctionCallExpression FunctionCall(Type type, string name, params Expression[] arguments) =>
            new ScalarFunctionCallExpression(type, false, name, arguments);

        public static ScalarFunctionCallExpression FunctionCall(Type type, string name, IReadOnlyList<Expression> arguments) =>
            new ScalarFunctionCallExpression(type, false, name, arguments);

        public static ScalarFunctionCallExpression FunctionCall(Type type, string name, Expression instance, IReadOnlyList<Expression> arguments) =>
            new ScalarFunctionCallExpression(type, false, name, new[] { instance }.Concat(arguments));

        public static readonly LiteralExpression EmptyStringLiteral =
            new LiteralExpression(typeof(string), "''");

        public static readonly LiteralExpression PercentStringLiteral =
            new LiteralExpression(typeof(string), "'%'");

        public static readonly LiteralExpression OneIntegerLiteral =
            new LiteralExpression(typeof(int), "1");
    }
}
