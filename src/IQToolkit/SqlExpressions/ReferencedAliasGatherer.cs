// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// Returns the set of all table aliases referenced in the source.
    /// </summary>
    public class ReferencedAliasGatherer : DbExpressionVisitor
    {
        private readonly HashSet<TableAlias> _aliases;

        private ReferencedAliasGatherer()
        {
            _aliases = new HashSet<TableAlias>();
        }

        public static HashSet<TableAlias> Gather(Expression source)
        {
            var gatherer = new ReferencedAliasGatherer();
            gatherer.Visit(source);
            return gatherer._aliases;
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            _aliases.Add(column.Alias);
            return column;
        }
    }
}
