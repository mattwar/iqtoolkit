using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace IQToolkit
{
    /// <summary>
    /// Implements a cache over a most recently used list
    /// </summary>
    public class MostRecentlyUsedCache<T>
    {
        private readonly int maxSize;
        private readonly List<T> list;
        private readonly Func<T, T, bool> fnEquals;
        private readonly ReaderWriterLockSlim rwlock;
        private int version;

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
            this.list = new List<T>();
            this.maxSize = maxSize;
            this.fnEquals = fnEquals;
            this.rwlock = new ReaderWriterLockSlim();
        }

        public int Count
        {
            get 
            {
                this.rwlock.EnterReadLock();
                try
                {
                    return this.list.Count;
                }
                finally
                {
                    this.rwlock.ExitReadLock();
                }
            }
        }

        public void Clear()
        {
            this.rwlock.EnterWriteLock();
            try
            {
                this.list.Clear();
                this.version++;
            }
            finally
            {
                this.rwlock.ExitWriteLock();
            }
        }

        public bool Lookup(T item, bool add, out T cached)
        {
            cached = default(T);
            int cacheIndex = -1;

            rwlock.EnterReadLock();
            int version = this.version;
            try
            {
                this.FindCachedItem(item, out cached, out cacheIndex);
            }
            finally
            {
                rwlock.ExitReadLock();
            }

            // now update item in the list (only if we need to change its position or add it)
            if (cacheIndex != 0 && add)
            {
                rwlock.EnterWriteLock();
                try
                {
                    // if list has changed find it again
                    this.FindCachedItem(item, out cached, out cacheIndex);

                    if (cacheIndex == -1)
                    {
                        // this is first time in list, put at start
                        this.list.Insert(0, item);
                        cached = item;
                    }
                    else
                    {
                        if (cacheIndex > 0)
                        {
                            // if item is not at start, move it to the start
                            this.list.RemoveAt(cacheIndex);
                            this.list.Insert(0, item);
                        }
                    }

                    // drop any items beyond max
                    if (this.list.Count > this.maxSize)
                    {
                        this.list.RemoveAt(this.list.Count - 1);
                    }

                    this.version++;
                }
                finally
                {
                    rwlock.ExitWriteLock();
                }
            }

            return cacheIndex >= 0;
        }

        private void FindCachedItem(T item, out T cached, out int index)
        {
            for (int i = 0, n = this.list.Count; i < n; i++)
            {
                cached = this.list[i];

                if (fnEquals(cached, item))
                {
                    index = i;
                    return;
                }
            }

            cached = default(T);
            index = -1;
        }
    }
}
