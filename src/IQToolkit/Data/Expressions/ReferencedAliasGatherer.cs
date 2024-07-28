// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Returns the set of all table aliases referenced in the source.
    /// </summary>
    public class ReferencedAliasGatherer : DbExpressionRewriter
    {
        HashSet<TableAlias> aliases;

        private ReferencedAliasGatherer()
        {
            this.aliases = new HashSet<TableAlias>();
        }

        public static HashSet<TableAlias> Gather(Expression source)
        {
            var gatherer = new ReferencedAliasGatherer();
            gatherer.Rewrite(source);
            return gatherer.aliases;
        }

        protected override Expression RewriteColumn(ColumnExpression column)
        {
            this.aliases.Add(column.Alias);
            return column;
        }
    }
}
