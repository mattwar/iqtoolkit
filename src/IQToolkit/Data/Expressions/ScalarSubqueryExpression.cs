// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A scalar subquery.
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
    }
}
