// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    /// <summary>
    /// Moves order-bys to the outermost select if possible
    /// </summary>
    public class MoveOrderByToOuterMostSelectRewriter : DbExpressionVisitor
    {
        private readonly QueryLanguage _language;
        private List<OrderExpression>? _gatheredOrderings;
        private bool _isOuterMostSelect;

        public MoveOrderByToOuterMostSelectRewriter(QueryLanguage language)
        {
            _language = language;
            _isOuterMostSelect = true;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            var saveIsOuterMostSelect = _isOuterMostSelect;
            try
            {
                _isOuterMostSelect = false;
                select = (SelectExpression)base.VisitSelect(select);

                var hasOrderBy = select.OrderBy.Count > 0;
                var hasGroupBy = select.GroupBy.Count > 0;
                var canHaveOrderBy = saveIsOuterMostSelect 
                        || select.Take != null 
                        || select.Skip != null;
                var canReceiveOrderings = canHaveOrderBy && !hasGroupBy && !select.IsDistinct && !AggregateChecker.HasAggregates(select);

                if (hasOrderBy)
                {
                    this.PrependOrderings(select.OrderBy);
                }

                if (select.IsReverse)
                {
                    this.ReverseOrderings();
                }

                IReadOnlyList<OrderExpression>? orderings = null;
                if (canReceiveOrderings)
                {
                    orderings = _gatheredOrderings;
                }
                else if (canHaveOrderBy)
                {
                    orderings = select.OrderBy;
                }

                var canPassOnOrderings = !saveIsOuterMostSelect && !hasGroupBy && !select.IsDistinct;
                var columns = select.Columns;

                if (_gatheredOrderings != null)
                {
                    if (canPassOnOrderings)
                    {
                        var producedAliases = DeclaredAliasGatherer.Gather(select.From);
                        // reproject order expressions using this select's alias so the outer select will have properly formed expressions
                        BindResult project = this.RebindOrderings(_gatheredOrderings, select.Alias, producedAliases, select.Columns);
                        _gatheredOrderings = null;
                        this.PrependOrderings(project.Orderings);
                        columns = project.Columns;
                    }
                    else
                    {
                        _gatheredOrderings = null;
                    }
                }

                if (orderings != select.OrderBy || columns != select.Columns || select.IsReverse)
                {
                    select = new SelectExpression(select.Alias, columns, select.From, select.Where, orderings, select.GroupBy, select.IsDistinct, select.Skip, select.Take, false);
                }

                return select;
            }
            finally
            {
                _isOuterMostSelect = saveIsOuterMostSelect;
            }
        }

        protected internal override Expression VisitScalarSubquery(ScalarSubqueryExpression scalar)
        {
            var saveOrderings = _gatheredOrderings;
            _gatheredOrderings = null;
            var result = base.VisitScalarSubquery(scalar);
            _gatheredOrderings = saveOrderings;
            return result;
        }

        protected internal override Expression VisitExistsSubquery(ExistsSubqueryExpression exists)
        {
            var saveOrderings = _gatheredOrderings;
            _gatheredOrderings = null;
            var result = base.VisitExistsSubquery(exists);
            _gatheredOrderings = saveOrderings;
            return result;
        }

        protected internal override Expression VisitInSubquery(InSubqueryExpression @in)
        {
            var saveOrderings = _gatheredOrderings;
            _gatheredOrderings = null;
            var result = base.VisitInSubquery(@in);
            _gatheredOrderings = saveOrderings;
            return result;
        }

        protected internal override Expression VisitJoin(JoinExpression join)
        {
            // make sure order by expressions lifted up from the left side are not lost
            // when visiting the right side
            var left = this.Visit(join.Left);
            var leftOrders = _gatheredOrderings;
            _gatheredOrderings = null; // start on the right with a clean slate
            var right = this.Visit(join.Right);
            this.PrependOrderings(leftOrders);
            var condition = this.Visit(join.Condition);
            return join.Update(join.JoinType, left, right, condition);
        }

        /// <summary>
        /// Add a sequence of order expressions to an accumulated list, prepending so as
        /// to give precedence to the new expressions over any previous expressions
        /// </summary>
        /// <param name="newOrderings"></param>
        protected void PrependOrderings(IReadOnlyList<OrderExpression>? newOrderings)
        {
            if (newOrderings != null)
            {
                if (_gatheredOrderings == null)
                {
                    _gatheredOrderings = new List<OrderExpression>();
                }
                for (int i = newOrderings.Count - 1; i >= 0; i--)
                {
                    _gatheredOrderings.Insert(0, newOrderings[i]);
                }
                // trim off obvious duplicates
                HashSet<string> unique = new HashSet<string>();
                for (int i = 0; i < _gatheredOrderings.Count;) 
                {
                    if (_gatheredOrderings[i].Expression is ColumnExpression column)
                    {
                        string hash = column.Alias + ":" + column.Name;
                        if (unique.Contains(hash))
                        {
                            _gatheredOrderings.RemoveAt(i);
                            // don't increment 'i', just continue
                            continue;
                        }
                        else
                        {
                            unique.Add(hash);
                        }
                    }
                    i++;
                }
            }
        }

        protected void ReverseOrderings()
        {
            if (_gatheredOrderings != null)
            {
                for (int i = 0, n = _gatheredOrderings.Count; i < n; i++)
                {
                    var ord = _gatheredOrderings[i];
                    _gatheredOrderings[i] =
                        new OrderExpression(
                            ord.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending,
                            ord.Expression
                            );
                }
            }
        }

        protected class BindResult
        {
            public IReadOnlyList<ColumnDeclaration> Columns { get; }
            public IReadOnlyList<OrderExpression> Orderings { get; }

            public BindResult(
                IEnumerable<ColumnDeclaration> columns, 
                IEnumerable<OrderExpression> orderings)
            {
                this.Columns = columns as IReadOnlyList<ColumnDeclaration>
                    ?? Array.Empty<ColumnDeclaration>();

                this.Orderings = orderings as IReadOnlyList<OrderExpression>
                    ?? Array.Empty<OrderExpression>();
            }
        }

        /// <summary>
        /// Rebind order expressions to reference a new alias and add to column declarations if necessary
        /// </summary>
        protected virtual BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, TableAlias alias, HashSet<TableAlias> existingAliases, IEnumerable<ColumnDeclaration> existingColumns)
        {
            List<ColumnDeclaration>? newColumns = null;
            var newOrderings = new List<OrderExpression>();

            foreach (OrderExpression ordering in orderings)
            {
                var ordExpr = ordering.Expression;
                var ordColumn = ordExpr as ColumnExpression;

                if (ordColumn == null || (existingAliases != null && existingAliases.Contains(ordColumn.Alias)))
                {
                    // check to see if a declared column already contains a similar expression
                    int iOrdinal = 0;
                    foreach (ColumnDeclaration decl in existingColumns)
                    {
                        if (decl.Expression is ColumnExpression declColumn)
                        {
                            if (decl.Expression == ordExpr)
                            {
                                ordExpr = new ColumnExpression(declColumn.Type, declColumn.QueryType, alias, decl.Name);
                                break;
                            }
                            else if (ordColumn != null
                                && ordColumn.Alias == declColumn.Alias
                                && ordColumn.Name == declColumn.Name)
                            {
                                // found it, so make a reference to this column
                                ordExpr = new ColumnExpression(declColumn.Type, declColumn.QueryType, alias, decl.Name);
                                break;
                            }
                        }

                        iOrdinal++;
                    }
                    
                    // if not already projected, add a new column declaration for it
                    if (ordExpr == ordering.Expression)
                    {
                        if (newColumns == null)
                        {
                            newColumns = new List<ColumnDeclaration>(existingColumns);
                            existingColumns = newColumns;
                        }

                        string colName = ordColumn != null ? ordColumn.Name : "c" + iOrdinal;
                        colName = newColumns.GetAvailableColumnName(colName);
                        var colType = _language.TypeSystem.GetQueryType(ordExpr.Type);
                        newColumns.Add(new ColumnDeclaration(colName, ordering.Expression, colType));
                        ordExpr = new ColumnExpression(ordExpr.Type, colType, alias, colName);
                    }

                    newOrderings.Add(new OrderExpression(ordering.OrderType, ordExpr));
                }
            }
            
            return new BindResult(existingColumns, newOrderings);
        }
    }
}
