// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IQToolkit
{
    public static class ReadOnlyExtensions
    {
        /// <summary>
        /// Converts the sequence into a <see cref="ReadOnlyCollection{T}"/>
        /// </summary>
        public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> sequence)
        {
            ReadOnlyCollection<T> roc = sequence as ReadOnlyCollection<T>;
            if (roc == null)
            {
                if (sequence == null)
                {
                    roc = EmptyReadOnlyCollection<T>.Empty;
                }
                else
                {
                    roc = new List<T>(sequence).AsReadOnly();
                }
            }

            return roc;
        }

        private class EmptyReadOnlyCollection<T>
        {
            internal static readonly ReadOnlyCollection<T> Empty = new List<T>().AsReadOnly();
        }
    }
}
