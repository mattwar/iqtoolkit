// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A SQL IS NULL operator.
    /// Allows tests against value types like int and float.
    /// </summary>
    public sealed class IsNullExpression : DbExpression
    {
        public Expression Expression { get; }

        public IsNullExpression(Expression expression)
            : base(typeof(bool))
        {
            this.Expression = expression;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.IsNull;

        public override bool IsPredicate => base.IsPredicate;

        public IsNullExpression Update(Expression expression)
        {
            if (expression != this.Expression)
            {
                return new IsNullExpression(expression);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitIsNull(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var expr = visitor.Visit(this.Expression);
            return this.Update(expr);
        }
    }
}
