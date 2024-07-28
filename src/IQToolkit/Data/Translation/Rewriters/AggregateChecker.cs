// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    /// <summary>
    /// Determines if a <see cref="SelectExpression"/> contains any <see cref="AggregateExpression"/>.
    /// </summary>
    class AggregateChecker : DbExpressionRewriter
    {
        private bool _hasAggregate = false;

        private AggregateChecker()
        {
        }

        internal static bool HasAggregates(SelectExpression expression)
        {
            AggregateChecker checker = new AggregateChecker();
            checker.Rewrite(expression);
            return checker._hasAggregate;
        }

        protected override Expression RewriteAggregate(AggregateExpression aggregate)
        {
            _hasAggregate = true;
            return aggregate;
        }

        protected override Expression RewriteSelect(SelectExpression select)
        {
            // only consider aggregates in these locations
            this.RewriteN(select.Where);
            this.VisitOrderExpressions(select.OrderBy);
            this.RewriteColumnDeclarations(select.Columns);
            return select;
        }

        protected override Expression RewriteScalarSubquery(ScalarSubqueryExpression scalar)
        {
            // don't count aggregates in subqueries
            return scalar;
        }

        protected override Expression RewriteExistsSubquery(ExistsSubqueryExpression exists)
        {
            // don't count aggregates in subqueries
            return exists;
        }

        protected override Expression RewriteInSubquery(InSubqueryExpression @in)
        {
            // don't count aggregates in subqueries
            return @in;
        }
    }
}