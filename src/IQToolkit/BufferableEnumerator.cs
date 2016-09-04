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
    /// An enumerator that can be buffered on demand.
    /// </summary>
    public class BufferableEnumerator<T> : IAsyncEnumerator<T>, IAsyncBufferable
    {
        private IEnumerator<T> enumerator;
        private bool buffered;

        public BufferableEnumerator(IEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

        public void Buffer()
        {
            if (!this.buffered)
            {
                var original = this.enumerator;
                var list = this.enumerator.ToList();
                this.enumerator = list.GetEnumerator();
                this.buffered = true;
                original.Dispose();
            }
        }

        public async Task BufferAsync(CancellationToken cancellationToken)
        {
            if (!this.buffered)
            {
                var original = this.enumerator;
                var list = await this.enumerator.ToAsync().ToListAsync(cancellationToken).ConfigureAwait(false);
                this.enumerator = list.GetEnumerator().ToAsync();
                this.buffered = true;
                original.Dispose();
            }
        }

        public T Current
        {
            get { return this.enumerator.Current; }
        }

        object IEnumerator.Current
        {
            get { return this.Current; }
        }

        public void Dispose()
        {
            this.enumerator.Dispose();
        }

        public bool MoveNext()
        {
            return this.enumerator.MoveNext();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return this.GetAsyncEnumerator().MoveNextAsync(cancellationToken);
        }

        private IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var ae = this.enumerator as IAsyncEnumerator<T>;
            if (ae == null)
            {
                this.enumerator = ae = this.enumerator.ToAsync();
            }

            return ae;
        }

        public void Reset()
        {
            this.enumerator.Reset();
        }
    }
}