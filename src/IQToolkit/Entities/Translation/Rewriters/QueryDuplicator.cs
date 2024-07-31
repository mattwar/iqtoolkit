// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using SqlExpressions;

    /// <summary>
    /// Deep clone's the expression, creating new parameter and table aliases when they are declared.
    /// </summary>
    public class QueryDuplicator : SqlExpressionVisitor
    {
        private ImmutableDictionary<ParameterExpression, ParameterExpression> _parameterMap;
        private ImmutableDictionary<TableAlias, TableAlias> _aliasMap;

        public QueryDuplicator()
        {
            _parameterMap = ImmutableDictionary<ParameterExpression, ParameterExpression>.Empty;
            _aliasMap = ImmutableDictionary<TableAlias, TableAlias>.Empty;
        }

        public static Expression Duplicate(Expression expression)
        {
            var duplicator = new QueryDuplicator();
            if (expression is AliasedExpression aliased)
                duplicator.RedeclareAlias(aliased);
            return duplicator.Visit(expression);
        }

        /// <summary>
        /// Declares a new <see cref="TableAlias"/> to be used for this expression
        /// in the duplicate expression tree.
        /// </summary>
        private void RedeclareAlias(AliasedExpression aliased)
        {
            _aliasMap = _aliasMap.SetItem(aliased.Alias, new TableAlias());
        }

        /// <summary>
        /// Redeclares all aliases found in select expression's from clause.
        /// </summary>
        private void RedeclareFromAliases(Expression? source)
        {
            switch (source)
            {
                case AliasedExpression aliased:
                    this.RedeclareAlias(aliased);
                    break;
                case JoinExpression join:
                    this.RedeclareFromAliases(join.Left);
                    this.RedeclareFromAliases(join.Right);
                    break;
            }
        }

        /// <summary>
        /// Declares a new <see cref="ParameterExpression"/> to be used for this 
        /// parameter in the duplicated expression tree.
        /// </summary>
        private ParameterExpression RedeclareParameter(ParameterExpression p)
        {
            var newParameter = Expression.Parameter(p.Type, p.Name + "_dupe");
            _parameterMap = _parameterMap.SetItem(p, newParameter);
            return newParameter;
        }   

        private IReadOnlyList<ParameterExpression> RemapParameters(IReadOnlyList<ParameterExpression> parameters)
        {
            return parameters.Select(p => this.RedeclareParameter(p)).ToImmutableArray();
        }

        public override Expression Visit(Expression expression)
        {
            if (expression == null)
                return null!;

            var oldAliasMap = _aliasMap;
            var oldParameterMap = _parameterMap;

            var result = base.Visit(expression);

            _aliasMap = oldAliasMap;
            _parameterMap = oldParameterMap;

            return result;
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            if (_aliasMap.TryGetValue(column.Alias, out var newAlias))
            {
                return new ColumnExpression(column.Type, column.QueryType, newAlias, column.Name);
            }
            else
            {
                // always duplicate column expression regardless
                return new ColumnExpression(column.Type, column.QueryType, column.Alias, column.Name);
            }
        }

        protected override Expression VisitConstant(ConstantExpression original)
        {
            // always duplicate constants
            return Expression.Constant(original.Value, original.Type);
        }

        protected override Expression VisitParameter(ParameterExpression original)
        {
            if (_parameterMap.TryGetValue(original, out var mapped))
            {
                return mapped;
            }
            else
            {
                // leave old parameter reference, there is no way to duplicate it 
                // without making it a different parameter.
                return original;
            }
        }

        protected override Expression VisitLambda<T>(Expression<T> original)
        {
            var remapped = this.RemapParameters(original.Parameters);
            var body = this.Visit(original.Body);
            return original.Update(body, remapped);
        }

        protected internal override Expression VisitClientProjection(ClientProjectionExpression original)
        {
            this.RedeclareAlias(original.Select);

            var select = (SelectExpression)this.Visit(original.Select);
            var projector = this.Visit(original.Projector);
            var aggregator = (LambdaExpression?)this.Visit(original.Aggregator!);

            return original.Update(select, projector, aggregator);
        }

        protected internal override Expression VisitClientJoin(ClientJoinExpression original)
        {
            this.RedeclareAlias(original.Projection.Select);

            // do client projection manually here because we already mapped the select's alias.
            var select = (SelectExpression)this.Visit(original.Projection.Select);
            var projector = this.Visit(original.Projection.Projector);
            var aggregator = (LambdaExpression?)this.Visit(original.Projection.Aggregator!);
            var outerKey = original.OuterKey.Rewrite(this);
            var innerKey = original.InnerKey.Rewrite(this);

            return original.Update(
                original.Projection.Update(select, projector, aggregator),
                outerKey,
                innerKey);
        }

        protected internal override Expression VisitTable(TableExpression table)
        {
            if (_aliasMap.TryGetValue(table.Alias, out var newAlias))
            {
                return new TableExpression(newAlias, table.Entity, table.Name);
            }
            else
            {
                System.Diagnostics.Debug.Fail("Alias was not declared.");
                return new TableExpression(table.Alias, table.Entity, table.Name);
            }
        }

        protected internal override Expression VisitSelect(SelectExpression original)
        {
            this.RedeclareFromAliases(original.From);

            var modified = (SelectExpression)base.VisitSelect(original);

            if (_aliasMap.TryGetValue(original.Alias, out var newAlias))
                modified = modified.WithAlias(newAlias);

            return modified;
        }

        // additional nodes that may not refer to other expressions
        protected internal override Expression VisitAggregate(AggregateExpression original)
        {
            var argument = this.Visit(original.Argument!);
            return original.Update(original.Type, original.AggregateName, argument, original.IsDistinct);
        }

        protected internal override Expression VisitScalarFunctionCall(ScalarFunctionCallExpression original)
        {
            var arguments = original.Arguments.Rewrite(this);
            return original.Update(original.Type, original.IsPredicate, original.Name, arguments);
        }

        protected override Expression VisitMethodCall(MethodCallExpression original)
        {
            var instance = this.Visit(original.Object);
            var arguments = original.Arguments.Rewrite(this);
            return Expression.Call(instance, original.Method, arguments);
        }
    }
}