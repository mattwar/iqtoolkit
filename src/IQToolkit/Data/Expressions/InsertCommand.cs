// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    public sealed class InsertCommand : CommandExpression
    {
        public TableExpression Table { get; }
        public IReadOnlyList<ColumnAssignment> Assignments { get; }

        public InsertCommand(TableExpression table, IEnumerable<ColumnAssignment> assignments)
            : base(typeof(int))
        {
            this.Table = table;
            this.Assignments = assignments.ToReadOnly();
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Insert;

        public InsertCommand Update(TableExpression table, IEnumerable<ColumnAssignment> assignments)
        {
            if (table != this.Table 
                || assignments != this.Assignments)
            {
                return new InsertCommand(table, assignments);
            }
            else
            {
                return this;
            }
        }
    }
}
