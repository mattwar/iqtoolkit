// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IQToolkit.Utils
{
    /// <summary>
    /// Simple implementation of the <see cref="IGrouping{TKey, TElement}"/> interface
    /// </summary>
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly TKey _key;
        private IEnumerable<TElement> _group;

        public Grouping(TKey key, IEnumerable<TElement> group)
        {
            _key = key;
            _group = group;
        }

        public TKey Key => _key;

        public IEnumerator<TElement> GetEnumerator()
        {
            // cache group to avoid multiple enumeration
            if (!(_group is ImmutableList<TElement>))
            {
                _group = _group.ToImmutableList();
            }

            return _group.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }   
}