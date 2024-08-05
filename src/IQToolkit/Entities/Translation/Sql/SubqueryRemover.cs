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
    using Expressions.Sql;

    /// <summary>
    /// Removes specific nested select expressions by replacing them with their from clause.
    /// </summary>
    public class SubqueryRemover : SqlExpressionVisitor
    {
        private readonly HashSet<SelectExpression> _selectsToRemove;
        private readonly Dictionary<TableAlias, Dictionary<string, Expression>> _map;

        private SubqueryRemover(IEnumerable<SelectExpression> selectsToRemove)
        {
            _selectsToRemove = new HashSet<SelectExpression>(selectsToRemove);
            _map = _selectsToRemove.ToDictionary(d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));
        }

        public static SelectExpression Remove(SelectExpression outerSelect, params SelectExpression[] selectsToRemove)
        {
            return Remove(outerSelect, (IEnumerable<SelectExpression>)selectsToRemove);
        }

        public static SelectExpression Remove(SelectExpression outerSelect, IEnumerable<SelectExpression> selectsToRemove)
        {
            return (SelectExpression)new SubqueryRemover(selectsToRemove).Visit(outerSelect);
        }

        public static ClientProjectionExpression Remove(ClientProjectionExpression projection, params SelectExpression[] selectsToRemove)
        {
            return Remove(projection, (IEnumerable<SelectExpression>)selectsToRemove);
        }

        public static ClientProjectionExpression Remove(ClientProjectionExpression projection, IEnumerable<SelectExpression> selectsToRemove)
        {
            return (ClientProjectionExpression)new SubqueryRemover(selectsToRemove).Visit(projection);
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            if (_selectsToRemove.Contains(select) 
                && select.From != null)
            {
                // replace the remove select with its FROM clauses
                return this.Visit(select.From);
            }
            else
            {
                return base.VisitSelect(select);
            }
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            if (_map.TryGetValue(column.Alias, out var nameMap))
            {
                if (nameMap.TryGetValue(column.Name, out var expr))
                {
                    return this.Visit(expr);
                }
            }

            return column;
        }
    }
}