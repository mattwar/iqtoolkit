// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Expressions
{
    public abstract class AliasedExpression : DbExpression
    {
        public TableAlias Alias { get; }

        protected AliasedExpression(Type type, TableAlias alias)
            : base(type)
        {
            this.Alias = alias;
        }
    }
}
