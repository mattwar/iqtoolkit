// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;

namespace IQToolkit
{
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
    public class DeferredList<T> : IDeferredList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable, IDeferLoadable
    {
        private readonly IEnumerable<T> source;
        private List<T> values;

        /// <summary>
        /// Construct a new <see cref="DeferredList{T}"/>
        /// </summary>
        /// <param name="source">The sequence of values that will be enumerated when <see cref="Load"/> is invoked.</param>
        public DeferredList(IEnumerable<T> source)
        {
            this.source = source;
        }

        /// <summary>
        /// Loads the list if not already loaded.
        /// </summary>
        public void Load()
        {
            if (!this.IsLoaded)
            {
                this.values = new List<T>(this.source);
            }
        }

        /// <summary>
        /// True if the list is already loaded.
        /// </summary>
        public bool IsLoaded
        {
            get { return this.values != null; }
        }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            this.Load();
            return this.values.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.Load();
            this.values.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this.Load();
            this.values.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                this.Load();
                return this.values[index];
            }
            set
            {
                this.Load();
                this.values[index] = value;
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            this.Load();
            this.values.Add(item);
        }

        public void Clear()
        {
            this.Load();
            this.values.Clear();
        }

        public bool Contains(T item)
        {
            this.Load();
            return this.values.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.Load();
            this.values.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { this.Load(); return this.values.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            this.Load();
            return this.values.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            this.Load();
            return this.values.GetEnumerator();
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
            return ((IList)this.values).Add(value);
        }

        public bool Contains(object value)
        {
            this.Load();
            return ((IList)this.values).Contains(value);
        }

        public int IndexOf(object value)
        {
            this.Load();
            return ((IList)this.values).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            this.Load();
            ((IList)this.values).Insert(index, value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            this.Load();
            ((IList)this.values).Remove(value);
        }

        object IList.this[int index]
        {
            get
            {
                this.Load();
                return ((IList)this.values)[index];
            }
            set
            {
                this.Load();
                ((IList)this.values)[index] = value;
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            this.Load();
            ((IList)this.values).CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return null; }
        }

        #endregion
    }
}