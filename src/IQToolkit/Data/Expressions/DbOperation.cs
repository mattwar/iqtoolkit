// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Expressions
{
    public abstract class DbOperation : DbExpression
    {
        public override bool IsPredicate { get; }

        protected DbOperation(Type type, bool isPredicate)
            : base(type)
        {
            this.IsPredicate = isPredicate;
        }
    }
}
