// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IQToolkit
{
    public static class AsyncEnumerator
    {
        public static IAsyncEnumerator<T> ToAsync<T>(this IEnumerator<T> enumerator)
        {
            var ae = enumerator as IAsyncEnumerator<T>;
            if (ae == null)
            {
                ae = new Wrapper<T>(enumerator);
            }

            return ae;
        }

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = new List<T>();

            while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(enumerator.Current);
            }

            return list;
        }

        public static List<T> ToList<T>(this IEnumerator<T> enumerator)
        {
            var list = new List<T>();

            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }

            return list;
        }

        private class Wrapper<T> : IAsyncEnumerator<T>, IEnumerator<T>, IEnumerator
        {
            private readonly IEnumerator<T> enumerator;

            public Wrapper(IEnumerator<T> enumerator)
            {
                this.enumerator = enumerator;
            }

            public T Current
            {
                get
                {
                    return this.enumerator.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.enumerator.Current;
                }
            }

            public void Dispose()
            {
                this.enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return this.enumerator.MoveNext();
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(this.MoveNext());
            }

            public void Reset()
            {
                this.enumerator.Reset();
            }
        }
    }
}
