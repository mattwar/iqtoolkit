// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;
    using Utils;

    /// <summary>
    /// Determines if a <see cref="SelectExpression"/> contains any <see cref="AggregateExpression"/>.
    /// </summary>
    class AggregateChecker : SqlExpressionVisitor
    {
        private bool _hasAggregate = false;

        private AggregateChecker()
        {
        }

        internal static bool HasAggregates(SelectExpression expression)
        {
            AggregateChecker checker = new AggregateChecker();
            checker.Visit(expression);
            return checker._hasAggregate;
        }

        protected internal override Expression VisitAggregate(AggregateExpression aggregate)
        {
            _hasAggregate = true;
            return aggregate;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            // only consider aggregates in these locations
            this.Visit(select.Where);
            select.OrderBy.Rewrite(o => o.Accept(this));
            select.Columns.Rewrite(d => d.Accept(this));
            return select;
        }

        protected internal override Expression VisitScalarSubquery(ScalarSubqueryExpression scalar)
        {
            // don't count aggregates in subqueries
            return scalar;
        }

        protected internal override Expression VisitExistsSubquery(ExistsSubqueryExpression exists)
        {
            // don't count aggregates in subqueries
            return exists;
        }

        protected internal override Expression VisitInSubquery(InSubqueryExpression @in)
        {
            // don't count aggregates in subqueries
            return @in;
        }
    }
}