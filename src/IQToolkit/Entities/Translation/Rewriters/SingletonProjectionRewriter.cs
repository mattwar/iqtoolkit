// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;

    /// <summary>
    /// Rewrites nested singleton projections into server-side joins.
    /// </summary>
    public class SingletonProjectionRewriter : SqlExpressionVisitor
    {
        private readonly QueryLanguage _language;

        public SingletonProjectionRewriter(QueryLanguage language)
        {
            _language = language;
        }

        public override Expression Visit(Expression exp)
        {
            if (exp is SubqueryExpression)
            {
                // do not consider subqueries... Reconsider?
                return exp;
            }

            return base.Visit(exp);
        }

        protected internal override Expression VisitSelect(SelectExpression original)
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

                return this.VisitSelect(modified);
            }
            else
            {
                return base.VisitSelect(original);
            }
        }

        protected internal override Expression VisitClientProjection(ClientProjectionExpression original)
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

                return this.VisitClientProjection(finalProjection);
            }
            else
            {
                return base.VisitClientProjection(original);
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
            if (ex is SqlExpression dbx)
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
}