// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    public sealed class UpdateCommand : CommandExpression
    {
        public TableExpression Table { get; }
        public Expression Where { get; }
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
            DbExpressionType.Update;

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
