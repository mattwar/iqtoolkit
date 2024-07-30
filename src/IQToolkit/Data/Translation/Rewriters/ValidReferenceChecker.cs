// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;
    using Utils;

    /// <summary>
    /// Checks for invalid column references in a query expression,
    /// such as reference to table alias not in scope or columns not selected.
    /// Also checks for duplicate table alias declarations.
    /// </summary>
    public class ValidReferenceChecker : DbExpressionVisitor
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
            rewriter.Visit(query);
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

        public override Expression Visit(Expression exp)
        {
            // scope changes don't propogate outward
            var oldAliases = _aliasesInScope;
            var result = base.Visit(exp);
            _aliasesInScope = oldAliases;
            return result;
        }

        protected internal override Expression VisitClientProjection(ClientProjectionExpression original)
        {
            this.Visit(original.Select);

            // put select's alias in scope for projection & aggregate
            this.AddToScope(original.Select);

            this.Visit(original.Projector);
            this.Visit(original.Aggregator!);

            return original;
        }

        protected internal override Expression VisitSelect(SelectExpression original)
        {
            var from = this.Visit(original.From!);

            // add from aliases to scope
            this.AddFromAliases(original.From);

            var where = this.Visit(original.Where!);
            var orderBy = original.OrderBy.Rewrite(o => o.Accept(this));
            var groupBy = original.GroupBy.Rewrite(this);
            var skip = this.Visit(original.Skip!);
            var take = this.Visit(original.Take!);
            var columns = original.Columns.Rewrite(d => d.Accept(this));

            return original;
        }

        protected internal override Expression VisitJoin(JoinExpression original)
        {
            switch (original.JoinType)
            {
                // right side of apply join can refer to left side's aliases
                case JoinType.CrossApply:
                case JoinType.OuterApply:
                    {
                        var left = this.Visit(original.Left);

                        // right can see aliases from left
                        this.AddFromAliases(original.Left);
                        var right = this.Visit(original.Right);

                        return original;
                    }

                default:
                    {
                        var left = this.Visit(original.Left);
                        var right = this.Visit(original.Right);

                        // condition can see aliases from left and right
                        if (original.Condition != null)
                        {
                            this.AddFromAliases(original);
                            var cond = this.Visit(original.Condition);
                        }

                        return original;
                    }
            }
        }

        protected internal override Expression VisitClientJoin(ClientJoinExpression original)
        {
            // outer key can see existing aliases in scope only.
            var outerKey = original.OuterKey.Rewrite(this);

            var projection = this.Visit(original.Projection);

            // inner key can see projection select's alias
            this.AddToScope(original.Projection.Select);

            var innerKey = original.InnerKey.Rewrite(this);

            return original;
        }

        protected internal override Expression VisitColumn(ColumnExpression original)
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