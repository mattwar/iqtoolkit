// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;

namespace IQToolkit.SqlExpressions
{
    using System.Linq.Expressions;
    using Utils;

    /// <summary>
    /// A TSQL expression that evaluates the current row number.
    /// </summary>
    public sealed class RowNumberExpression : DbExpression
    {
        public IReadOnlyList<OrderExpression> OrderBy { get; }

        public RowNumberExpression(IEnumerable<OrderExpression> orderBy)
            : base(typeof(int))
        {
            this.OrderBy = orderBy.ToReadOnly();
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.RowNumber;

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
            if (visitor is DbExpressionVisitor dbVisitor)
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
