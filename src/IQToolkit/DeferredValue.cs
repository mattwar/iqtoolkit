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
        private IEnumerable<T> source;
        private bool loaded;
        private T value;

        /// <summary>
        /// Constructs a <see cref="DeferredValue{T}"/>
        /// </summary>
        public DeferredValue(T value)
        {
            this.value = value;
            this.source = null;
            this.loaded = true;
        }

        /// <summary>
        /// Constructs a <see cref="DeferredValue{T}"/> from the first item in the sequence
        /// when the <see cref="Load"/> method is invoked.
        /// </summary>
        public DeferredValue(IEnumerable<T> source)
        {
            this.source = source;
            this.loaded = false;
            this.value = default(T);
        }

        /// <summary>
        /// Loads the value if it is not already loaded.
        /// </summary>
        public void Load()
        {
            if (this.source != null && !this.IsLoaded)
            {
                this.value = this.source.SingleOrDefault();
                this.loaded = true;
            }
        }

        /// <summary>
        /// True if the value is already loaded.
        /// </summary>
        public bool IsLoaded
        {
            get { return this.loaded; }
        }

        /// <summary>
        /// True if the value was assigned instead of being loaded from a source.
        /// </summary>
        public bool IsAssigned
        {
            get { return this.loaded && this.source == null; }
        }

        /// <summary>
        /// The value that is defer loaded.
        /// The value will be loaded if not already loaded or assigned when this property is read.
        /// </summary>
        public T Value
        {
            get
            {
                this.Load();
                return this.value;
            }

            set
            {
                this.value = value;
                this.loaded = true;
                this.source = null;
            }
        }
    }
}