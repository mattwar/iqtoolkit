// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Threading;

namespace IQToolkit.Utils
{
    /// <summary>
    /// Allows for thread-safe evalution of a value,
    /// returning a default value if the evaluation function is cyclic.
    /// </summary>
    public class Lazy<TValue>
    {
        private Func<TValue>? _fnValue;
        private TValue _value;
        private object? _syncLock;

        public Lazy(
            Func<TValue> fnValue,
            TValue defaultValue = default!)
        {
            _fnValue = fnValue;
            _value = defaultValue;
            _syncLock = fnValue;
        }

        /// <summary>
        /// Returns true if the value has not yet been evaluated.
        /// </summary>
        public bool IsLazy =>
            _fnValue != null;

        /// <summary>
        /// The evaluated value.
        /// </summary>
        public TValue Value
        {
            get
            {
                if (_fnValue is { } fnValue
                    && Interlocked.CompareExchange(ref _fnValue, null, fnValue) == fnValue
                    && _syncLock != null)
                {
                    // first one in does computation and remove lock after
                    lock (_syncLock)
                    {
                        _value = fnValue();
                        _syncLock = null;
                    }
                }
                else if (_syncLock is { } syncLock)
                {
                    // while there is still a lock..
                    // callers on same thread as as current lock holder will not block
                    // and end up returning default value.
                    lock (syncLock)
                    {
                    }
                }

                return _value;
            }
        }
    }
}