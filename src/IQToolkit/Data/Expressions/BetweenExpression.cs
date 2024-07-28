// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// The between expression: expr between (lower, upper)
    /// </summary>
    public sealed class BetweenExpression : DbExpression
    {
        public Expression Expression { get; }
        public Expression Lower { get; }
        public Expression Upper { get; }

        public BetweenExpression(
            Expression expression, 
            Expression lower, 
            Expression upper)
            : base(expression.Type)
        {
            this.Expression = expression;
            this.Lower = lower;
            this.Upper = upper;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Between;

        public override bool IsPredicate => true;

        public BetweenExpression Update(
            Expression expression, 
            Expression lower, 
            Expression upper)
        {
            if (expression != this.Expression 
                || lower != this.Lower 
                || upper != this.Upper)
            {
                return new BetweenExpression(expression, lower, upper);
            }
            else
            {
                return this;
            }
        }
    }
}
