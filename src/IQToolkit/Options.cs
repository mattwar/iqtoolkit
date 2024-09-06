// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Immutable;

namespace IQToolkit
{
    public class Options
    {
        private readonly ImmutableDictionary<int, object?> _valueMap;

        private Options(ImmutableDictionary<int, object?> valueMap)
        {
            _valueMap = valueMap;
        }

        public TValue GetOption<TValue>(Option<TValue> option)
        {
            return _valueMap.TryGetValue(option.Id, out var value)
                && value is TValue tValue
                ? tValue
                : option.Default;
        }

        public Options WithOption<TValue>(Option<TValue> option, TValue value)
        {
            return new Options(_valueMap.SetItem(option.Id, value));
        }

        public static readonly Options Default =
            new Options(ImmutableDictionary<int, object?>.Empty);
    }

    public class Option<TValue>
    {
        internal int Id { get; }

        public string Name { get; }
        public TValue Default { get; }

        private static int _nextId;

        public Option(string name, TValue defaultValue)
        {
            this.Id = ++_nextId;
            this.Name = name;
            this.Default = defaultValue;
        }
    }
}