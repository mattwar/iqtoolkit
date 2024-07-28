// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    public sealed class DeleteCommand : CommandExpression
    {
        public TableExpression Table { get; }
        public Expression? Where { get; }

        public DeleteCommand(TableExpression table, Expression? where)
            : base(typeof(int))
        {
            this.Table = table;
            this.Where = where;
        }

        public override DbExpressionType DbNodeType => 
            DbExpressionType.Delete;

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
