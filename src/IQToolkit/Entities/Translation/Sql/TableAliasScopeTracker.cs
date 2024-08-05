// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;
    using Utils;

    public abstract class TableAliasScopeTracker : SqlExpressionVisitor
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

        protected internal override Expression VisitClientProjection(ClientProjectionExpression original)
        {
            var select = (SelectExpression)this.Visit(original.Select);

            return this.Scope(original.Select, () =>
            {
                var projector = this.Visit(original.Projector);
                return original.Update(select, projector!, original.Aggregator);
            });
        }

        protected internal override Expression VisitClientJoin(ClientJoinExpression original)
        {
            var outerKey = original.OuterKey.Rewrite(this);
            
            return this.Scope(original.Projection.Select, () =>
            {
                var projection = (ClientProjectionExpression)this.VisitClientProjection(original.Projection);
                var innerKey = original.InnerKey.Rewrite(this);
                return original.Update(projection!, outerKey, innerKey!);
            });
        }

        protected internal override Expression VisitSelect(SelectExpression original)
        {
            var from = this.Visit(original.From!);

            return this.Scope(original.From, () =>
            {
                var where = this.Visit(original.Where);
                var orderBy = original.OrderBy.Rewrite(o => o.Accept(this));
                var groupBy = original.GroupBy.Rewrite(this);
                var skip = this.Visit(original.Skip);
                var take = this.Visit(original.Take);
                var columns = original.Columns.Rewrite(d => d.Accept(this));

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

        protected internal override Expression VisitJoin(JoinExpression original)
        {
            switch (original.JoinType)
            {
                case JoinType.OuterApply:
                case JoinType.CrossApply:
                    var left = this.Visit(original.Left);
                    return this.Scope(original.Left, () =>
                    {
                        var right = this.Visit(original.Right);
                        return this.Scope(original.Right, () =>
                        {
                            var cond = this.Visit(original.Condition);
                            return original.Update(original.JoinType, left, right, cond);
                        });
                    });

                default:
                    return base.VisitJoin(original);
            }
        }
    }
}
