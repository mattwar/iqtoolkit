// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;

namespace IQToolkit.Expressions.Sql
{
    using System.Linq.Expressions;
    using Utils;

    /// <summary>
    /// A TSQL expression that evaluates the current row number.
    /// </summary>
    public sealed class RowNumberExpression : SqlExpression
    {
        public IReadOnlyList<OrderExpression> OrderBy { get; }

        public RowNumberExpression(IEnumerable<OrderExpression> orderBy)
            : base(typeof(int))
        {
            this.OrderBy = orderBy.ToReadOnly();
        }

        public RowNumberExpression Update(IReadOnlyList<OrderExpression> orderBy)
        {
            if (orderBy != this.OrderBy)
            {
                return new RowNumberExpression(orderBy);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitRowNumber(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var orderby = this.OrderBy.Rewrite(o => o.Accept(visitor));
            return this.Update(orderby);
        }
    }
}
