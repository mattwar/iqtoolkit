// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Immutable;

namespace IQToolkit
{
    public class QueryOptions
    {
        private readonly ImmutableDictionary<int, object?> _valueMap;

        private QueryOptions(ImmutableDictionary<int, object?> valueMap)
        {
            _valueMap = valueMap;
        }

        public TValue GetOption<TValue>(QueryOption<TValue> option)
        {
            return _valueMap.TryGetValue(option.Id, out var value)
                && value is TValue tValue
                ? tValue
                : option.Default;
        }

        public QueryOptions WithOption<TValue>(QueryOption<TValue> option, TValue value)
        {
            return new QueryOptions(_valueMap.SetItem(option.Id, value));
        }

        public static readonly QueryOptions Default =
            new QueryOptions(ImmutableDictionary<int, object?>.Empty);
    }

    public class QueryOption<TValue>
    {
        internal int Id { get; }

        public string Name { get; }
        public TValue Default { get; }

        private static int _nextId;

        public QueryOption(string name, TValue defaultValue)
        {
            this.Id = ++_nextId;
            this.Name = name;
            this.Default = defaultValue;
        }
    }
}