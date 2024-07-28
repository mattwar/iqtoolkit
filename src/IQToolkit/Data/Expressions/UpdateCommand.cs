// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    /// <summary>
    /// A SQL UPDATE command.
    /// </summary>
    public sealed class UpdateCommand : CommandExpression
    {
        /// <summary>
        /// The table to be updated.
        /// </summary>
        public TableExpression Table { get; }

        /// <summary>
        /// The predicate that determines which rows are updated.
        /// </summary>
        public Expression Where { get; }

        /// <summary>
        /// The assignments that update the columns of each row.
        /// </summary>
        public IReadOnlyList<ColumnAssignment> Assignments { get; }

        public UpdateCommand(
            TableExpression table, 
            Expression where, 
            IEnumerable<ColumnAssignment> assignments)
            : base(typeof(int))
        {
            this.Table = table;
            this.Where = where;
            this.Assignments = assignments.ToReadOnly();
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.UpdateCommand;

        public UpdateCommand Update(
            TableExpression table, 
            Expression where, 
            IEnumerable<ColumnAssignment> assignments)
        {
            if (table != this.Table 
                || where != this.Where 
                || assignments != this.Assignments)
            {
                return new UpdateCommand(table, where, assignments);
            }
            else
            {
                return this;
            }
        }
    }
}
