// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    /// <summary>
    /// A SQL IN expression for a list of values.
    /// </summary>
    public sealed class InValuesExpression : DbExpression
    {
        /// <summary>
        /// The expression on the left of the in operator
        /// </summary>
        public Expression Expression { get; }

        /// <summary>
        /// The values (if a list of explicit values is used)
        /// </summary>
        public IReadOnlyList<Expression> Values { get; }

        public InValuesExpression(Expression expression, IEnumerable<Expression> values)
            : base(typeof(bool))
        {
            this.Expression = expression;
            this.Values = values.ToReadOnly();
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.InValues;

        public override bool IsPredicate => true;

        public InValuesExpression Update(
            Expression expression,
            IEnumerable<Expression> values)
        {
            if (expression != this.Expression
                || values != this.Values)
            {
                return new InValuesExpression(expression, values);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitInValues(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var expr = visitor.Visit(this.Expression);
            var values = this.Values.Rewrite(visitor);
            return this.Update(expr, values);
        }
    }
}
