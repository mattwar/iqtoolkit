// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Returns the set of all columns referenced by the given expression
    /// </summary>
    public class ReferencedColumnGatherer : DbExpressionRewriter
    {
        HashSet<ColumnExpression> columns = new HashSet<ColumnExpression>();
        bool first = true;

        public static HashSet<ColumnExpression> Gather(Expression expression)
        {
            var visitor = new ReferencedColumnGatherer();
            visitor.Rewrite(expression);
            return visitor.columns;
        }

        protected override Expression RewriteColumn(ColumnExpression column)
        {
            this.columns.Add(column);
            return column;
        }

        protected override Expression RewriteSelect(SelectExpression select)
        {
            if (first)
            {
                first = false;
                return base.RewriteSelect(select);
            }
            return select;
        }
    }
}