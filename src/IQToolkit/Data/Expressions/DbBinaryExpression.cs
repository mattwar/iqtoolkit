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
        /// <summary>
        /// The left operand.
        /// </summary>
        public Expression Left { get; }

        /// <summary>
        /// The text of the operator.
        /// </summary>
        public string Operator { get; }

        /// <summary>
        /// The right operand.
        /// </summary>
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

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitDbBinary(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var left = visitor.Visit(this.Left);
            var right = visitor.Visit(this.Right);
            return this.Update(this.Type, this.IsPredicate, left, this.Operator, right);
        }
    }
}
