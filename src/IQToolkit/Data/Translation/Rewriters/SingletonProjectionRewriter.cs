// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;


#if true
    /// <summary>
    /// Rewrites nested singleton projection into server-side joins
    /// </summary>
    public class SingletonProjectionRewriter : DbExpressionRewriter
    {
        private readonly QueryLanguage _language;

        public SingletonProjectionRewriter(QueryLanguage language)
        {
            _language = language;
        }

        public override Expression Rewrite(Expression exp)
        {
            if (exp is SubqueryExpression)
            {
                // do not consider subqueries... Reconsider?
                return exp;
            }

            return base.Rewrite(exp);
        }

        protected override Expression RewriteSelect(SelectExpression original)
        {
            // find all client projections in this select
            var nestedClientProjections =
                original.FindAll<ClientProjectionExpression>(
                    fnMatch: cp =>
                        cp.Select.From != null
                        && cp.IsSingleton
                        && CanJoinOnServer(cp.Select),
                    fnDescend: ex => !(ex is SelectExpression) || ex == original
                    );

            if (nestedClientProjections.Count > 0)
            {
                var newFrom = original.From;

                var altExpressions = new List<Expression>();
                foreach (var cp in nestedClientProjections)
                {
                    var cpDuped = new ClientProjectionExpression(cp.Select, cp.Projector.Duplicate());
                    var cpWithTest = _language.AddOuterJoinTest(cpDuped);
                    var newSelect = cpWithTest.Select;
                    newFrom = new JoinExpression(JoinType.OuterApply, newFrom!, newSelect, null);
                    altExpressions.Add(cpWithTest.Projector);
                }

                var modified =
                    original
                        .WithFrom(newFrom)
                        .ReplaceAll(nestedClientProjections, altExpressions);

                return this.RewriteSelect(modified);
            }
            else
            {
                return base.RewriteSelect(original);
            }
        }

        protected override Expression RewriteClientProjection(ClientProjectionExpression original)
        {
            // find all client projections in the projector
            var nestedClientProjections =
                original.Projector.FindAll<ClientProjectionExpression>(
                    fnMatch: cp =>
                        cp.Select.From != null
                        && cp.IsSingleton
                        && CanJoinOnServer(cp.Select),
                    fnDescend: ex => !(ex is ClientProjectionExpression)
                    );

            if (nestedClientProjections.Count > 0)
            {
                var newProjection = original;

                var altExpressions = new List<Expression>();
                foreach (var cp in nestedClientProjections)
                {
                    TableAlias newAlias = new TableAlias();
                    var extraSelect = newProjection.Select.AddRedundantSelect(_language, newAlias);

                    // remap any references to the outer select to the new alias;
                    var source = cp.Select.RemapTableAliases(newAlias, extraSelect.Alias);

                    // add outer-join test
                    var pex = _language.AddOuterJoinTest(new ClientProjectionExpression(source, cp.Projector));
                    var pc = ColumnProjector.ProjectColumns(_language, pex.Projector, extraSelect.Columns, extraSelect.Alias, newAlias, cp.Select.Alias);
                    var join = new JoinExpression(JoinType.OuterApply, extraSelect.From!, pex.Select, null);
                    var newSelect = new SelectExpression(extraSelect.Alias, pc.Columns, join, null);

                    newProjection = newProjection.Update(newSelect, newProjection.Projector, newProjection.Aggregator);

                    altExpressions.Add(pc.Projector);
                }

                var finalProjection = newProjection
                    .ReplaceAll(nestedClientProjections, altExpressions);

                return this.RewriteClientProjection(finalProjection);
            }
            else
            {
                return base.RewriteClientProjection(original);
            }
        }

        private bool CanJoinOnServer(SelectExpression select)
        {
            // can add singleton (1:0,1) join if no grouping/aggregates or distinct
            return !select.IsDistinct
                && (select.GroupBy == null || select.GroupBy.Count == 0)
                && !AggregateChecker.HasAggregates(select);
        }

        private static bool IsPredicate(Expression ex)
        {
            if (ex is DbExpression dbx)
            {
                return dbx.IsPredicate;
            }
            else
            {
                switch (ex.NodeType)
                {
                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                        return true;
                    case ExpressionType.Not:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private static bool IsScalarContet(Expression? context)
        {
            return context is UnaryExpression
                || context is BinaryExpression
                || context is ConditionalExpression
                || context is MethodCallExpression;
        }
    }

#else
        /// <summary>
        /// Rewrites nested singleton projection into server-side joins
        /// </summary>
        public class SingletonProjectionRewriter : DbExpressionRewriter
    {
        private readonly QueryLanguage _language;
        private bool _isTopLevel = true;
        private SelectExpression? _currentSelect;
        private Expression? _context;

        public SingletonProjectionRewriter(QueryLanguage language)
        {
            _language = language;
        }

        public override Expression Rewrite(Expression exp)
        {
            if (exp is SubqueryExpression)
            {
                // do not consider subqueries
                return exp;
            }
            else if (exp is CommandExpression)
            {
                _isTopLevel = true;
            }

            var oldContext = _context;
            _context = exp;
            var result = base.Rewrite(exp);
            _context = oldContext;
            return result;
        }

        protected override Expression RewriteClientJoin(ClientJoinExpression join)
        {
            // treat client joins as new top level
            var saveTop = _isTopLevel;
            var saveSelect = _currentSelect;
            _isTopLevel = true;
            _currentSelect = null;
            var result = base.RewriteClientJoin(join);
            _isTopLevel = saveTop;
            _currentSelect = saveSelect;
            return result;
        }

        protected override Expression RewriteClientProjection(ClientProjectionExpression proj)
        {
            if (_isTopLevel)
            {
                _isTopLevel = false;
                _currentSelect = proj.Select;
                var rewrittenProj = base.RewriteClientProjection(proj);
                return proj.Update(_currentSelect, rewrittenProj, proj.Aggregator);
            }

            if (proj.IsSingleton)
            {
                // can convert to join?
                if (_currentSelect != null
                    && _currentSelect.From != null
                    && this.CanJoinOnServer(_currentSelect))
                {
                    TableAlias newAlias = new TableAlias();
                    var extraSelect = _currentSelect.AddRedundantSelect(_language, newAlias);

                    // remap any references to the outer select to the new alias;
                    var source = proj.Select.RemapTableAliases(newAlias, extraSelect.Alias);

                    // add outer-join test
                    var pex = _language.AddOuterJoinTest(new ClientProjectionExpression(source, proj.Projector));

                    var pc = ColumnProjector.ProjectColumns(_language, pex.Projector, extraSelect.Columns, extraSelect.Alias, newAlias, proj.Select.Alias);

                    var join = new JoinExpression(JoinType.OuterApply, extraSelect.From!, pex.Select, null);

                    _currentSelect = new SelectExpression(extraSelect.Alias, pc.Columns, join, null);

                    return this.Rewrite(pc.Projector);
                }
                else 
                {
                    // convert to scalar subquery
                    TableAlias newAlias = new TableAlias();
                    var pc = ColumnProjector.ProjectColumns(_language, proj.Projector, proj.Select.Columns, proj.Select.Alias, newAlias, proj.Select.Alias);

                    var subquery = new ScalarSubqueryExpression(
                        proj.Projector.Type,
                        new SelectExpression(newAlias, pc.Columns, proj.Select, null)
                        );

                    return this.Rewrite(subquery);
                }
            }

            var saveTop = _isTopLevel;
            var saveSelect = _currentSelect;
            _isTopLevel = true;
            _currentSelect = null;
            var result = base.RewriteClientProjection(proj);
            _isTopLevel = saveTop;
            _currentSelect = saveSelect;

            return result;
        }

        private bool CanJoinOnServer(SelectExpression select)
        {
            // can add singleton (1:0,1) join if no grouping/aggregates or distinct
            return !select.IsDistinct
                && (select.GroupBy == null || select.GroupBy.Count == 0)
                && !AggregateChecker.HasAggregates(select);
        }

        private static bool IsPredicate(Expression ex)
        {
            if (ex is DbExpression dbx)
            {
                return dbx.IsPredicate;
            }
            else
            {
                switch (ex.NodeType)
                {
                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                        return true;
                    case ExpressionType.Not:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private static bool IsScalarContet(Expression? context)
        {
            return context is UnaryExpression
                || context is BinaryExpression
                || context is ConditionalExpression
                || context is MethodCallExpression;
        }
    }
#endif
}