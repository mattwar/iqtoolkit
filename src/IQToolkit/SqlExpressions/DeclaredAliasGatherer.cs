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
    ///  returns the set of all aliases produced by a query source
    /// </summary>
    public class DeclaredAliasGatherer : DbExpressionVisitor
    {
        private readonly HashSet<TableAlias> _aliases;

        private DeclaredAliasGatherer()
        {
            _aliases = new HashSet<TableAlias>();
        }

        public static HashSet<TableAlias> Gather(Expression? source)
        {
            var gatherer = new DeclaredAliasGatherer();
            gatherer.Visit(source);
            return gatherer._aliases;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            _aliases.Add(select.Alias);
            return select;
        }

        protected internal override Expression VisitTable(TableExpression table)
        {
            _aliases.Add(table.Alias);
            return table;
        }
    }
}
