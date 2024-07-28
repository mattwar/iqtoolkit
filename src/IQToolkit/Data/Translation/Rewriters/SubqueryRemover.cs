// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    /// <summary>
    /// Removes specific nested select expressions by replacing them with their from clause.
    /// </summary>
    public class SubqueryRemover : DbExpressionRewriter
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
            return (SelectExpression)new SubqueryRemover(selectsToRemove).Rewrite(outerSelect);
        }

        public static ClientProjectionExpression Remove(ClientProjectionExpression projection, params SelectExpression[] selectsToRemove)
        {
            return Remove(projection, (IEnumerable<SelectExpression>)selectsToRemove);
        }

        public static ClientProjectionExpression Remove(ClientProjectionExpression projection, IEnumerable<SelectExpression> selectsToRemove)
        {
            return (ClientProjectionExpression)new SubqueryRemover(selectsToRemove).Rewrite(projection);
        }

        protected override Expression RewriteSelect(SelectExpression select)
        {
            if (_selectsToRemove.Contains(select) 
                && select.From != null)
            {
                // replace the remove select with its FROM clauses
                return this.Rewrite(select.From);
            }
            else
            {
                return base.RewriteSelect(select);
            }
        }

        protected override Expression RewriteColumn(ColumnExpression column)
        {
            if (_map.TryGetValue(column.Alias, out var nameMap))
            {
                if (nameMap.TryGetValue(column.Name, out var expr))
                {
                    return this.Rewrite(expr);
                }
                else
                {

                }

                //throw new Exception("Reference to undefined column");
            }
            else
            {

            }

            return column;
        }
    }
}