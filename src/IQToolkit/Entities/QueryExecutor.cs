// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.IO;

namespace IQToolkit.Entities
{
    using Mapping;
    using Utils;

    /// <summary>
    /// A base class for executors that execute database queries and commands.
    /// </summary>
    public abstract class QueryExecutor
    {
        /// <summary>
        /// The current <see cref="TypeConverter"/> used to convert result value types.
        /// </summary>
        public abstract TypeConverter Converter { get; }

        /// <summary>
        /// The <see cref="TextWriter"/> used to log messages.
        /// </summary>
        public abstract TextWriter? Log { get; }

        /// <summary>
        /// The current <see cref="Data.QueryTypeSystem"/> used to translate CLR types to database types.
        /// </summary>
        public abstract QueryTypeSystem TypeSystem { get; }

        /// <summary>
        /// Creates a new <see cref="QueryExecutor"/> with the <see cref="Converter"/> property assigned.
        /// </summary>
        public QueryExecutor WithConverter(TypeConverter converter) =>
            With(converter: converter);

        /// <summary>
        /// Creates a new <see cref="QueryExecutor"/> with the <see cref="Log"/> property assigned.
        /// </summary>
        public QueryExecutor WithLog(TextWriter? log) =>
            With(log: log);

        /// <summary>
        /// Creates a new <see cref="QueryExecutor"/> with the <see cref="TypeSystem"/> property assigned.
        /// </summary>
        public QueryExecutor WithTypeSystem(QueryTypeSystem typeSystem) =>
            With(typeSystem: typeSystem);

        protected virtual QueryExecutor With(
            TypeConverter? converter = null,
            QueryTypeSystem? typeSystem = null,
            Optional<TextWriter?> log = default)
        {
            var newConverter = converter ?? this.Converter;
            var newTypeSystem = typeSystem ?? this.TypeSystem;
            var newLog = log.HasValue ? log.Value : this.Log;

            if (newConverter != this.Converter
                || newTypeSystem != this.TypeSystem
                || newLog != this.Log)
            {
                return Construct(newConverter, newTypeSystem, newLog);
            }

            return this;
        }

        protected abstract QueryExecutor Construct(
            TypeConverter converter,
            QueryTypeSystem typeSystem,
            TextWriter? log
            );

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
        public abstract int ExecuteCommand(QueryCommand query, object?[]? paramValues = null);

        /// <summary>
        /// Invokes the <see cref="Action"/> while the executor is connection.
        /// </summary>
        public virtual void DoConnected(Action action)
        {
            action();
        }

        /// <summary>
        /// Invokes the <see cref="Action"/> under a transation if possible.
        /// </summary>
        public virtual void DoTransacted(Action action)
        {
            action();
        }
    }
}