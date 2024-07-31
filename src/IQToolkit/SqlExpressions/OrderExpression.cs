// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// A pairing of an expression and an <see cref="OrderType"/>,
    /// used in <see cref="SelectExpression.OrderBy"/>.
    /// </summary>
    public sealed class OrderExpression
    {
        public OrderType OrderType { get; }
        public Expression Expression { get; }

        public OrderExpression(OrderType orderType, Expression expression)
        {
            this.OrderType = orderType;
            this.Expression = expression;
        }

        public OrderExpression Update(OrderType orderType, Expression expression)
        {
            if (orderType != this.OrderType
                || expression != this.Expression)
            {
                return new OrderExpression(orderType, expression);
            }
            else
            {
                return this;
            }
        }

        internal OrderExpression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitOrder(this);
            return this.VisitChildren(visitor);
        }

        internal OrderExpression VisitChildren(ExpressionVisitor visitor)
        {
            var expression = visitor.Visit(this.Expression);
            return this.Update(this.OrderType, expression);
        }
    }
}
