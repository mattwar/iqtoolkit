// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// The base type of all command expressions.
    /// </summary>
    public abstract class CommandExpression : DbExpression
    {
        protected CommandExpression(Type type)
            : base(type)
        {
        }
    }
}
