// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// A database IF command
    /// </summary>
    public sealed class IfCommand : CommandExpression
    {
        /// <summary>
        /// The predicate expression.
        /// </summary>
        public Expression Test { get; }

        /// <summary>
        /// The action taken if the test is true.
        /// </summary>
        public Expression IfTrue { get; }

        /// <summary>
        /// The optional action taken if the test is false.
        /// </summary>
        public Expression? IfFalse { get; }

        public IfCommand(Expression test, Expression ifTrue, Expression? ifFalse = null)
            : base(ifTrue.Type)
        {
            this.Test = test;
            this.IfTrue = ifTrue;
            this.IfFalse = ifFalse;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.IfCommand;

        public IfCommand Update(
            Expression test, 
            Expression ifTrue, 
            Expression? ifFalse)
        {
            if (test != this.Test 
                || ifTrue != this.IfTrue 
                || ifFalse != this.IfFalse)
            {
                return new IfCommand(test, ifTrue, ifFalse);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitIfCommand(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var check = visitor.Visit(this.Test);
            var ifTrue = visitor.Visit(this.IfTrue);
            var ifFalse = visitor.Visit(this.IfFalse);
            return this.Update(check, ifTrue, ifFalse);
        }
    }
}
