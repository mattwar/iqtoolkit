using System;
using System.Collections.Generic;
using System.Threading;

namespace IQToolkit.Utils
{
    /// <summary>
    /// Implements a cache over a most recently used list
    /// </summary>
    public class MostRecentlyUsedCache<T>
    {
        private readonly int _maxSize;
        private readonly List<T> _list;
        private readonly Func<T, T, bool> _fnEquals;
        private readonly ReaderWriterLockSlim _rwlock;
        private int _version;

        public MostRecentlyUsedCache(int maxSize)
            : this(maxSize, EqualityComparer<T>.Default)
        {
        }

        public MostRecentlyUsedCache(int maxSize, IEqualityComparer<T> comparer)
            : this(maxSize, (x,y) => comparer.Equals(x, y))
        {
        }

        public MostRecentlyUsedCache(int maxSize, Func<T,T,bool> fnEquals)
        {
            _list = new List<T>();
            _maxSize = maxSize;
            _fnEquals = fnEquals;
            _rwlock = new ReaderWriterLockSlim();
        }

        public int Count
        {
            get 
            {
                _rwlock.EnterReadLock();
                try
                {
                    return _list.Count;
                }
                finally
                {
                    _rwlock.ExitReadLock();
                }
            }
        }

        public void Clear()
        {
            _rwlock.EnterWriteLock();
            try
            {
                _list.Clear();
                _version++;
            }
            finally
            {
                _rwlock.ExitWriteLock();
            }
        }

        public bool Lookup(T item, bool add, out T cached)
        {
            cached = default!;
            int cacheIndex = -1;

            _rwlock.EnterReadLock();
            int version = _version;
            try
            {
                this.FindCachedItem(item, out cached, out cacheIndex);
            }
            finally
            {
                _rwlock.ExitReadLock();
            }

            // now update item in the list (only if we need to change its position or add it)
            if (cacheIndex != 0 && add)
            {
                _rwlock.EnterWriteLock();
                try
                {
                    // if list has changed find it again
                    this.FindCachedItem(item, out cached, out cacheIndex);

                    if (cacheIndex == -1)
                    {
                        // this is first time in list, put at start
                        _list.Insert(0, item);
                        cached = item;
                    }
                    else
                    {
                        if (cacheIndex > 0)
                        {
                            // if item is not at start, move it to the start
                            _list.RemoveAt(cacheIndex);
                            _list.Insert(0, item);
                        }
                    }

                    // drop any items beyond max
                    if (_list.Count > _maxSize)
                    {
                        _list.RemoveAt(_list.Count - 1);
                    }

                    _version++;
                }
                finally
                {
                    _rwlock.ExitWriteLock();
                }
            }

            return cacheIndex >= 0;
        }

        private void FindCachedItem(T item, out T cached, out int index)
        {
            for (int i = 0, n = _list.Count; i < n; i++)
            {
                cached = _list[i];

                if (_fnEquals(cached, item))
                {
                    index = i;
                    return;
                }
            }

            cached = default!;
            index = -1;
        }
    }
}
