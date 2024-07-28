// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    /// <summary>
    /// Checks for invalid column references in a query expression,
    /// such as reference to table alias not in scope or columns not selected.
    /// Also checks for duplicate table alias declarations.
    /// </summary>
    public class ValidReferenceChecker : DbExpressionRewriter
    {
        private ImmutableDictionary<TableAlias, AliasedExpression> _aliasesInScope;
        private ImmutableList<ColumnExpression> _invalidReferences;
        private ImmutableList<AliasedExpression> _invalidDeclarations;

        public ValidReferenceChecker(
            SelectExpression? outerSelect)
        {
            _aliasesInScope = ImmutableDictionary<TableAlias, AliasedExpression>.Empty;
            _invalidReferences = ImmutableList<ColumnExpression>.Empty;
            _invalidDeclarations = ImmutableList<AliasedExpression>.Empty;

            this.AddFromAliases(outerSelect);
        }

        public static bool HasValidReferences(
            Expression query,
            SelectExpression? outerSelect)
        {
            var rewriter = new ValidReferenceChecker(outerSelect);
            rewriter.Rewrite(query);
            return rewriter._invalidReferences.Count == 0
                && rewriter._invalidDeclarations.Count == 0;
        }

        private void AddToScope(AliasedExpression aliased)
        {
            // if alias already in scope, mark as invalid declaration
            if (_aliasesInScope.ContainsKey(aliased.Alias))
            {
                _invalidDeclarations = _invalidDeclarations.Add(aliased);
            }

            // add to scope
            _aliasesInScope = _aliasesInScope.SetItem(aliased.Alias, aliased);
        }

        private void AddFromAliases(Expression? from)
        {
            switch (from)
            {
                case AliasedExpression aliased:
                    AddToScope(aliased);
                    break;
                case JoinExpression join:
                    AddFromAliases(join.Left);
                    AddFromAliases(join.Right);
                    break;
            }
        }

        private void AddInvalidReference(ColumnExpression column)
        {
            _invalidReferences = _invalidReferences.Add(column);
        }

        public override Expression Rewrite(Expression exp)
        {
            // scope changes don't propogate outward
            var oldAliases = _aliasesInScope;
            var result = base.Rewrite(exp);
            _aliasesInScope = oldAliases;
            return result;
        }

        protected override Expression RewriteClientProjection(ClientProjectionExpression original)
        {
            this.Rewrite(original.Select);

            // put select's alias in scope for projection & aggregate
            this.AddToScope(original.Select);

            this.Rewrite(original.Projector);
            this.RewriteN(original.Aggregator);

            return original;
        }

        protected override Expression RewriteSelect(SelectExpression original)
        {
            var from = this.RewriteN(original.From);

            // add from aliases to scope
            this.AddFromAliases(original.From);

            var where = this.RewriteN(original.Where);
            var orderBy = this.VisitOrderExpressions(original.OrderBy);
            var groupBy = this.RewriteExpressionList(original.GroupBy);
            var skip = this.RewriteN(original.Skip);
            var take = this.RewriteN(original.Take);
            var columns = this.RewriteColumnDeclarations(original.Columns);

            return original;
        }

        protected override Expression RewriteJoin(JoinExpression original)
        {
            switch (original.JoinType)
            {
                // right side of apply join can refer to left side's aliases
                case JoinType.CrossApply:
                case JoinType.OuterApply:
                    {
                        var left = this.Rewrite(original.Left);

                        // right can see aliases from left
                        this.AddFromAliases(original.Left);
                        var right = this.Rewrite(original.Right);

                        return original;
                    }

                default:
                    {
                        var left = this.Rewrite(original.Left);
                        var right = this.Rewrite(original.Right);

                        // condition can see aliases from left and right
                        if (original.Condition != null)
                        {
                            this.AddFromAliases(original);
                            var cond = this.Rewrite(original.Condition);
                        }

                        return original;
                    }
            }
        }

        protected override Expression RewriteClientJoin(ClientJoinExpression original)
        {
            // outer key can see existing aliases in scope only.
            var outerKey = this.RewriteExpressionList(original.OuterKey);

            var projection = this.Rewrite(original.Projection);

            // inner key can see projection select's alias
            this.AddToScope(original.Projection.Select);
            var innerKey = this.RewriteExpressionList(original.InnerKey);

            return original;
        }

        protected override Expression RewriteColumn(ColumnExpression original)
        {
            // check to see if column refers to alias in scope
            if (!_aliasesInScope.TryGetValue(original.Alias, out var aliased))
            {
                this.AddInvalidReference(original);
            }

            // check to see if column refers to column in select 
            if (aliased is SelectExpression select)
            {
                if (!select.Columns.Any(c => c.Name == original.Name))
                    this.AddInvalidReference(original);
            }

            return original;
        }
    }
}