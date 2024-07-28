// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IQToolkit.Utils
{
    public static class ReadOnlyExtensions
    {
        /// <summary>
        /// Converts the sequence into a <see cref="IReadOnlyList{T}"/>>
        /// </summary>
        public static IReadOnlyList<T> ToReadOnly<T>(this IEnumerable<T>? sequence)
        {
            return sequence.ToImmutable();
        }

        /// <summary>
        /// Converts the sequence into a <see cref="ImmutalbeList{T}"/>>
        /// </summary>
        private static ImmutableList<T> ToImmutable<T>(this IEnumerable<T>? sequence)
        {
            if (sequence is ImmutableList<T> imm)
                return imm;

            if (sequence == null
                || (sequence is IReadOnlyList<T> rol && rol.Count == 0))
            {
                return ImmutableList<T>.Empty;
            }
            else
            {
                return ImmutableList<T>.Empty.AddRange(sequence);
            }
        }

        public static IReadOnlyList<T> Add<T>(this IReadOnlyList<T> list, T value) =>
            list.ToImmutable().Add(value);

        public static IReadOnlyList<T> AddRange<T>(this IReadOnlyList<T> list, IEnumerable<T> values) =>
            list.ToImmutable().AddRange(values);

        public static IReadOnlyList<T> SetItem<T>(this IReadOnlyList<T> list, int index, T value) =>
            list.ToImmutable().SetItem(index, value);

        public static IReadOnlyList<T> Insert<T>(this IReadOnlyList<T> list, int index, T value) =>
            list.ToImmutable().Insert(index, value);

        public static IReadOnlyList<T> InsertRange<T>(this IReadOnlyList<T> list, int index, IEnumerable<T> values) =>
            list.ToImmutable().InsertRange(index, values);

        public static IReadOnlyList<T> Remove<T>(this IReadOnlyList<T> list, T value) =>
            list.ToImmutable().Remove(value);

        public static IReadOnlyList<T> RemoveAt<T>(this IReadOnlyList<T> list, int index) =>
            list.ToImmutable().RemoveAt(index);

        public static IReadOnlyList<T> RemoveRange<T>(this IReadOnlyList<T> list, IEnumerable<T> values) =>
            list.ToImmutable().RemoveRange(values);
    }

    public static class ReadOnlyList<T>
    {
        public static IReadOnlyList<T> Empty => ImmutableList<T>.Empty;
    }
}
