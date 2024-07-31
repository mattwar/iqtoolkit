// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Expressions.Sql
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

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitDeleteCommand(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var table = (TableExpression)visitor.Visit(this.Table);
            var where = visitor.Visit(this.Where);
            return this.Update(table, where);
        }
    }
}
