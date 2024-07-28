// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    using Mapping;

    public sealed class EntityExpression : DbExpression
    {
        public MappingEntity Entity { get; }
        public Expression Expression { get; }

        public EntityExpression(MappingEntity entity, Expression expression)
            : base(expression.Type)
        {
            this.Entity = entity;
            this.Expression = expression;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Entity;

        public EntityExpression Update(MappingEntity entity, Expression expression)
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
    }
}
