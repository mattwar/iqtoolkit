// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Immutable;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    /// <summary>
    /// Merges select expressions with their immediate nested select (from)
    /// if their parts of mergeable.
    /// </summary>
    public class SubqueryMerger : DbExpressionRewriter
    {
        private SubqueryMerger()
        {
        }

        internal static Expression Merge(Expression expression)
        {
            return new SubqueryMerger().Rewrite(expression);
        }

        private bool _isTopLevel = true;

        protected override Expression RewriteSelect(SelectExpression select)
        {
            bool wasTopLevel = _isTopLevel;
            _isTopLevel = false;

            select = (SelectExpression)base.RewriteSelect(select);

            // next attempt to merge subqueries that would have been removed by the above
            // logic except for the existence of a where clause
            while (true)
            {
                if (CanMergeWithFrom(select, wasTopLevel))
                {
                    var fromSelect = GetLeftMostSelect(select.From);
                    if (fromSelect == null)
                        break;

                    // remove the redundant subquery
                    select = SubqueryRemover.Remove(select, fromSelect);

                    // merge where expressions 
                    var where = select.Where;
                    if (fromSelect.Where != null)
                    {
                        if (where != null)
                        {
                            where = fromSelect.Where.And(where);
                        }
                        else
                        {
                            where = fromSelect.Where;
                        }
                    }

                    var orderBy = select.OrderBy.Count > 0 ? select.OrderBy : fromSelect.OrderBy;
                    var groupBy = select.GroupBy.Count > 0 ? select.GroupBy : fromSelect.GroupBy;
                    var skip = select.Skip != null ? select.Skip : fromSelect.Skip;
                    var take = select.Take != null ? select.Take : fromSelect.Take;
                    bool isDistinct = select.IsDistinct | fromSelect.IsDistinct;
                    bool isReverse = select.IsReverse | fromSelect.IsReverse;

                    select = select.Update(select.Alias, select.From, where, orderBy, groupBy, skip, take, isDistinct, isReverse, select.Columns);
                }
                else if (IsSimpleColumnReprojection(select)
                    && select.From is SelectExpression fromSel)
                {
                    select = fromSel.WithAlias(select.Alias);
                }
                else
                {
                    break;
                }
            }

            return select;
        }

        private static SelectExpression? GetLeftMostSelect(Expression? source)
        {
            if (source is SelectExpression select)
            {
                return select;
            }
            else if (source is JoinExpression join)
            {
                return GetLeftMostSelect(join.Left);
            }
            else
            {
                return null;
            }
        }

        private static bool IsColumnProjection(SelectExpression select)
        {
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                var col = select.Columns[i];

                if (!(col.Expression is ColumnExpression || col.Expression is ConstantExpression))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// True if the select expression is just reprojecting columns from the from clause
        /// with the same names.
        /// </summary>
        private static bool IsSimpleColumnReprojection(SelectExpression select)
        {
            if (!(select.From is SelectExpression fromSelect))
                return false;

            foreach (var col in select.Columns)
            {
                if (!(col.Expression is ColumnExpression fromCol)
                    || col.Name != fromCol.Name)
                {
                    return false;
                }
            }

            return select.Where == null
                && select.Skip == null
                && select.Take == null
                && select.OrderBy.Count == 0
                && select.GroupBy.Count == 0
                && !select.IsDistinct
                && !select.IsReverse;
        }

        private static bool CanMergeWithFrom(SelectExpression select, bool isTopLevel)
        {
            var fromSelect = GetLeftMostSelect(select.From!);
            if (fromSelect == null)
                return false;
            if (!IsColumnProjection(fromSelect))
                return false;
            bool selHasNameMapProjection = RedundantSubqueryRemover.IsNameMapProjection(select);
            bool selHasOrderBy = select.OrderBy.Count > 0;
            bool selHasGroupBy = select.GroupBy.Count > 0;
            bool selHasAggregates = AggregateChecker.HasAggregates(select);
            bool selHasJoin = select.From is JoinExpression;
            bool frmHasOrderBy = fromSelect.OrderBy.Count > 0;
            bool frmHasGroupBy = fromSelect.GroupBy.Count > 0;
            bool frmHasAggregates = AggregateChecker.HasAggregates(fromSelect);
            // both cannot have orderby
            if (selHasOrderBy && frmHasOrderBy && !HasSameOrderBy(select, fromSelect))
                return false;
            // both cannot have groupby
            if (selHasGroupBy && frmHasGroupBy)
                return false;
            // these are distinct operations 
            if (select.IsReverse || fromSelect.IsReverse)
                return false;
            // cannot move forward order-by if outer has group-by
            if (frmHasOrderBy && (selHasGroupBy || selHasAggregates || select.IsDistinct))
                return false;
            // cannot move forward group-by if outer has where clause
            if (frmHasGroupBy /*&& (select.Where != null)*/) // need to assert projection is the same in order to move group-by forward
                return false;
            // cannot move forward a take if outer has take or skip or distinct
            if (fromSelect.Take != null && (select.Take != null || select.Skip != null || select.IsDistinct || selHasAggregates || selHasGroupBy || selHasJoin))
                return false;
            // cannot move forward a skip if outer has skip or distinct
            if (fromSelect.Skip != null && (select.Skip != null || select.IsDistinct || selHasAggregates || selHasGroupBy || selHasJoin))
                return false;
            // cannot move forward a distinct if outer has take, skip, groupby or a different projection
            if (fromSelect.IsDistinct && (select.Take != null || select.Skip != null || !selHasNameMapProjection || selHasGroupBy || selHasAggregates || (selHasOrderBy && !isTopLevel) || selHasJoin))
                return false;
            if (frmHasAggregates && (select.Take != null || select.Skip != null || select.IsDistinct || selHasAggregates || selHasGroupBy || selHasJoin))
                return false;

            // everything looks good for merge.
            return true;
        }

        private static bool HasSameOrderBy(
            SelectExpression select, 
            SelectExpression fromSelect)
        {
            var map = ImmutableDictionary<TableAlias, TableAlias>.Empty.Add(select.Alias, fromSelect.Alias);

            if (select.OrderBy.Count != fromSelect.OrderBy.Count)
                return false;

            for (int i = 0, n = select.OrderBy.Count; i < n; i++)
            {
                if (select.OrderBy[i].OrderType != fromSelect.OrderBy[i].OrderType)
                    return false;

                if (!DbExpressionComparer.Default.Equals(select.OrderBy[i].Expression, fromSelect.OrderBy[i].Expression, map))
                    return false;
            }

            return true;
        }
    }
}