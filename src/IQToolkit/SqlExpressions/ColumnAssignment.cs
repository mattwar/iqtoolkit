// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// The assignment of a column in an <see cref="InsertCommand"/> or <see cref="UpdateCommand"/>.
    /// </summary>
    public sealed class ColumnAssignment
    {
        /// <summary>
        /// The column to assigned.
        /// </summary>
        public ColumnExpression Column { get; }

        /// <summary>
        /// The value to assign the column.
        /// </summary>
        public Expression Expression { get; }

        public ColumnAssignment(ColumnExpression column, Expression expression)
        {
            this.Column = column;
            this.Expression = expression;
        }

        public ColumnAssignment Update(ColumnExpression column, Expression expression)
        {
            if (column != this.Column 
                || expression != this.Expression)
            {
                return new ColumnAssignment(column, expression);
            }
            else
            {
                return this;
            }
        }

        internal ColumnAssignment Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitColumnAssignment(this);
            return this.VisitChildren(visitor);
        }

        internal ColumnAssignment VisitChildren(ExpressionVisitor visitor)
        {
            var column = (ColumnExpression)visitor.Visit(this.Column);
            var expression = visitor.Visit(this.Expression);
            return this.Update(column, expression);
        }
    }
}
