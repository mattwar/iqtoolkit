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
        /// <summary>
        /// Returns all the matching expressions in the subtree under this expression.
        /// </summary>
        public static IReadOnlyList<TExpression> FindAll<TExpression>(
            this Expression root,
            Func<TExpression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
            where TExpression : Expression
        {
            List<TExpression>? list = null;

            DbExpressionWalker.Walk(
                root,
                expression =>
                {
                    if (expression is TExpression tex
                        && (fnMatch == null || fnMatch(tex)))
                    {
                        if (list == null)
                            list = new List<TExpression>();
                        list.Add(tex);
                    }
                },
                fnDescend: fnDescend);

            return list.ToReadOnly();
        }

        /// <summary>
        /// Returns all the matching expressions in the subtree under this expression.
        /// </summary>
        public static IReadOnlyList<Expression> FindAll(
            this Expression root,
            Func<Expression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
        {
            return FindAll<Expression>(root, fnMatch, fnDescend);
        }

        /// <summary>
        /// Returns the first matching expression in the subtree under this expression 
        /// or null if no expression matches.
        /// </summary>
        public static TExpression? FindFirstOrDefault<TExpression>(
            this Expression root,
            Func<TExpression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
            where TExpression : Expression
        {
            TExpression? found = null;

            DbExpressionWalker.Walk(
                root,

                fnBefore: expression =>
                {
                    if (found == null
                        && expression is TExpression tex
                        && (fnMatch == null || fnMatch(tex)))
                    {
                        found = tex;
                    }
                },

                fnDescend: e => found == null && (fnDescend == null || fnDescend(e))
                );

            return found;
        }

        /// <summary>
        /// Returns the first matching expression in the subtree under this expression 
        /// or null if no expression matches.
        /// </summary>
        public static Expression? FindFirstOrDefault(
            this Expression root,
            Func<Expression, bool>? fnMatch = null,
            Func<Expression, bool>? fnDescend = null)
        {
            return FindFirstOrDefault<Expression>(root, fnMatch, fnDescend);
        }

        /// <summary>
        /// Replace any number of expressions in the subtree under this expression,
        /// returning the new subtree with the expression replaced.
        /// </summary>
        public static TExpression Replace<TExpression>(
            this TExpression root, 
            Func<Expression, Expression> fnReplacer,
            Func<Expression, bool>? fnDescend = null)
            where TExpression : Expression
        {
            var replacer = new Replacer(fnReplacer, fnDescend);
            return (TExpression)replacer.Rewrite(root);
        }

        /// <summary>
        /// Replaces one expression for another in the subtree under this expression,
        /// returning the new subtree with the expression replaced.
        /// </summary>
        public static TExpression Replace<TExpression>(
            this TExpression root, 
            Expression searchFor, 
            Expression replaceWith)
            where TExpression : Expression
        {
            return Replace(root,
                exp => exp == searchFor ? replaceWith : exp
                );
        }

        /// <summary>
        /// Replace all corresponding expressions in the subtree under this expression,
        /// returning the new subtree with the expressions replaced.
        /// </summary>
        public static TExpression ReplaceAll<TExpression>(
            this TExpression root,
            IReadOnlyList<Expression> searchFor,
            IReadOnlyList<Expression> replaceWith)
            where TExpression : Expression
        {
            var map = new Dictionary<Expression, Expression>(
                Enumerable.Zip(searchFor, replaceWith, (s, r) => KeyValuePair.Create(s, r))
            );

            return Replace(root, exp =>
            {
                if (map.TryGetValue(exp, out var replacement))
                    return replacement;
                return exp;
            });
        }

        private class Replacer : DbExpressionRewriter
        {
            private readonly Func<Expression, Expression> _fnReplacer;
            private readonly Func<Expression, bool>? _fnDescend;

            public Replacer(
                Func<Expression, Expression> fnReplacer,
                Func<Expression, bool>? fnDescend)
            {
                _fnReplacer = fnReplacer;
                _fnDescend = fnDescend;
            }

            public override Expression Rewrite(Expression exp)
            {
                var replaced = _fnReplacer(exp);
                if (replaced != exp)
                {
                    // this expression needs to be replaced, don't bother with the sub-tree.
                    return replaced;
                }
                else if (_fnDescend == null || _fnDescend(exp))
                {
                    // look down the sub-tree
                    return base.Rewrite(exp);
                }
                else
                {
                    return exp;
                }
            }
        }

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
