// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// A database query language unary operator expression.
    /// </summary>
    public sealed class DbPrefixUnaryExpression : DbOperation
    {
        /// <summary>
        /// The text of the operator.
        /// </summary>
        public string Operator { get; }

        /// <summary>
        /// The operand.
        /// </summary>
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

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitDbPrefixUnary(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var operand = visitor.Visit(this.Operand);
            return this.Update(this.Type, this.IsPredicate, this.Operator, operand);
        }
    }
}
