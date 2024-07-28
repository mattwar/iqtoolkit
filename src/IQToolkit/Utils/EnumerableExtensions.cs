// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;

namespace IQToolkit.Utils
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns the first items that are distinct on the selected value.
        /// </summary>
        public static IEnumerable<T> DistinctBy<T,S>(
            this IEnumerable<T> sequence,
            Func<T, S> selector)
            where S : IEquatable<S>
        {
            var seen = new HashSet<S>();
            foreach (var item in sequence)
            {
                var value = selector(item);
                if (seen.Add(value))
                {
                    yield return item;
                }
            }
        }
    }
}