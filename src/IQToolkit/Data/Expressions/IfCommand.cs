// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A database IF command
    /// </summary>
    public sealed class IfCommand : CommandExpression
    {
        public Expression Check { get; }
        public Expression IfTrue { get; }
        public Expression? IfFalse { get; }

        public IfCommand(Expression check, Expression ifTrue, Expression? ifFalse = null)
            : base(ifTrue.Type)
        {
            this.Check = check;
            this.IfTrue = ifTrue;
            this.IfFalse = ifFalse;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.If;

        public IfCommand Update(
            Expression check, 
            Expression ifTrue, 
            Expression? ifFalse)
        {
            if (check != this.Check 
                || ifTrue != this.IfTrue 
                || ifFalse != this.IfFalse)
            {
                return new IfCommand(check, ifTrue, ifFalse);
            }
            else
            {
                return this;
            }
        }
    }
}
