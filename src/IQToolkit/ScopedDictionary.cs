// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;

namespace IQToolkit
{
    /// <summary>
    /// A <see cref="ScopedDictionary{TKey, TValue}"/> is a dictionary that contains all the
    /// items of another dictionary, plus any additional items added directly to it.
    /// </summary>
    public class ScopedDictionary<TKey, TValue>
    {
        private readonly ScopedDictionary<TKey, TValue> previous;
        private readonly Dictionary<TKey, TValue> map;

        /// <summary>
        /// Construct a new <see cref="ScopedDictionary{TKey, TValue}"/> given a previous dictionary.
        /// </summary>
        public ScopedDictionary(ScopedDictionary<TKey, TValue> previous)
        {
            this.previous = previous;
            this.map = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Construct a <see cref="ScopedDictionary{TKey, TValue}"/> given a previous dictionary and a
        /// sequence of key-value pairs.
        /// </summary>
        public ScopedDictionary(ScopedDictionary<TKey, TValue> previous, IEnumerable<KeyValuePair<TKey, TValue>> pairs)
            : this(previous)
        {
            foreach (var p in pairs)
            {
                this.map.Add(p.Key, p.Value);
            }
        }

        /// <summary>
        /// Add a new value to the <see cref="ScopedDictionary{TKey, TValue}"/>
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            this.map.Add(key, value);
        }

        /// <summary>
        /// Try to get the value for a given key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            for (ScopedDictionary<TKey, TValue> scope = this; scope != null; scope = scope.previous)
            {
                if (scope.map.TryGetValue(key, out value))
                    return true;
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Returns true if the <see cref="ScopedDictionary{TKey, TValue}"/> contains the key.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            for (ScopedDictionary<TKey, TValue> scope = this; scope != null; scope = scope.previous)
            {
                if (scope.map.ContainsKey(key))
                    return true;
            }

            return false;
        }
    }
}