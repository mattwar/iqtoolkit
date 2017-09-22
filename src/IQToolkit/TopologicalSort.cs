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
    /// Sorts a sequence of items in a graph.
    /// </summary>
    public static class TopologicalSorter
    {
        /// <summary>
        /// Returns the items in order relative to other items.
        /// </summary>
        /// <param name="items">The input items.</param>
        /// <param name="fnItemsBeforeMe">A function that yields items known to be ordered before this item.</param>
        public static IEnumerable<T> Sort<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> fnItemsBeforeMe)
        {
            return Sort<T>(items, fnItemsBeforeMe, null);
        }

        /// <summary>
        /// Returns the items in order relative to other items.
        /// </summary>
        /// <param name="items">The input items.</param>
        /// <param name="fnItemsBeforeMe">A function that yields items known to be ordered before this item.</param>
        /// <param name="comparer">An equality comparer for the items.</param>
        public static IEnumerable<T> Sort<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> fnItemsBeforeMe, IEqualityComparer<T> comparer)
        {
            HashSet<T> seen = comparer != null ? new HashSet<T>(comparer) : new HashSet<T>();
            HashSet<T> done = comparer != null ? new HashSet<T>(comparer) : new HashSet<T>();
            List<T> result = new List<T>();

            foreach (var item in items)
            {
                SortItem(item, fnItemsBeforeMe, seen, done, result);
            }

            return result;
        }

        private static void SortItem<T>(T item, Func<T, IEnumerable<T>> fnItemsBeforeMe, HashSet<T> seen, HashSet<T> done, List<T> result)
        {
            if (!done.Contains(item))
            {
                if (seen.Contains(item))
                {
                    throw new InvalidOperationException("Cycle in topological sort");
                }

                seen.Add(item);

                var itemsBefore = fnItemsBeforeMe(item);
                if (itemsBefore != null)
                {
                    foreach (var itemBefore in itemsBefore)
                    {
                        SortItem(itemBefore, fnItemsBeforeMe, seen, done, result);
                    }
                }

                result.Add(item);
                done.Add(item);
            }
        }
    }
}