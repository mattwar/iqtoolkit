// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// Represents the raw text of a literal in the syntax of the database language.
    /// </summary>
    public sealed class LiteralExpression : SqlExpression
    {
        public string LiteralText { get; }

        public LiteralExpression(Type type, string literalText)
            : base(type)
        {
            this.LiteralText = literalText;
        }

        public LiteralExpression Update(Type type, string literalText)
        {
            if (type != this.Type
                || literalText != this.LiteralText)
            {
                return new LiteralExpression(type, literalText);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitLiteral(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            return this;
        }
    }
}
