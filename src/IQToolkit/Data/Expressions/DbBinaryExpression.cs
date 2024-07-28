// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A database query language binary operator expression.
    /// </summary>
    public sealed class DbBinaryExpression : DbOperation
    {
        public Expression Left { get; }
        public string Operator { get; }
        public Expression Right { get; }

        public DbBinaryExpression(
            Type type, 
            bool isPredicate,
            Expression left, 
            string @operator, 
            Expression right)
            : base(type, isPredicate)
        {
            this.Left = left;
            this.Operator = @operator;
            this.Right = right;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.DbBinary;

        public DbBinaryExpression Update(
            Type type, 
            bool isPredicate,
            Expression left, 
            string @operator, 
            Expression right)
        {
            if (type != this.Type
                || isPredicate != this.IsPredicate
                || left != this.Left
                || @operator != this.Operator
                || right != this.Right)
            {
                return new DbBinaryExpression(type, isPredicate, left, @operator, right);
            }
            else
            {
                return this;
            }
        }
    }
}
