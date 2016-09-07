// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IQToolkit
{
    /// <summary>
    /// An enumerable wrapper that buffers the results of the underyling enumerable 
    /// the first time it is enumerated.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BufferedEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> enumerable;
        private IEnumerable<T> buffer;

        public BufferedEnumerable(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        public IEnumerable<T> UnderlyingEnumerable
        {
            get { return this.enumerable; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (this.buffer == null)
            {
                this.buffer = this.enumerable.ToList();
            }

            return this.buffer.GetEnumerator();
        }

        public async Task<IAsyncEnumerator<T>> GetEnumeratorAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.buffer == null)
            {
                this.buffer = await this.enumerable.ToListAsync(cancellationToken).ConfigureAwait(false);
            }

            return this.buffer.GetEnumerator().ToAsync();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}