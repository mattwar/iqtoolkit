// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// A SQL scalar subquery.
    /// </summary>
    public sealed class ScalarSubqueryExpression : SubqueryExpression
    {
        public ScalarSubqueryExpression(Type type, SelectExpression select)
            : base(type, select)
        {
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.ScalarSubquery;

        public ScalarSubqueryExpression Update(Type type, SelectExpression select)
        {
            if (type != this.Type
                || select != this.Select)
            {
                return new ScalarSubqueryExpression(type, select);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitScalarSubquery(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var select = (SelectExpression)visitor.Visit(this.Select);
            return this.Update(this.Type, select);
        }
    }
}
