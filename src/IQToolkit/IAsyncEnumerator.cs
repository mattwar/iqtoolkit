// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IQToolkit
{
    public interface IAsyncEnumerator<out T> : IEnumerator<T>
    {
        Task<bool> MoveNextAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
