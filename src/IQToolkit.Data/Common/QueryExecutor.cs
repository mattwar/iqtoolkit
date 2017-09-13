// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// An abstraction that represents the execution of database commands.
    /// </summary>
    public abstract class QueryExecutor
    {
        /// <summary>
        /// The number of rows affected by the execution of the last command.
        /// </summary>
        public abstract int RowsAffected { get; }

        /// <summary>
        /// Converts a value to the specified type.
        /// </summary>
        public abstract object Convert(object value, Type type);

        /// <summary>
        /// Executes the command once and and projects the rows of the resulting rowset into a sequence of values.
        /// </summary>
        public abstract IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues);

        /// <summary>
        /// Executes the command over a series of parameter sets, and returns the total number of rows affected.
        /// </summary>
        public abstract IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream);

        /// <summary>
        /// Execute the same command over a series of parameter sets, and produces a sequence of values, once per execution.
        /// </summary>
        public abstract IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<FieldReader, T> fnProjector, MappingEntity entity, int batchSize, bool stream);

        /// <summary>
        /// Produces an <see cref="IEnumerable{T}"/> that will execute the command when enumerated.
        /// </summary>
        public abstract IEnumerable<T> ExecuteDeferred<T>(QueryCommand query, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues);

        /// <summary>
        /// Execute a single command with the specified parameter values and return the number of rows affected.
        /// </summary>
        public abstract int ExecuteCommand(QueryCommand query, object[] paramValues);
    }
}