// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Represents a SQL join operation.
    /// </summary>
    public sealed class JoinExpression : DbExpression
    {
        public JoinType JoinType { get; }
        public Expression Left { get; }
        public Expression Right { get; }
        public new Expression? Condition { get; }

        public JoinExpression(JoinType joinType, Expression left, Expression right, Expression? condition)
            : base(typeof(void))
        {
            this.JoinType = joinType;
            this.Left = left;
            this.Right = right;
            this.Condition = condition;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Join;

        public JoinExpression Update(
            JoinType joinType,
            Expression left,
            Expression right,
            Expression? condition)
        {
            if (joinType != this.JoinType 
                || left != this.Left 
                || right != this.Right 
                || condition != this.Condition)
            {
                return new JoinExpression(joinType, left, right, condition);
            }
            else
            {
                return this;
            }
        }
    }
}
