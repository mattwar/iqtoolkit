// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Entities.Mapping;
using System;
using System.Collections.Generic;

namespace IQToolkit.Utils
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns the first items that are distinct on the selected value.
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, S>(
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

        /// <summary>
        /// Selects all items in the sequence 
        /// and all items referenced by the items in the sequence, 
        /// recursively.
        /// </summary>
        public static IEnumerable<T> SelectManyRecursive<T>(
            this IEnumerable<T> items,
            Func<T, IEnumerable<T>> selector,
            IEqualityComparer<T>? comparer = null)
        {
            var seen = new HashSet<T>(comparer ?? EqualityComparer<T>.Default);
            var list = new List<T>();

            foreach (var item in items)
            {
                list.Clear();
                GatherAll(item, selector, seen, list);

                foreach (var gatheredItem in list)
                {
                    yield return gatheredItem;
                }
            }
        }

        private static void GatherAll<T>(
            T item, 
            Func<T, IEnumerable<T>> selector,
            HashSet<T> seen, 
            List<T> list)
        {
            if (seen.Add(item))
            {
                list.Add(item);

                var referenced = selector(item);
                foreach (var refItem in referenced)
                {
                    GatherAll(item, selector, seen, list);
                }
            }
        }

        /// <summary>
        /// Converts the sequence to a dictionary. 
        /// If there are duplicate keys, the aggregator determines the value.
        /// </summary>
        public static Dictionary<K, V> ToDictionary<T, K, V>(
            this IEnumerable<T> sequence,
            Func<T, K> fnKey,
            Func<T, V> fnValue, 
            Func<K, V, V, V> fnAggregator)
        {
            var dict = new Dictionary<K, V>();

            foreach (var t in sequence)
            {
                var key = fnKey(t);
                var value = fnValue(t);

                if (dict.TryGetValue(key, out var existingValue))
                {
                    value = fnAggregator(key, existingValue, value);
                }

                dict[key] = value;
            }

            return dict;
        }

        /// <summary>
        /// Converts the sequence to a dictionary. 
        /// If there are duplicate keys, the first value wins.
        /// </summary>
        public static Dictionary<K, V> ToDictionary_FirstWins<T, K, V>(
            this IEnumerable<T> sequence,
            Func<T, K> fnKey,
            Func<T, V> fnValue) =>
            ToDictionary(sequence, fnKey, fnValue, (k, v1, v2) => v1);

        /// <summary>
        /// Converts the sequence to a dictionary. 
        /// If there are duplicate keys, the first value wins.
        /// </summary>
        public static Dictionary<K, T> ToDictionary_FirstWins<T, K>(
            this IEnumerable<T> sequence,
            Func<T, K> fnKey) =>
            ToDictionary_FirstWins(sequence, fnKey, t => t);

        /// <summary>
        /// Converts the sequence to a dictionary. 
        /// If there are duplicate keys, the last value wins.
        /// </summary>
        public static Dictionary<K, V> ToDictionary_LastWins<T, K, V>(
            this IEnumerable<T> sequence,
            Func<T, K> fnKey,
            Func<T, V> fnValue) =>
            ToDictionary(sequence, fnKey, fnValue, (k, v1, v2) => v2);

        /// <summary>
        /// Converts the sequence to a dictionary. 
        /// If there are duplicate keys, the last value wins.
        /// </summary>
        public static Dictionary<K, T> ToDictionary_LastWins<T, K>(
            this IEnumerable<T> sequence,
            Func<T, K> fnKey) =>
            ToDictionary_LastWins(sequence, fnKey, t => t);
    }
}