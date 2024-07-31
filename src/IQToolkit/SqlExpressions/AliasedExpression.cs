// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// Base class for <see cref="SqlExpression"/> that declare a <see cref="TableAlias"/>.
    /// </summary>
    public abstract class AliasedExpression : SqlExpression
    {
        public TableAlias Alias { get; }

        protected AliasedExpression(Type type, TableAlias alias)
            : base(type)
        {
            this.Alias = alias;
        }
    }
}
