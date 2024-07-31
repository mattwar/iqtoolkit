// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;

    /// <summary>
    /// Rewrites queries with skip and take to use nested queries with inverted ordering technique
    /// </summary>
    public class SkipTakeToTopRewriter : SqlExpressionVisitor
    {
        private readonly QueryLanguage _language;

        public SkipTakeToTopRewriter(QueryLanguage language)
        {
            _language = language;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            // select * from table order by x skip s take t 
            // =>
            // select * from (select top s * from (select top s + t from table order by x) order by -x) order by x

            select = (SelectExpression)base.VisitSelect(select);

            if (select.Skip != null 
                && select.Take != null 
                && select.OrderBy.Count > 0)
            {
                var skip = select.Skip;
                var take = select.Take;
                var skipPlusTake = PartialEvaluator.Eval(Expression.Add(skip, take));

                select = select.WithTake(skipPlusTake).WithSkip(null);
                select = select.AddRedundantSelect(_language, new TableAlias());
                select = select.WithTake(take);

                // propogate order-bys to new layer
                select = (SelectExpression)select.MoveOrderByToOuterSelect(_language);
                var inverted = select.OrderBy.Select(ob => new OrderExpression(
                    ob.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending,
                    ob.Expression
                    ));
                select = select.WithOrderBy(inverted);

                select = select.AddRedundantSelect(_language, new TableAlias());
                select = select.WithTake(Expression.Constant(0)); // temporary
                select = (SelectExpression)select.MoveOrderByToOuterSelect(_language);
                var reverted = select.OrderBy.Select(ob => new OrderExpression(
                    ob.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending,
                    ob.Expression
                    ));
                select = select.WithOrderBy(reverted);
                select = select.WithTake(null);
            }

            return select;
        }
    }
}