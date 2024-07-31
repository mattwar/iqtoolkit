// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Entities
{
    /// <summary>
    /// A type that exposes a <see cref="Executor"/> property.
    /// </summary>
    public interface IHaveExecutor
    {
        QueryExecutor Executor { get; }
    }
}
