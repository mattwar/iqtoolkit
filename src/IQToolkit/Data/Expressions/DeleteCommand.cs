// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A SQL DELETE command.
    /// </summary>
    public sealed class DeleteCommand : CommandExpression
    {
        /// <summary>
        /// The table to delete from.
        /// </summary>
        public TableExpression Table { get; }

        /// <summary>
        /// The predicate that determines which rows to delete.
        /// </summary>
        public Expression? Where { get; }

        public DeleteCommand(TableExpression table, Expression? where)
            : base(typeof(int))
        {
            this.Table = table;
            this.Where = where;
        }

        public override DbExpressionType DbNodeType => 
            DbExpressionType.DeleteCommand;

        public DeleteCommand Update(TableExpression table, Expression? where)
        {
            if (table != this.Table 
                || where != this.Where)
            {
                return new DeleteCommand(table, where);
            }
            else
            {
                return this;
            }
        }
    }
}
