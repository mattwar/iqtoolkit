// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IQToolkit
{
    /// <summary>
    /// An <see cref="IEnumerable{T}"/> wrapper that only allows 
    /// the underlying enumerable to be enumerated once.
    /// </summary>
    public class EnumerateOnce<T> : IEnumerable<T>, IEnumerable
    {
        private IEnumerable<T> enumerable;

        public EnumerateOnce(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var en = Interlocked.Exchange(ref enumerable, null);

            if (en != null)
            {
                return en.GetEnumerator();
            }

            throw new Exception("Enumerated more than once.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}