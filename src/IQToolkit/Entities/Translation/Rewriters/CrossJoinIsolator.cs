// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using SqlExpressions;

    /// <summary>
    /// Isolates cross joins from other types of joins by pushing down into nested subqueries.
    /// </summary>
    public class CrossJoinIsolator : SqlExpressionVisitor
    {
        private readonly Dictionary<ColumnExpression, ColumnExpression> _map;
        private ILookup<TableAlias, ColumnExpression>? _currentColumns;
        private JoinType? _lastJoin;

        public CrossJoinIsolator()
        {
            _map = new Dictionary<ColumnExpression, ColumnExpression>();
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            var saveColumns = _currentColumns;
            _currentColumns = ReferencedColumnGatherer.Gather(select).ToLookup(c => c.Alias);               
            var saveLastJoin = _lastJoin;
            _lastJoin = null;

            var result = base.VisitSelect(select);
                
            _currentColumns = saveColumns;
            _lastJoin = saveLastJoin;

            return result;
        }

        protected internal override Expression VisitJoin(JoinExpression join)
        {
            var saveLastJoin = _lastJoin;
            _lastJoin = join.JoinType;
            join = (JoinExpression)base.VisitJoin(join);
            _lastJoin = saveLastJoin;

            if (_lastJoin != null 
                && (join.JoinType == JoinType.CrossJoin) != (_lastJoin == JoinType.CrossJoin))
            {
                var result = this.MakeSubquery(join);
                return result;
            }

            return join;
        }

        private Expression MakeSubquery(JoinExpression expression)
        {
            var newAlias = new TableAlias();
            var aliases = DeclaredAliasGatherer.Gather(expression);

            var decls = new List<ColumnDeclaration>();
            if (_currentColumns != null)
            {
                foreach (var ta in aliases)
                {
                    foreach (var col in _currentColumns[ta])
                    {
                        var name = decls.GetAvailableColumnName(col.Name);
                        var decl = new ColumnDeclaration(name, col, col.QueryType);
                        decls.Add(decl);
                        var newCol = new ColumnExpression(col.Type, col.QueryType, newAlias, col.Name);
                        _map.Add(col, newCol);
                    }
                }
            }

            return new SelectExpression(newAlias, decls, expression, null);
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            return _map.TryGetValue(column, out var mapped)
                ? mapped
                : column;
        }
    }
}