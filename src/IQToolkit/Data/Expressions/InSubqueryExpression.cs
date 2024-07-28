﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A SQL IN subquery.
    /// </summary>
    public sealed class InSubqueryExpression : SubqueryExpression
    {
        /// <summary>
        /// The expression on the left of the in operator
        /// </summary>
        public Expression Expression { get; }

        public InSubqueryExpression(Expression expression, SelectExpression select)
            : base(typeof(bool), select)
        {
            this.Expression = expression;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.InSubquery;

        public override bool IsPredicate => true;

        public InSubqueryExpression Update(
            Expression expression,
            SelectExpression select)
        {
            if (expression != this.Expression 
                || select != this.Select)
            {
                return new InSubqueryExpression(expression, select);
            }
            else
            {
                return this;
            }
        }
    }
}
