// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;

    /// <summary>
    /// Rewrite all references to one or more table aliases to a new single alias
    /// </summary>
    public class TableAliasRemapper : SqlExpressionVisitor
    {
        private readonly HashSet<TableAlias> _oldAliases;
        private readonly TableAlias _newAlias;

        public TableAliasRemapper(IEnumerable<TableAlias> oldAliases, TableAlias newAlias)
        {
            _oldAliases = new HashSet<TableAlias>(oldAliases);
            _newAlias = newAlias;
        }

        public static Expression Map(Expression expression, TableAlias newAlias, IEnumerable<TableAlias> oldAliases)
        {
            return new TableAliasRemapper(oldAliases, newAlias).Visit(expression);
        }

        public static Expression Map(Expression expression, TableAlias newAlias, params TableAlias[] oldAliases)
        {
            return Map(expression, newAlias, (IEnumerable<TableAlias>)oldAliases);
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            if (_oldAliases.Contains(column.Alias))
            {
                return new ColumnExpression(column.Type, column.QueryType, _newAlias, column.Name);
            }

            return column;
        }
    }
}
