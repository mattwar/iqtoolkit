// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IQToolkit.Collections
{
    using Utils;

    /// <summary>
    /// Common interface for controlling defer-loadable types
    /// </summary>
    public interface IDeferLoadable
    {
        bool IsLoaded { get; }
        void Load();
    }

    /// <summary>
    /// An interface denoting a list that can be defer loaded.
    /// </summary>
    public interface IDeferredList : IList, IDeferLoadable
    {
    }

    /// <summary>
    /// An interface denoting a list that can be defer loaded.
    /// </summary>
    public interface IDeferredList<T> : IList<T>, IDeferredList
    {
    }

    /// <summary>
    /// A <see cref="IList{T}"/> that is loaded the first time the contents are examined, 
    /// or when the <see cref="Load"/> method is called.
    /// </summary>
    public class DeferredList<T> 
        : IDeferredList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable, IDeferLoadable
    {
        private readonly Lazy<List<T>> _lazy;
        private List<T> LoadedList => _lazy.Value;

        /// <summary>
        /// Construct a new <see cref="DeferredList{T}"/>
        /// </summary>
        /// <param name="source">The sequence of values that will be enumerated when <see cref="Load"/> is invoked.</param>
        public DeferredList(IEnumerable<T> source)
        {
            _lazy = new Lazy<List<T>>(() => source.ToList());
        }

        /// <summary>
        /// Loads the list if not already loaded.
        /// </summary>
        public void Load()
        {
            // referencing causes loading
            var _ = this.LoadedList;
        }

        /// <summary>
        /// True if the list is already loaded.
        /// </summary>
        public bool IsLoaded
        {
            get { return !_lazy.IsLazy; }
        }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            this.Load();
            return this.LoadedList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.Load();
            this.LoadedList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this.Load();
            this.LoadedList.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                this.Load();
                return this.LoadedList[index];
            }
            set
            {
                this.Load();
                this.LoadedList[index] = value;
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            this.Load();
            this.LoadedList.Add(item);
        }

        public void Clear()
        {
            this.Load();
            this.LoadedList.Clear();
        }

        public bool Contains(T item)
        {
            this.Load();
            return this.LoadedList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.Load();
            this.LoadedList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { this.Load(); return this.LoadedList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            this.Load();
            return this.LoadedList.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            this.Load();
            return this.LoadedList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IList Members

        public int Add(object value)
        {
            this.Load();
            return ((IList)LoadedList).Add(value);
        }

        public bool Contains(object value)
        {
            this.Load();
            return ((IList)LoadedList).Contains(value);
        }

        public int IndexOf(object value)
        {
            this.Load();
            return ((IList)this.LoadedList).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            this.Load();
            ((IList)this.LoadedList).Insert(index, value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            this.Load();
            ((IList)this.LoadedList).Remove(value);
        }

        object IList.this[int index]
        {
            get
            {
                this.Load();
                return ((IList)this.LoadedList)[index];
            }
            set
            {
                this.Load();
                ((IList)this.LoadedList)[index] = value;
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            this.Load();
            ((IList)this.LoadedList).CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object? SyncRoot
        {
            get { return null; }
        }

        #endregion
    }
}