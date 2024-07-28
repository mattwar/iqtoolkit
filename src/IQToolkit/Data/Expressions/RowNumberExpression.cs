// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    /// <summary>
    /// An expression that evaluates the current row number.
    /// TSQL only?
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
    }
}
