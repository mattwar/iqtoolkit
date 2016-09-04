// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Threading;
using System.Threading.Tasks;

namespace IQToolkit
{
    public interface IAsyncBufferable : IBufferable
    {
        Task BufferAsync(CancellationToken cancellationToken);
    }
}