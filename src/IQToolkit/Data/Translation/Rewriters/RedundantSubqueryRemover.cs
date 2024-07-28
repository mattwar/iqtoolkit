// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;
    using Utils;

    /// <summary>
    /// Removes select expressions that don't add any additional semantic value
    /// </summary>
    public class RedundantSubqueryRemover : DbExpressionRewriter
    {
        public RedundantSubqueryRemover() 
        {
        }

        public static Expression RemoveRedudantSuqueries(Expression expression)
        {
            return new RedundantSubqueryRemover().Rewrite(expression);
        }

        protected override Expression RewriteSelect(SelectExpression select)
        {
            var modified = (SelectExpression)base.RewriteSelect(select);

            if (modified.From is SelectExpression modifiedSelect
                && IsRedudantSubquery(modifiedSelect))
            {
                var removed = SubqueryRemover.Remove(modified, modifiedSelect);
                return removed;
            }

            return modified;
        }

#if false
        private static IReadOnlyList<SelectExpression> GetRedundantSubqueries(SelectExpression select) =>
            select.From != null
                ? select.From.FindAll<SelectExpression>(
                    s => IsRedudantSubquery(s),
                    e => !(e is SubqueryExpression)
                    )
                : ReadOnlyList<SelectExpression>.Empty;
#endif

        protected override Expression RewriteClientProjection(ClientProjectionExpression original)
        {
            var modified = (ClientProjectionExpression)base.RewriteClientProjection(original);
            
            if (modified.Select.From is SelectExpression fromSelect
                && IsRedudantSubquery(fromSelect)) 
            {
                var removed = SubqueryRemover.Remove(modified, fromSelect);
                return removed;
            }

            return original;
        }

        internal static bool IsSimpleProjection(SelectExpression select)
        {
            foreach (ColumnDeclaration decl in select.Columns)
            {
                var col = decl.Expression as ColumnExpression;
                if (col == null || decl.Name != col.Name)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The select returns the same exact columns as the from's select.
        /// </summary>
        internal static bool IsNameMapProjection(SelectExpression select)
        {
            if (select.From is SelectExpression fromSelect)
            {
                if (select.Columns.Count != fromSelect.Columns.Count)
                    return false;

                var fromColumns = fromSelect.Columns;

                // test that all columns in 'select' are refering to columns in the same position in from.
                for (int i = 0, n = select.Columns.Count; i < n; i++)
                {
                    var col = select.Columns[i].Expression as ColumnExpression;
                    if (col == null || !(col.Name == fromColumns[i].Name))
                        return false;
                }

                return true;
            }

            return false;
        }

        internal static bool IsInitialProjection(SelectExpression select)
        {
            return select.From is TableExpression;
        }

        internal static bool IsRedudantSubquery(SelectExpression select)
        {
            return (IsSimpleProjection(select) || IsNameMapProjection(select))
                && !select.IsDistinct
                && !select.IsReverse
                && select.Take == null
                && select.Skip == null
                && select.Where == null
                && select.OrderBy.Count == 0
                && select.GroupBy.Count == 0;
        }

#if false
        class RedundantSubqueryGatherer : DbExpressionRewriter
        {
            private List<SelectExpression>? _redundant;

            private RedundantSubqueryGatherer()
            {
            }

            internal static IReadOnlyList<SelectExpression> Gather(Expression? source)
            {
                RedundantSubqueryGatherer gatherer = new RedundantSubqueryGatherer();
                gatherer.RewriteN(source);
                return gatherer._redundant ?? ReadOnlyList<SelectExpression>.Empty; 
            }

            public override Expression Rewrite(Expression exp)
            {
                if (exp is SubqueryExpression)
                {
                    // don't visit inside subqueries
                    return exp;
                }
                else if (exp is SelectExpression select)
                {
                    if (IsRedudantSubquery(select))
                    {
                        if (_redundant == null)
                        {
                            _redundant = new List<SelectExpression>();
                        }

                        _redundant.Add(select);
                    }

                    return select;
                }
                else
                {
                    return base.Rewrite(exp);
                }
            }
        }
#endif
    }
}