// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A database query language unary operator expression.
    /// </summary>
    public sealed class DbPrefixUnaryExpression : DbOperation
    {
        public string Operator { get; }
        public Expression Operand { get; }

        public DbPrefixUnaryExpression(Type type, bool isPredicate, string @operator, Expression operand)
            : base(type, isPredicate)
        {
            this.Operator = @operator;
            this.Operand = operand;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.DbPrefixUnary;

        public DbPrefixUnaryExpression Update(Type type, bool isPredicate, string @operator, Expression operand)
        {
            if (type != this.Type
                || isPredicate != this.IsPredicate
                || @operator != this.Operator
                || operand != this.Operand)
            {
                return new DbPrefixUnaryExpression(type, isPredicate, @operator, operand);
            }
            else
            {
                return this;
            }
        }
    }
}
