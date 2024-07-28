// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    public abstract class TableAliasScopeTracker : DbExpressionRewriter
    {
        private ImmutableHashSet<TableAlias> _aliasesInScope;

        protected TableAliasScopeTracker(IEnumerable<TableAlias>? aliasesInScope)
        {
            _aliasesInScope = aliasesInScope != null
                ? aliasesInScope.ToImmutableHashSet()
                : ImmutableHashSet<TableAlias>.Empty;
        }

        protected bool IsInScope(TableAlias alias)
        {
            return _aliasesInScope.Contains(alias);
        }

        /// <summary>
        /// Executes the action with aliases declared the context in scope.
        /// </summary>
        protected virtual TExpression Scope<TExpression>(Expression? context, Func<TExpression> action)
            where TExpression : Expression
        {
            var oldAliases = _aliasesInScope;
            AddAliasesToScope(context);
            var result = action();
            _aliasesInScope = oldAliases;
            return result;
        }

        protected virtual void AddAliasToScope(AliasedExpression aliased)
        {
            _aliasesInScope = _aliasesInScope.Add(aliased.Alias);
        }

        protected virtual void AddAliasesToScope(Expression? from)
        {
            switch (from)
            {
                case AliasedExpression aliased:
                    AddAliasToScope(aliased);
                    break;

                case JoinExpression join:
                    AddAliasesToScope(join.Left);
                    AddAliasesToScope(join.Right);
                    break;
            }
        }

        protected override Expression RewriteClientProjection(ClientProjectionExpression original)
        {
            var select = (SelectExpression)this.Rewrite(original.Select);

            return this.Scope(original.Select, () =>
            {
                var projector = this.Rewrite(original.Projector);
                return original.Update(select, projector!, original.Aggregator);
            });
        }

        protected override Expression RewriteClientJoin(ClientJoinExpression original)
        {
            var outerKey = this.RewriteExpressionList(original.OuterKey);
            
            return this.Scope(original.Projection.Select, () =>
            {
                var projection = (ClientProjectionExpression)this.RewriteClientProjection(original.Projection);
                var innerKey = this.RewriteExpressionList(original.InnerKey);
                return original.Update(projection!, outerKey, innerKey!);
            });
        }

        protected override Expression RewriteSelect(SelectExpression original)
        {
            var from = this.RewriteN(original.From);

            return this.Scope(original.From, () =>
            {
                var where = this.RewriteN(original.Where);
                var orderBy = this.VisitOrderExpressions(original.OrderBy);
                var groupBy = this.RewriteExpressionList(original.GroupBy);
                var skip = this.RewriteN(original.Skip);
                var take = this.RewriteN(original.Take);
                var columns = this.RewriteColumnDeclarations(original.Columns);

                return original.Update(
                    original.Alias,
                    from,
                    where,
                    orderBy,
                    groupBy,
                    skip,
                    take,
                    original.IsDistinct,
                    original.IsReverse,
                    columns
                    );
            });
        }

        protected override Expression RewriteJoin(JoinExpression original)
        {
            switch (original.JoinType)
            {
                case JoinType.OuterApply:
                case JoinType.CrossApply:
                    var left = this.Rewrite(original.Left);
                    return this.Scope(original.Left, () =>
                    {
                        var right = this.Rewrite(original.Right);
                        return this.Scope(original.Right, () =>
                        {
                            var cond = this.RewriteN(original.Condition);

                            return original.Update(original.JoinType, left, right, cond);
                        });
                    });

                default:
                    return base.RewriteJoin(original);
            }
        }
    }
}
