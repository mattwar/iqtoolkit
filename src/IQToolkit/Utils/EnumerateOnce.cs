// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace IQToolkit.Utils
{
    /// <summary>
    /// An <see cref="IEnumerable{T}"/> wrapper that only allows 
    /// the underlying enumerable to be enumerated once.
    /// </summary>
    public class EnumerateOnce<T> : IEnumerable<T>, IEnumerable
    {
        private IEnumerable<T>? _enumerable;

        public EnumerateOnce(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var en = Interlocked.Exchange(ref _enumerable, null);

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