using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IQToolkit
{
    public static class AsyncEnumerable
    {
        public static IAsyncEnumerable<T> ToAsync<T>(this IEnumerable<T> enumerable)
        {
            var ae = enumerable as IAsyncEnumerable<T>;
            if (ae != null)
            {
                return ae;
            }

            return new Wrapper<T>(enumerable);
        }

        public static Task<IAsyncEnumerator<T>> GetEnumeratorAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            return enumerable.ToAsync().GetEnumeratorAsync(cancellationToken);
        }

        private class Wrapper<T> : IAsyncEnumerable<T>
        {
            private readonly IEnumerable<T> enumerable;

            public Wrapper(IEnumerable<T> enumerable)
            {
                this.enumerable = enumerable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this.enumerable.GetEnumerator();
            }

            public Task<IAsyncEnumerator<T>> GetEnumeratorAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(this.enumerable.GetEnumerator().ToAsync());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.enumerable.GetEnumerator();
            }
        }

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var enumerator = await enumerable.GetEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                return await enumerator.ToListAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<List<T>> ToListAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ae = enumerable as IAsyncEnumerable<T>;
            if (ae != null)
            {
                return ae.ToListAsync(cancellationToken);
            }
            else
            {
                return Task.FromResult(enumerable.ToList());
            }
        }

        public static Task<T> SingleAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ae = enumerable as IAsyncEnumerable<T>;
            if (ae != null)
            {
                return SingleAsync(ae, cancellationToken);
            }
            else
            {
                return Task.FromResult(enumerable.Single());
            }
        }

        public static async Task<T> SingleAsync<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var enumerator = await enumerable.GetEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        throw MoreThanOneElement();
                    }

                    return value;
                }
                else
                {
                    throw EmptySequence();
                }
            }
        }

        public static Task<T> SingleOrDefaultAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ae = enumerable as IAsyncEnumerable<T>;
            if (ae != null)
            {
                return SingleOrDefaultAsync(ae);
            }
            else
            {
                return Task.FromResult(enumerable.SingleOrDefault());
            }
        }

        public static async Task<T> SingleOrDefaultAsync<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            using (var enumerator = await enumerable.GetEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        throw MoreThanOneElement();
                    }

                    return value;
                }
                else
                {
                    return default(T);
                }
            }
        }

        public static Task<T> FirstAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ae = enumerable as IAsyncEnumerable<T>;
            if (ae != null)
            {
                return FirstAsync(ae, cancellationToken);
            }
            else
            {
                return Task.FromResult(enumerable.First());
            }
        }

        public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var enumerator = await enumerable.GetEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        throw MoreThanOneElement();
                    }

                    return value;
                }
                else
                {
                    throw EmptySequence();
                }
            }
        }

        public static Task<T> FirstOrDefaultAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ae = enumerable as IAsyncEnumerable<T>;
            if (ae != null)
            {
                return FirstOrDefaultAsync(ae, cancellationToken);
            }
            else
            {
                return Task.FromResult(enumerable.FirstOrDefault());
            }
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var enumerator = await enumerable.GetEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    if (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        throw MoreThanOneElement();
                    }

                    return value;
                }
                else
                {
                    throw EmptySequence();
                }
            }
        }

        private static Exception EmptySequence()
        {
            return new InvalidOperationException("More than one element in sequence.");
        }

        private static Exception MoreThanOneElement()
        {
            return new InvalidOperationException("More than one element in sequence.");
        }
    }
}
