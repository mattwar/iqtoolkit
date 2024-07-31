// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using SqlExpressions;
    using Utils;

    /// <summary>
    /// Removes column declarations in SelectExpression's that are not referenced
    /// </summary>
    public class UnusedColumnRemover
    {
        public static Expression RemoveUnusedColumns(Expression expression, IEnumerable<TableAlias>? aliasesInScope = null)
        {
            var columnsUsed = Referencer.GetSelectColumnsUsed(expression, aliasesInScope);
            var removed = Remover.RemoveUnused(expression, columnsUsed);
            return removed;
        }

        private class Remover : SqlExpressionVisitor
        {
            private readonly ImmutableDictionary<SelectExpression, ImmutableHashSet<string>> _selectColumnsReferenced;

            public Remover(
                ImmutableDictionary<SelectExpression, ImmutableHashSet<string>> selectColumnsReferenced)
            {
                _selectColumnsReferenced = selectColumnsReferenced;
            }

            public static Expression RemoveUnused(Expression expression, ImmutableDictionary<SelectExpression, ImmutableHashSet<string>> selectColumnsReferenced)
            {
                var remover = new Remover(selectColumnsReferenced);
                return remover.Visit(expression);
            }

            protected internal override Expression VisitSelect(SelectExpression original)
            {
                var modified = (SelectExpression)base.VisitSelect(original);

                var newColumns = original.Columns.Rewrite(col =>
                {
                    if (_selectColumnsReferenced.TryGetValue(original, out var names))
                    {
                        return names.Contains(col.Name) ? col : null;
                    }

                    return col;
                });

                return modified.WithColumns(newColumns);
            }
        }

        /// <summary>
        /// Gets a map of all the select expression's columns referenced in the overall query
        /// </summary>
        private class Referencer : TableAliasScopeTracker
        {
            private ImmutableDictionary<TableAlias, SelectExpression> _aliasToSelectMap;
            private ImmutableDictionary<SelectExpression, ImmutableHashSet<string>> _selectColumnsUsed;

            public Referencer(IEnumerable<TableAlias>? aliasesInScope)
                : base(aliasesInScope)

            {                
                _aliasToSelectMap = ImmutableDictionary<TableAlias, SelectExpression>.Empty;
                _selectColumnsUsed = ImmutableDictionary<SelectExpression, ImmutableHashSet<string>>.Empty;
            }

            public static ImmutableDictionary<SelectExpression, ImmutableHashSet<string>> GetSelectColumnsUsed(
                Expression expression, IEnumerable<TableAlias>? aliasesInScope = null)
            {
                var marker = new Referencer(aliasesInScope);
                marker.Visit(expression);
                return marker._selectColumnsUsed;
            }

            protected override void AddAliasToScope(AliasedExpression aliased)
            {
                base.AddAliasToScope(aliased);

                if (aliased is SelectExpression select)
                {
                    _aliasToSelectMap = _aliasToSelectMap.SetItem(aliased.Alias, select);
                }
            }

            private void MarkColumn(TableAlias alias, string columnName)
            {
                // add name of column referenced for select expression currently associated with the alias
                if (_aliasToSelectMap.TryGetValue(alias, out var select))
                {
                    MarkColumn(select, columnName);
                }
            }

            private void MarkColumn(SelectExpression select, string columnName)
            {
                if (!_selectColumnsUsed.TryGetValue(select, out var columns))
                    columns = ImmutableHashSet<string>.Empty;

                var newColumns = columns.Add(columnName);
                _selectColumnsUsed = _selectColumnsUsed.SetItem(select, newColumns);
            }

            private void MarkAllColumns(Expression? context)
            {
                switch (context)
                {
                    case SelectExpression select:
                        foreach (var col in select.Columns)
                        {
                            MarkColumn(select, col.Name);
                        }
                        break;

                    case JoinExpression join:
                        MarkAllColumns(join.Left);
                        MarkAllColumns(join.Right);
                        break;
                }
            }

            protected override TExpression Scope<TExpression>(Expression? context, Func<TExpression> action)
            {
                var oldSelectMap = _aliasToSelectMap;
                var result = base.Scope(context, action);
                _aliasToSelectMap = oldSelectMap;
                return result;
            }

            protected internal override Expression VisitColumn(ColumnExpression original)
            {
                MarkColumn(original.Alias, original.Name);
                return original;
            }

            protected internal override Expression VisitScalarSubquery(ScalarSubqueryExpression original)
            {
                // scalar subquery reference its select column
                System.Diagnostics.Debug.Assert(original.Select.Columns.Count == 1);
                MarkColumn(original.Select, original.Select.Columns[0].Name);
                return base.VisitScalarSubquery(original);
            }

            protected internal override Expression VisitInSubquery(InSubqueryExpression original)
            {
                // in subquery references its select column
                System.Diagnostics.Debug.Assert(original.Select.Columns.Count == 1);
                MarkColumn(original.Select, original.Select.Columns[0].Name);
                return base.VisitInSubquery(original);
            }

            protected internal override Expression VisitSelect(SelectExpression original)
            {
                if (original.IsDistinct)
                {
                    // mark all columns in distinct select as used
                    MarkAllColumns(original);
                }

                // if select has a count(*) aggregate then all from subquery columns are used
                if (original.Columns.Any(c => HasCountAllAggregate(c.Expression)))
                {
                    MarkAllColumns(original.From);
                }

                return base.VisitSelect(original);
            }

            protected static bool HasCountAllAggregate(Expression expression)
            {
                return expression.FindFirstDownOrDefault<AggregateExpression>(
                    a => a.AggregateName == "Count" && a.Argument == null
                    ) != null;
            }
        }
    }
}