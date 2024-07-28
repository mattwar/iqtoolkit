// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Represents an outer joined expression (for example the right-side of left-outer-join)
    /// that includes a Test expression to determine the outer-joined part exists.
    /// </summary>
    public sealed class OuterJoinedExpression : DbExpression
    {
        public Expression Test { get; }
        public Expression Expression { get; }

        public OuterJoinedExpression(Expression test, Expression expression)
            : base(expression.Type)
        {
            this.Test = test;
            this.Expression = expression;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.OuterJoined;

        public OuterJoinedExpression Update(
            Expression test, Expression expression)
        {
            if (test != this.Test 
                || expression != this.Expression)
            {
                return new OuterJoinedExpression(test, expression);
            }
            else
            {
                return this;
            }
        }
    }
}
