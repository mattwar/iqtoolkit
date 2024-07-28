// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// An exists subquery (scalar boolean).
    /// </summary>
    public sealed class ExistsSubqueryExpression : SubqueryExpression
    {
        public new SelectExpression Select => base.Select!;

        public ExistsSubqueryExpression(SelectExpression select)
            : base(typeof(bool), select)
        {
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.ExistsSubquery;

        public override bool IsPredicate => true;

        public ExistsSubqueryExpression Update(
            SelectExpression select)
        {
            if (select != this.Select)
            {
                return new ExistsSubqueryExpression(select);
            }
            else
            {
                return this;
            }
        }
    }
}
