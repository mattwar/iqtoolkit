// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// An expression that can be identified in the tree by its <see cref="Id"/>,
    /// which stays the same regardless of rewrites.
    /// </summary>
    public sealed class TaggedExpression : DbExpression
    {
        public int Id { get; }
        public Expression Expression { get; }

        public TaggedExpression(int id, Expression expression)
            : base(expression.Type)
        {
            this.Id = id;
            this.Expression = expression;
        }

        private static int _nextId;

        public TaggedExpression(Expression expression)
            : this(++_nextId, expression)
        {
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Tagged;

        public TaggedExpression Update(
            Expression expression)
        {
            if (expression != this.Expression)
            {
                return new TaggedExpression(this.Id, expression);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitTagged(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var expression = visitor.Visit(this.Expression);
            return this.Update(expression);
        }
    }
}
