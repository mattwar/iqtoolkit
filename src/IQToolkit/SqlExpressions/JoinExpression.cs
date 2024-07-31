// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// A SQL JOIN operation.
    /// </summary>
    public sealed class JoinExpression : DbExpression
    {
        /// <summary>
        /// The type of the join.
        /// </summary>
        public JoinType JoinType { get; }

        /// <summary>
        /// The left operand of the join.
        /// </summary>
        public Expression Left { get; }

        /// <summary>
        /// The right operand of the join.
        /// </summary>
        public Expression Right { get; }

        /// <summary>
        /// An optional join condition.
        /// </summary>
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

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitJoin(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var left = visitor.Visit(this.Left);
            var right = visitor.Visit(this.Right);
            var condition = visitor.Visit(this.Condition);
            return this.Update(this.JoinType, left, right, condition);
        }
    }
}
