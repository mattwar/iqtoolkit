// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    using Translation;
    using Utils;

    public static class DbExpressionExtensions
    {
        // These really belond on SelectExpression as first class methods
        #region SelectExpression extensions
        public static string GetAvailableColumnName(
            this IReadOnlyList<ColumnDeclaration> columns,
            string baseName)
        {
            string name = baseName;
            int n = 0;

            while (!IsUniqueName(columns, name))
            {
                name = baseName + (n++);
            }

            return name;
        }

        private static bool IsUniqueName(
            IReadOnlyList<ColumnDeclaration> columns,
            string name)
        {
            foreach (var col in columns)
            {
                if (col.Name == name)
                {
                    return false;
                }
            }
            return true;
        }

        public static ClientProjectionExpression AddOuterJoinTest(
            this ClientProjectionExpression proj,
            QueryLanguage language,
            Expression expression)
        {
            var colName = proj.Select.Columns.GetAvailableColumnName("Test");
            var colType = language.TypeSystem.GetQueryType(expression.Type);
            var newSource = proj.Select.AddColumn(new ColumnDeclaration(colName, expression, colType));
            var newProjector =
                new OuterJoinedExpression(
                    new ColumnExpression(expression.Type, colType, newSource.Alias, colName),
                    proj.Projector
                    );
            return new ClientProjectionExpression(newSource, newProjector, proj.Aggregator);
        }

        public static SelectExpression AddRedundantSelect(this SelectExpression sel, QueryLanguage language, TableAlias newAlias)
        {
            var newColumns =
                from d in sel.Columns
                let qt = (d.Expression is ColumnExpression) ? ((ColumnExpression)d.Expression).QueryType : language.TypeSystem.GetQueryType(d.Expression.Type)
                select new ColumnDeclaration(d.Name, new ColumnExpression(d.Expression.Type, qt, newAlias, d.Name), qt);

            var newFrom = new SelectExpression(newAlias, sel.Columns, sel.From, sel.Where, sel.OrderBy, sel.GroupBy, sel.IsDistinct, sel.Skip, sel.Take, sel.IsReverse);
            return new SelectExpression(sel.Alias, newColumns, newFrom, null, null, null, false, null, null, false);
        }

        public static SelectExpression RemoveRedundantFrom(this SelectExpression select)
        {
            if (select.From is SelectExpression fromSelect)
            {
                return SubqueryRemover.Remove(select, fromSelect);
            }

            return select;
        }

        public static string ToDebugText(this Expression expression) =>
            DbExpressionDebugFormatter.Singleton.Format(expression);
        #endregion
    }
}
