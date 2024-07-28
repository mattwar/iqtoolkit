// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    public static class DbExpressionFactory
    {
        public static DbBinaryExpression Concat(this Expression left, Expression right) =>
           Add(left, right, typeof(string));

        public static DbBinaryExpression Add(this Expression left, Expression right, Type type) =>
            new DbBinaryExpression(type, false, left, "+", right);

        public static DbBinaryExpression Subtract(this Expression left, Expression right, Type type) =>
            new DbBinaryExpression(type, false, left, "-", right);

        public static DbBinaryExpression Like(this Expression left, Expression right) =>
            new DbBinaryExpression(typeof(bool), true, left, "LIKE", right);

        public static IsNullExpression IsNull(this Expression left) =>
            new IsNullExpression(left);

        public static Expression IsNotNull(this Expression left) =>
            Expression.Not(left.IsNull());

        public static DbLiteralExpression Literal(Type type, string literalText) =>
            new DbLiteralExpression(type, literalText);

        public static FunctionCallExpression FunctionCall(Type type, string name, params Expression[] arguments) =>
            new FunctionCallExpression(type, false, name, arguments);

        public static FunctionCallExpression FunctionCall(Type type, string name, IReadOnlyList<Expression> arguments) =>
            new FunctionCallExpression(type, false, name, arguments);

        public static FunctionCallExpression FunctionCall(Type type, string name, Expression instance, IReadOnlyList<Expression> arguments) =>
            new FunctionCallExpression(type, false, name, new[] { instance }.Concat(arguments));

        public static readonly DbLiteralExpression EmptyStringLiteral =
            new DbLiteralExpression(typeof(string), "''");

        public static readonly DbLiteralExpression PercentStringLiteral =
            new DbLiteralExpression(typeof(string), "'%'");

        public static readonly DbLiteralExpression OneIntegerLiteral =
            new DbLiteralExpression(typeof(int), "1");
    }
}
