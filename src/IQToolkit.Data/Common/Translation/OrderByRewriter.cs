﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Moves order-bys to the outermost select if possible
    /// </summary>
    public class OrderByRewriter : DbExpressionVisitor
    {
        readonly QueryLanguage language;
        IList<OrderExpression> gatheredOrderings;
        bool isOuterMostSelect;

        private OrderByRewriter(QueryLanguage language)
        {
            this.language = language;
            this.isOuterMostSelect = true;
        }

        public static Expression Rewrite(QueryLanguage language, Expression expression)
        {
            return new OrderByRewriter(language).Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool saveIsOuterMostSelect = this.isOuterMostSelect;
            try
            {
                this.isOuterMostSelect = false;
                select = (SelectExpression)base.VisitSelect(select);

                bool hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
                bool hasGroupBy = select.GroupBy != null && select.GroupBy.Count > 0;
                bool canHaveOrderBy = saveIsOuterMostSelect || select.Take != null || select.Skip != null;
                bool canReceiveOrderings = canHaveOrderBy && !hasGroupBy && !select.IsDistinct && !AggregateChecker.HasAggregates(select);

                if (hasOrderBy)
                {
                    this.PrependOrderings(select.OrderBy);
                }

                if (select.IsReverse)
                {
                    this.ReverseOrderings();
                }

                IEnumerable<OrderExpression> orderings = null;
                if (canReceiveOrderings)
                {
                    orderings = this.gatheredOrderings;
                }
                else if (canHaveOrderBy)
                {
                    orderings = select.OrderBy;
                }
                bool canPassOnOrderings = !saveIsOuterMostSelect && !hasGroupBy && !select.IsDistinct;
                ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;
                if (this.gatheredOrderings != null)
                {
                    if (canPassOnOrderings)
                    {
                        var producedAliases = DeclaredAliasGatherer.Gather(select.From);
                        // reproject order expressions using this select's alias so the outer select will have properly formed expressions
                        BindResult project = this.RebindOrderings(this.gatheredOrderings, select.Alias, producedAliases, select.Columns);
                        this.gatheredOrderings = null;
                        this.PrependOrderings(project.Orderings);
                        columns = project.Columns;
                    }
                    else
                    {
                        this.gatheredOrderings = null;
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
                this.isOuterMostSelect = saveIsOuterMostSelect;
            }
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            var saveOrderings = this.gatheredOrderings;
            this.gatheredOrderings = null;
            var result = base.VisitSubquery(subquery);
            this.gatheredOrderings = saveOrderings;
            return result;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            // make sure order by expressions lifted up from the left side are not lost
            // when visiting the right side
            Expression left = this.VisitSource(join.Left);
            IList<OrderExpression> leftOrders = this.gatheredOrderings;
            this.gatheredOrderings = null; // start on the right with a clean slate
            Expression right = this.VisitSource(join.Right);
            this.PrependOrderings(leftOrders);
            Expression condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.Join, left, right, condition);
            }
            return join;
        }

        /// <summary>
        /// Add a sequence of order expressions to an accumulated list, prepending so as
        /// to give precedence to the new expressions over any previous expressions
        /// </summary>
        /// <param name="newOrderings"></param>
        protected void PrependOrderings(IList<OrderExpression> newOrderings)
        {
            if (newOrderings != null)
            {
                if (this.gatheredOrderings == null)
                {
                    this.gatheredOrderings = new List<OrderExpression>();
                }
                for (int i = newOrderings.Count - 1; i >= 0; i--)
                {
                    this.gatheredOrderings.Insert(0, newOrderings[i]);
                }
                // trim off obvious duplicates
                HashSet<string> unique = new HashSet<string>();
                for (int i = 0; i < this.gatheredOrderings.Count;) 
                {
                    ColumnExpression column = this.gatheredOrderings[i].Expression as ColumnExpression;
                    if (column != null)
                    {
                        string hash = column.Alias + ":" + column.Name;
                        if (unique.Contains(hash))
                        {
                            this.gatheredOrderings.RemoveAt(i);
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
            if (this.gatheredOrderings != null)
            {
                for (int i = 0, n = this.gatheredOrderings.Count; i < n; i++)
                {
                    var ord = this.gatheredOrderings[i];
                    this.gatheredOrderings[i] =
                        new OrderExpression(
                            ord.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending,
                            ord.Expression
                            );
                }
            }
        }

        protected class BindResult
        {
            readonly ReadOnlyCollection<ColumnDeclaration> columns;
            readonly ReadOnlyCollection<OrderExpression> orderings;
            public BindResult(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orderings)
            {
                this.columns = columns as ReadOnlyCollection<ColumnDeclaration>;
                if (this.columns == null)
                {
                    this.columns = new List<ColumnDeclaration>(columns).AsReadOnly();
                }
                this.orderings = orderings as ReadOnlyCollection<OrderExpression>;
                if (this.orderings == null)
                {
                    this.orderings = new List<OrderExpression>(orderings).AsReadOnly();
                }
            }
            public ReadOnlyCollection<ColumnDeclaration> Columns
            {
                get { return this.columns; }
            }
            public ReadOnlyCollection<OrderExpression> Orderings
            {
                get { return this.orderings; }
            }
        }

        /// <summary>
        /// Rebind order expressions to reference a new alias and add to column declarations if necessary
        /// </summary>
        protected virtual BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, TableAlias alias, HashSet<TableAlias> existingAliases, IEnumerable<ColumnDeclaration> existingColumns)
        {
            List<ColumnDeclaration> newColumns = null;
            List<OrderExpression> newOrderings = new List<OrderExpression>();
            foreach (OrderExpression ordering in orderings)
            {
                Expression expr = ordering.Expression;
                ColumnExpression column = expr as ColumnExpression;
                if (column == null || (existingAliases != null && existingAliases.Contains(column.Alias)))
                {
                    // check to see if a declared column already contains a similar expression
                    int iOrdinal = 0;
                    foreach (ColumnDeclaration decl in existingColumns)
                    {
                        ColumnExpression declColumn = decl.Expression as ColumnExpression;
                        if (decl.Expression == ordering.Expression ||
                            (column != null && declColumn != null && column.Alias == declColumn.Alias && column.Name == declColumn.Name))
                        {
                            // found it, so make a reference to this column
                            expr = new ColumnExpression(column.Type, column.QueryType, alias, decl.Name);
                            break;
                        }
                        iOrdinal++;
                    }
                    // if not already projected, add a new column declaration for it
                    if (expr == ordering.Expression)
                    {
                        if (newColumns == null)
                        {
                            newColumns = new List<ColumnDeclaration>(existingColumns);
                            existingColumns = newColumns;
                        }
                        string colName = column != null ? column.Name : "c" + iOrdinal;
                        colName = newColumns.GetAvailableColumnName(colName);
                        var colType = this.language.TypeSystem.GetColumnType(expr.Type);
                        newColumns.Add(new ColumnDeclaration(colName, ordering.Expression, colType));
                        expr = new ColumnExpression(expr.Type, colType, alias, colName);
                    }
                    newOrderings.Add(new OrderExpression(ordering.OrderType, expr));
                }
            }
            return new BindResult(existingColumns, newOrderings);
        }
    }
}
