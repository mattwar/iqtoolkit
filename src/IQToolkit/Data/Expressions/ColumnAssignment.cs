// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    public sealed class ColumnAssignment
    {
        public ColumnExpression Column { get; }
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
    }
}
