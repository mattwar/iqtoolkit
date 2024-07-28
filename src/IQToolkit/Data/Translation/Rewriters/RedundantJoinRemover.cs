// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;
    using Utils;

    /// <summary>
    /// Removes joins expressions that are identical to joins that already exist
    /// </summary>
    public class RedundantJoinRemover : DbExpressionRewriter
    {
        private readonly Dictionary<TableAlias, TableAlias> _map;

        public RedundantJoinRemover()
        {
            _map = new Dictionary<TableAlias, TableAlias>();
        }

        protected override Expression RewriteJoin(JoinExpression join)
        {
            var result = base.RewriteJoin(join);
            if (result is JoinExpression rjoin)
            {
                if (rjoin.Right is AliasedExpression right
                    && this.FindSimilarRight(rjoin.Left as JoinExpression, join) is AliasedExpression similarRight)
                {
                    this._map.Add(right.Alias, similarRight.Alias);
                    return join.Left;
                }
            }
            return result;
        }

        private Expression? FindSimilarRight(JoinExpression? join, JoinExpression compareTo)
        {
            if (join == null)
                return null;

            if (join.JoinType == compareTo.JoinType)
            {
                if (join.Right.NodeType == compareTo.Right.NodeType
                    && DbExpressionComparer.Default.Equals(join.Right, compareTo.Right))
                {
                    if (join.Condition == compareTo.Condition)
                        return join.Right;
                    var scope = ImmutableDictionary<TableAlias, TableAlias>.Empty
                        .Add(((AliasedExpression)join.Right).Alias, ((AliasedExpression)compareTo.Right).Alias);
                    if (DbExpressionComparer.Default.Equals(join.Condition, compareTo.Condition, scope))
                        return join.Right;
                }
            }

            var result = FindSimilarRight(join.Left as JoinExpression, compareTo);
            if (result == null)
            {
                result = FindSimilarRight(join.Right as JoinExpression, compareTo);
            }

            return result;
        }

        protected override Expression RewriteColumn(ColumnExpression column)
        {
            TableAlias mapped;
            if (this._map.TryGetValue(column.Alias, out mapped))
            {
                return new ColumnExpression(column.Type, column.QueryType, mapped, column.Name);
            }
            return column;
        }
    }
}
