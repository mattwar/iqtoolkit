// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Expressions.Sql
{
    using Entities.Mapping;

    /// <summary>
    /// Associates the expression with a mapped entity.
    /// </summary>
    public sealed class EntityExpression : SqlExpression
    {
        /// <summary>
        /// The entity.
        /// </summary>
        public MappedEntity Entity { get; }

        /// <summary>
        /// The expression that constructs the entity instance.
        /// </summary>
        public Expression Expression { get; }

        public EntityExpression(MappedEntity entity, Expression expression)
            : base(expression.Type)
        {
            this.Entity = entity;
            this.Expression = expression;
        }

        public EntityExpression Update(MappedEntity entity, Expression expression)
        {
            if (entity != this.Entity
                || expression != this.Expression)
            {
                return new EntityExpression(entity, expression);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitEntity(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var exp = visitor.Visit(this.Expression);
            return this.Update(this.Entity, exp);
        }
    }
}
