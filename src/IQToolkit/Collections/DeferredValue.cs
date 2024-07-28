// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IQToolkit
{
    /// <summary>
    /// A defer loaded value.
    /// </summary>
    public struct DeferredValue<T> : IDeferLoadable
    {
        private IEnumerable<T>? _source;
        private bool _loaded;
        private T _value;

        /// <summary>
        /// Constructs a <see cref="DeferredValue{T}"/>
        /// </summary>
        public DeferredValue(T value)
        {
            _value = value;
            _source = null;
            _loaded = true;
        }

        /// <summary>
        /// Constructs a <see cref="DeferredValue{T}"/> from the first item in the sequence
        /// when the <see cref="Load"/> method is invoked.
        /// </summary>
        public DeferredValue(IEnumerable<T> source)
        {
            _source = source;
            _loaded = false;
            _value = default!;
        }

        /// <summary>
        /// Loads the value if it is not already loaded.
        /// </summary>
        public void Load()
        {
            if (_source != null && !this.IsLoaded)
            {
                _value = _source.SingleOrDefault();
                _loaded = true;
            }
        }

        /// <summary>
        /// True if the value is already loaded.
        /// </summary>
        public bool IsLoaded => _loaded;

        /// <summary>
        /// True if the value was assigned instead of being loaded from a source.
        /// </summary>
        public bool IsAssigned => IsLoaded && _source == null;

        /// <summary>
        /// The value that is defer loaded.
        /// The value will be loaded if not already loaded or assigned when this property is read.
        /// </summary>
        public T Value
        {
            get
            {
                this.Load();
                return _value;
            }

            set
            {
                _value = value;
                _loaded = true;
                _source = null;
            }
        }
    }
}