// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Expressions.Sql
{
    /// <summary>
    /// The base type of all command expressions.
    /// </summary>
    public abstract class CommandExpression : SqlExpression
    {
        protected CommandExpression(Type type)
            : base(type)
        {
        }
    }
}
