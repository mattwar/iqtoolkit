// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Expressions
{
    using System.Collections.Immutable;

    /// <summary>
    /// Determines if two expressions are equivalent. 
    /// Supports <see cref="DbExpression"/> nodes.
    /// </summary>
    public class DbExpressionComparer : ExpressionComparer
    {
        protected DbExpressionComparer(
            Func<object?, object?, bool>? fnCompare)
            : base(fnCompare)
        {
        }

        public static readonly new DbExpressionComparer Default =
            new DbExpressionComparer(null);

        public override bool Equals(Expression? x, Expression? y)
        {
            return this.Compare(x, y, DbScope.Default);
        }

        public override bool Equals(
            Expression? x,
            Expression? y,
            ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap)
        {
            return this.Compare(x, y, DbScope.Default.WithParameterMap(parameterMap));
        }

        public virtual bool Equals(
            Expression? x, 
            Expression? y, 
            ImmutableDictionary<TableAlias, TableAlias> aliasMap)
        {
            return this.Compare(x, y, DbScope.Default.WithAliasMap(aliasMap));
        }

        public bool Equals<TExpression>(
            IReadOnlyList<TExpression> x, 
            IReadOnlyList<TExpression> y)
            where TExpression : Expression
        {
            return this.CompareExpressionList(x, y, DbScope.Default);
        }

        public bool Equals<TExpression>(
            IReadOnlyList<TExpression> x,
            IReadOnlyList<TExpression> y,
            ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap)
            where TExpression : Expression
        {
            return this.CompareExpressionList(x, y, DbScope.Default.WithParameterMap(parameterMap));
        }

        public virtual bool Equals<TExpression>(
            IReadOnlyList<TExpression> x,
            IReadOnlyList<TExpression> y,
            ImmutableDictionary<TableAlias, TableAlias> aliasMap)
            where TExpression : Expression
        {
            return this.CompareExpressionList(x, y, DbScope.Default.WithAliasMap(aliasMap));
        }

        protected class DbScope : Scope
        {
            public ImmutableDictionary<TableAlias, TableAlias> AliasMap { get; }

            public static readonly new DbScope Default = new DbScope(
                ImmutableDictionary<ParameterExpression, ParameterExpression>.Empty,
                ImmutableDictionary<TableAlias, TableAlias>.Empty);

            public DbScope(
                ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap,
                ImmutableDictionary<TableAlias, TableAlias> aliasMap)
                : base(parameterMap)
            {
                this.AliasMap = aliasMap;
            }

            public DbScope WithAliasMap(ImmutableDictionary<TableAlias, TableAlias> aliasMap)
            {
                return Create(this.ParameterMap, aliasMap);
            }

            protected override Scope Create(
                ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap)
            {
                return new DbScope(parameterMap, this.AliasMap);
            }

            protected virtual DbScope Create(
                ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap,
                ImmutableDictionary<TableAlias, TableAlias> aliasMap)
            {
                return new DbScope(parameterMap, aliasMap);
            }
        }

        private DbScope MapAliases(Expression? a, Expression? b, DbScope scope)
        {
            var aliasMap = scope.AliasMap;

            var prodA = DeclaredAliasGatherer.Gather(a).ToArray();
            var prodB = DeclaredAliasGatherer.Gather(b).ToArray();

            for (int i = 0, n = prodA.Length; i < n; i++)
            {
                aliasMap = aliasMap.Add(prodA[i], prodB[i]);
            }

            return scope.WithAliasMap(aliasMap);
        }

        protected override bool Compare(
            Expression? a, 
            Expression? b,
            Scope scope)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.NodeType != b.NodeType)
                return false;
            if (a.Type != b.Type)
                return false;

            var s = (DbScope)scope;

            switch (a)
            {
                case AggregateExpression aggregateA:
                    return this.CompareAggregate(aggregateA, (AggregateExpression)b, s);
                case BatchExpression batchA:
                    return this.CompareBatch(batchA, (BatchExpression)b, s);
                case BlockCommand blkcomA:
                    return this.CompareBlock(blkcomA, (BlockCommand)b, s);
                case BetweenExpression betweenA:
                    return this.CompareBetween(betweenA, (BetweenExpression)b, s);
                case ClientParameterExpression cparamA:
                    return this.CompareClientParameter(cparamA, (ClientParameterExpression)b, s);
                case ClientProjectionExpression cprojA:
                    return this.CompareClientProjection(cprojA, (ClientProjectionExpression)b, s);
                case ClientJoinExpression cjoinA:
                    return this.CompareClientJoin(cjoinA, (ClientJoinExpression)b, s);
                case ColumnExpression columnA:
                    return this.CompareColumn(columnA, (ColumnExpression)b, s);
                case DeleteCommand delcomA:
                    return this.CompareDelete(delcomA, (DeleteCommand)b, s);
                case EntityExpression entityA:
                    return this.CompareEntity(entityA, (EntityExpression)b, s);
                case ExistsSubqueryExpression existsA:
                    return this.CompareExistsSubquery(existsA, (ExistsSubqueryExpression)b, s);
                case DbFunctionCallExpression functionA:
                    return this.CompareFunction(functionA, (DbFunctionCallExpression)b, s);
                case IfCommand ifcomA:
                    return this.CompareIfCommand(ifcomA, (IfCommand)b, s);
                case InsertCommand inscomA:
                    return this.CompareInsertCommand(inscomA, (InsertCommand)b, s);
                case IsNullExpression isnullA:
                    return this.CompareIsNull(isnullA, (IsNullExpression)b, s);
                case InValuesExpression invalsA:
                    return this.CompareInValues(invalsA, (InValuesExpression)b, s);
                case InSubqueryExpression insubA:
                    return this.CompareInSubquery(insubA, (InSubqueryExpression)b, s);
                case JoinExpression joinA:
                    return this.CompareJoin(joinA, (JoinExpression)b, s);
                case RowNumberExpression rownumA:
                    return this.CompareRowNumber(rownumA, (RowNumberExpression)b, s);
                case ScalarSubqueryExpression scalarA:
                    return this.CompareScalarSubquery(scalarA, (ScalarSubqueryExpression)b, s);
                case SelectExpression selectA:
                    return this.CompareSelect(selectA, (SelectExpression)b, s);
                case TableExpression tableA:
                    return this.CompareTable(tableA, (TableExpression)b);
                case TaggedExpression taggedA:
                    return this.CompareTagged(taggedA, (TaggedExpression)b, s);
                case UpdateCommand updcomA:
                    return this.CompareUpdate(updcomA, (UpdateCommand)b, s);
                default:
                    return base.Compare(a, b, s);
            }
        }

        protected virtual bool CompareAlias(
            TableAlias a,
            TableAlias b,
            DbScope s)
        {

            if (s.AliasMap.TryGetValue(a, out var mapped))
                return mapped == b;

            return a == b;
        }

        protected virtual bool CompareAggregate(
            AggregateExpression a,
            AggregateExpression b,
            DbScope s)
        {
            return a.AggregateName == b.AggregateName
                && this.Compare(a.Argument, b.Argument, s);
        }

        protected virtual bool CompareBatch(
            BatchExpression x,
            BatchExpression y,
            DbScope s)
        {
            return this.Compare(x.Input, y.Input, s)
                && this.Compare(x.Operation, y.Operation, s)
                && this.Compare(x.BatchSize, y.BatchSize, s)
                && this.Compare(x.Stream, y.Stream, s);
        }

        protected virtual bool CompareBetween(
            BetweenExpression a,
            BetweenExpression b,
            DbScope s
            )
        {
            return this.Compare(a.Expression, b.Expression, s)
                && this.Compare(a.Lower, b.Lower, s)
                && this.Compare(a.Upper, b.Upper, s);
        }

        protected virtual bool CompareBlock(
            BlockCommand x,
            BlockCommand y,
            DbScope s)
        {
            return this.CompareExpressionList(x.Commands, y.Commands, s);
        }

        protected virtual bool CompareClientJoin(
            ClientJoinExpression a,
            ClientJoinExpression b,
            DbScope s)
        {
            return this.Compare(a.Projection, b.Projection, s)
                && this.CompareExpressionList(a.OuterKey, b.OuterKey, s)
                && this.CompareExpressionList(a.InnerKey, b.InnerKey, s);
        }

        protected virtual bool CompareClientParameter(
            ClientParameterExpression a,
            ClientParameterExpression b,
            DbScope s)
        {
            return a.Name == b.Name
                && this.Compare(a.Value, b.Value, s);
        }

        protected virtual bool CompareClientProjection(
            ClientProjectionExpression a,
            ClientProjectionExpression b,
            DbScope s)
        {
            if (!this.Compare(a.Select, b.Select, s))
                return false;

            s = s.WithAliasMap(s.AliasMap.Add(a.Select.Alias, b.Select.Alias));

            return this.Compare(a.Projector, b.Projector, s)
                && this.Compare(a.Aggregator, b.Aggregator, s)
                && a.IsSingleton == b.IsSingleton;
        }


        protected virtual bool CompareColumn(
            ColumnExpression a,
            ColumnExpression b,
            DbScope s)
        {
            return this.CompareAlias(a.Alias, b.Alias, s)
                && a.Name == b.Name;
        }

        protected virtual bool CompareColumnAssignments(
            IReadOnlyList<ColumnAssignment> x,
            IReadOnlyList<ColumnAssignment> y,
            DbScope s)
        {
            if (x == y)
                return true;

            if (x.Count != y.Count)
                return false;

            for (int i = 0, n = x.Count; i < n; i++)
            {
                if (!this.Compare(x[i].Column, y[i].Column, s)
                    || !this.Compare(x[i].Expression, y[i].Expression, s))
                    return false;
            }

            return true;
        }

        protected virtual bool CompareColumnDeclaration(
            ColumnDeclaration a,
            ColumnDeclaration b,
            DbScope s)
        {
            return a.Name == b.Name && this.Compare(a.Expression, b.Expression, s);
        }

        protected virtual bool CompareColumnDeclarations(
            IReadOnlyList<ColumnDeclaration>? a,
            IReadOnlyList<ColumnDeclaration>? b,
            DbScope s)
        {
            if (a == b)
                return true;

            if (a == null || b == null)
                return false;

            if (a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!this.CompareColumnDeclaration(a[i], b[i], s))
                    return false;
            }

            return true;
        }

        protected virtual bool CompareDelete(
            DeleteCommand x,
            DeleteCommand y,
            DbScope s)
        {
            return this.Compare(x.Table, y.Table, s)
                && this.Compare(x.Where, y.Where, s);
        }

        protected virtual bool CompareEntity(
            EntityExpression x,
            EntityExpression y,
            DbScope s)
        {
            return x.Entity == y.Entity
                && this.Compare(x.Expression, y.Expression, s);
        }

        protected virtual bool CompareExistsSubquery(
            ExistsSubqueryExpression a,
            ExistsSubqueryExpression b,
            DbScope s)
        {
            return this.Compare(a.Select, b.Select, s);
        }

        protected virtual bool CompareFunction(
            DbFunctionCallExpression x,
            DbFunctionCallExpression y,
            DbScope s)
        {
            return x.Name == y.Name
                && this.CompareExpressionList(x.Arguments, y.Arguments, s);
        }

        protected virtual bool CompareIfCommand(
            IfCommand x,
            IfCommand y,
            DbScope s)
        {
            return this.Compare(x.Test, y.Test, s)
                && this.Compare(x.IfTrue, y.IfTrue, s)
                && this.Compare(x.IfFalse, y.IfFalse, s);
        }

        protected virtual bool CompareInValues(
            InValuesExpression a,
            InValuesExpression b,
            DbScope s)
        {
            return this.Compare(a.Expression, b.Expression, s)
                && this.CompareExpressionList(a.Values, b.Values, s);
        }

        protected virtual bool CompareInsertCommand(
            InsertCommand x,
            InsertCommand y,
            DbScope s)
        {
            return this.Compare(x.Table, y.Table, s)
                && this.CompareColumnAssignments(x.Assignments, y.Assignments, s);
        }

        protected virtual bool CompareIsNull(
            IsNullExpression a,
            IsNullExpression b,
            DbScope s)
        {
            return this.Compare(a.Expression, b.Expression, s);
        }

        protected virtual bool CompareInSubquery(
            InSubqueryExpression a,
            InSubqueryExpression b,
            DbScope s)
        {
            return this.Compare(a.Expression, b.Expression, s)
                && this.Compare(a.Select, b.Select, s);
        }

        protected virtual bool CompareJoin(
            JoinExpression a,
            JoinExpression b,
            DbScope s)
        {
            if (a.JoinType != b.JoinType
                || !this.Compare(a.Left, b.Left, s))
                return false;

            if (a.JoinType == JoinType.CrossApply
                || a.JoinType == JoinType.OuterApply)
            {
                s = this.MapAliases(a.Left, b.Left, s);
                return this.Compare(a.Right, b.Right, s)
                    && this.Compare(a.Condition, b.Condition, s);
            }
            else
            {
                return this.Compare(a.Right, b.Right, s)
                    && this.Compare(a.Condition, b.Condition, s);
            }
        }

        protected virtual bool CompareOrderList(
            IReadOnlyList<OrderExpression>? a, 
            IReadOnlyList<OrderExpression>? b,
            DbScope s)
        {
            if (a == b)
                return true;
            
            if (a == null || b == null)
                return false;
            
            if (a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (a[i].OrderType != b[i].OrderType 
                    || !this.Compare(a[i].Expression, b[i].Expression, s))
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual bool CompareRowNumber(
            RowNumberExpression a, 
            RowNumberExpression b,
            DbScope s)
        {
            return this.CompareOrderList(a.OrderBy, b.OrderBy, s);
        }

        protected virtual bool CompareScalarSubquery(
            ScalarSubqueryExpression a, 
            ScalarSubqueryExpression b,
            DbScope s)
        {
            return this.Compare(a.Select, b.Select, s);
        }

        protected virtual bool CompareSelect(
            SelectExpression a,
            SelectExpression b,
            DbScope s)
        {
            if (!this.Compare(a.From, b.From, s))
                return false;

            s = this.MapAliases(a.From, b.From, s);

            return this.Compare(a.Where, b.Where, s)
                && this.CompareOrderList(a.OrderBy, b.OrderBy, s)
                && this.CompareExpressionList(a.GroupBy, b.GroupBy, s)
                && this.Compare(a.Skip, b.Skip, s)
                && this.Compare(a.Take, b.Take, s)
                && a.IsDistinct == b.IsDistinct
                && a.IsReverse == b.IsReverse
                && this.CompareColumnDeclarations(a.Columns, b.Columns, s);
        }

        protected virtual bool CompareTable(
            TableExpression a,
            TableExpression b)
        {
            return a.Name == b.Name;
        }

        protected virtual bool CompareTagged(
            TaggedExpression a,
            TaggedExpression b,
            DbScope s)
        {
            return this.Compare(a.Expression, b.Expression, s);
        }

        protected virtual bool CompareUpdate(
            UpdateCommand x, 
            UpdateCommand y,
            DbScope s)
        {
            return this.Compare(x.Table, y.Table, s) 
                && this.Compare(x.Where, y.Where, s) 
                && this.CompareColumnAssignments(x.Assignments, y.Assignments, s);
        }
    }
}