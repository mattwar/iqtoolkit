// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using SqlExpressions;

    /// <summary>
    /// Rewrites skip expressions into uses of TSQL row_number function
    /// </summary>
    public class SkipToRowNumberRewriter : DbExpressionVisitor
    {
        private readonly QueryLanguage _language;

        private SkipToRowNumberRewriter(QueryLanguage language)
        {
            _language = language;
        }

        public static Expression Rewrite(QueryLanguage language, Expression expression)
        {
            return new SkipToRowNumberRewriter(language).Visit(expression);
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression)base.VisitSelect(select);
            if (select.Skip != null)
            {
                var newSelect = select.WithSkip(null).WithTake(null);
                bool canAddColumn = !select.IsDistinct && (select.GroupBy == null || select.GroupBy.Count == 0);
                if (!canAddColumn)
                {
                    newSelect = newSelect.AddRedundantSelect(_language, new TableAlias());
                }
                var colType = _language.TypeSystem.GetQueryType(typeof(int));
                newSelect = newSelect.AddColumn(new ColumnDeclaration("_rownum", new RowNumberExpression(select.OrderBy), colType));

                // add layer for WHERE clause that references new rownum column
                newSelect = newSelect.AddRedundantSelect(_language, new TableAlias());
                newSelect = newSelect.RemoveColumn(newSelect.Columns.Single(c => c.Name == "_rownum"));

                var newAlias = ((SelectExpression)newSelect.From!).Alias;
                ColumnExpression rnCol = new ColumnExpression(typeof(int), colType, newAlias, "_rownum");
                Expression where;

                if (select.Take != null)
                {
                    where = new BetweenExpression(rnCol, Expression.Add(select.Skip, Expression.Constant(1)), Expression.Add(select.Skip, select.Take));
                }
                else
                {
                    where = rnCol.GreaterThan(select.Skip);
                }

                if (newSelect.Where != null)
                {
                    where = newSelect.Where.And(where);
                }

                newSelect = newSelect.WithWhere(where);

                select = newSelect;
            }

            return select;
        }
    }
}