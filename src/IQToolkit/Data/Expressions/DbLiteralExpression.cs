// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Represents the raw text of a literal in the syntax of the database language.
    /// </summary>
    public sealed class DbLiteralExpression : DbExpression
    {
        public string LiteralText { get; }

        public DbLiteralExpression(Type type, string literalText)
            : base(type)
        {
            this.LiteralText = literalText;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.DbLiteral;

        public DbLiteralExpression Update(Type type, string literalText)
        {
            if (type != this.Type
                || literalText != this.LiteralText)
            {
                return new DbLiteralExpression(type, literalText);
            }
            else
            {
                return this;
            }
        }
    }
}
