// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// Represents an outer joined expression in an projection.
    /// Includes both the normal projected expression and a test to determine 
    /// if the outer join value exists (typically a test for null on a non-nullable column).
    /// </summary>
    public sealed class OuterJoinedExpression : SqlExpression
    {
        /// <summary>
        /// The test expression that determines if the outer joined part exists.
        /// </summary>
        public Expression Test { get; }

        /// <summary>
        /// The expression based on the outer-joined source.
        /// </summary>
        public Expression Expression { get; }

        public OuterJoinedExpression(Expression test, Expression expression)
            : base(expression.Type)
        {
            this.Test = test;
            this.Expression = expression;
        }

        public OuterJoinedExpression Update(
            Expression test, Expression expression)
        {
            if (test != this.Test 
                || expression != this.Expression)
            {
                return new OuterJoinedExpression(test, expression);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitOuterJoined(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var test = visitor.Visit(this.Test);
            var expression = visitor.Visit(this.Expression);
            return this.Update(test, expression);
        }
    }
}
