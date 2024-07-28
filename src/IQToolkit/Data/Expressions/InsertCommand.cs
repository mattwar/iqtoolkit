// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    /// <summary>
    /// A SQL INSERT command.
    /// </summary>
    public sealed class InsertCommand : CommandExpression
    {
        /// <summary>
        /// The table to insert to.
        /// </summary>
        public TableExpression Table { get; }

        /// <summary>
        /// The column assignments defining the row to insert.
        /// </summary>
        public IReadOnlyList<ColumnAssignment> Assignments { get; }

        public InsertCommand(TableExpression table, IEnumerable<ColumnAssignment> assignments)
            : base(typeof(int))
        {
            this.Table = table;
            this.Assignments = assignments.ToReadOnly();
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.InsertCommand;

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
