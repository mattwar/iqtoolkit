using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.IO;

namespace IQToolkit.Data
{
    using Mapping;
    using Sql;
    using Utils;
     
    /// <summary>
    /// A <see cref="QueryExecutor"/> that executes commands with a <see cref="DbConnection"/>
    /// </summary>
    public class DbQueryExecutor : QueryExecutor
    {
        /// <summary>
        /// The current <see cref="IDbConnection"/>
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// The isolation level used for transactions.
        /// </summary>
        public IsolationLevel Isolation { get; }

        /// <summary>
        /// The current <see cref="IDbTransaction"/>
        /// </summary>
        public IDbTransaction? Transaction { get; private set; }

        /// <summary>
        /// The <see cref="TypeConverter"/> to use when reading query results.
        /// </summary>
        public override TypeConverter Converter { get; }

        /// <summary>
        /// The <see cref="Data.QueryTypeSystem"/> used to translate CLR types to database types.
        /// </summary>
        public override QueryTypeSystem TypeSystem { get; }

        /// <summary>
        /// The <see cref="TextWriter"/> used to log messages.
        /// </summary>
        public override TextWriter? Log { get; }

        /// <summary>
        /// The number of concurrent actions using the connection.
        /// It will stay open while the count > 0.
        /// </summary>
        private int _nConnectedActions = 0;

        /// <summary>
        /// Remember if a query execution action opened the connection.
        /// If the connection was already open when an execution action occurred, 
        /// the connection will remain open after the execution.
        /// </summary>
        private bool _actionOpenedConnection = false;

        protected DbQueryExecutor(
            IDbConnection connection,
            IsolationLevel isolation,
            IDbTransaction? transaction,
            TypeConverter? converter,
            QueryTypeSystem? typeSystem,
            TextWriter? log)
        {
            this.Connection = connection;
            this.Isolation = isolation;
            this.Transaction = transaction;
            this.Converter = converter ?? TypeConverter.Default;
            this.TypeSystem = typeSystem ?? SqlTypeSystem.Singleton;
            this.Log = log;
        }

        public DbQueryExecutor(
            IDbConnection connection,
            IsolationLevel isolation = IsolationLevel.ReadCommitted,
            TypeConverter? converter = null,
            QueryTypeSystem? typeSystem = null,
            TextWriter? log = null)
            : this(
                  connection, 
                  isolation, 
                  null, 
                  converter, 
                  typeSystem,
                  log)
        {
        }

        /// <summary>
        /// True if a query or other action caused the connection to become open.
        /// </summary>
        protected bool ActionOpenedConnection => _actionOpenedConnection;

        /// <summary>
        /// Creates a new <see cref="DbQueryExecutor"/> with the <see cref="Converter"/> property assigned.
        /// </summary>
        public new DbQueryExecutor WithConverter(TypeConverter converter) =>
            (DbQueryExecutor)base.WithConverter(converter);

        /// <summary>
        /// Creates a new <see cref="QueryExecutor"/> with the <see cref="TypeSystem"/> property assigned.
        /// </summary>
        public new DbQueryExecutor WithTypeSystem(QueryTypeSystem typeSystem) =>
            (DbQueryExecutor)base.WithTypeSystem(typeSystem);

        /// <summary>
        /// Creates a new <see cref="DbQueryExecutor"/> with the <see cref="Log"/> property assigned.
        /// </summary>
        public new DbQueryExecutor WithLog(TextWriter? log) =>
            (DbQueryExecutor)base.WithLog(log);

        protected override QueryExecutor Construct(
            TypeConverter converter, 
            QueryTypeSystem typeSystem, 
            TextWriter? log)
        {
            return new DbQueryExecutor(
                this.Connection,
                this.Isolation,
                this.Transaction,
                converter,
                typeSystem,
                log
                );
        }

        /// <summary>
        /// Opens the connection if it is currently closed.
        /// </summary>
        protected void StartUsingConnection()
        {
            if (this.Connection.State == ConnectionState.Closed)
            {
                this.Connection.Open();
                _actionOpenedConnection = true;
            }

            _nConnectedActions++;
        }

        /// <summary>
        /// Closes the connection if no actions still require it.
        /// </summary>
        protected void StopUsingConnection()
        {
            System.Diagnostics.Debug.Assert(_nConnectedActions > 0);

            _nConnectedActions--;

            if (_nConnectedActions == 0 && _actionOpenedConnection)
            {
                this.Connection.Close();
                _actionOpenedConnection = false;
            }
        }

        /// <summary>
        /// Invokes the specified <see cref="Action"/> while the connection is open.
        /// </summary>
        public override void DoConnected(Action action)
        {
            this.StartUsingConnection();
            try
            {
                action();
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        /// <summary>
        /// Invokes the specified <see cref="Action"/> during a database transaction.
        /// If no transaction is currently associated with the <see cref="DbEntityProvider"/> a new
        /// one is started for the duration of the action. If the action completes without exception
        /// the transation is commited, otherwise it is aborted.
        /// </summary>
        public override void DoTransacted(Action action)
        {
            this.StartUsingConnection();
            try
            {
                if (this.Transaction == null)
                {
                    var trans = this.Connection.BeginTransaction(this.Isolation);
                    try
                    {
                        this.Transaction = trans;
                        action();
                        trans.Commit();
                    }
                    finally
                    {
                        this.Transaction = null;
                        trans.Dispose();
                    }
                }
                else
                {
                    action();
                }
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        /// <summary>
        /// Executes the command once and and projects the rows of the resulting rowset into a sequence of values.
        /// </summary>
        public override IEnumerable<T> Execute<T>(
            QueryCommand command, 
            Func<FieldReader, T> fnProjector, 
            MappingEntity entity, 
            object[] parameterValues)
        {
            this.LogCommand(command, parameterValues);

            var cap = this.GetCommandAndParameters(command);
            this.SetParameterValues(cap.Parameters, parameterValues);

            this.StartUsingConnection();
            try
            {
                var reader = this.ExecuteReader(cap.Command);
                var result = Project(reader, fnProjector, entity, true);

                if (_actionOpenedConnection)
                {
                    result = result.ToList();
                }
                else
                {
                    result = new EnumerateOnce<T>(result);
                }

                return result;
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        /// <summary>
        /// Executes the command over a series of parameter sets, and returns the total number of rows affected.
        /// </summary>
        public override IEnumerable<int> ExecuteBatch(
            QueryCommand query, 
            IEnumerable<object[]> parameterValueSets, 
            int batchSize, 
            bool stream)
        {
            this.StartUsingConnection();
            try
            {
                var result = this.ExecuteBatch(query, parameterValueSets);
                if (!stream || this.ActionOpenedConnection)
                {
                    return result.ToList();
                }
                else
                {
                    return new EnumerateOnce<int>(result);
                }
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        private IEnumerable<int> ExecuteBatch(
            QueryCommand query, 
            IEnumerable<object[]> parameterValueSets)
        {
            this.LogCommand(query, null);

            var cap = this.GetCommandAndParameters(query);

            foreach (var parameterValues in parameterValueSets)
            {
                this.LogParameters(query, parameterValues);
                this.LogMessage("");
                this.SetParameterValues(cap.Parameters, parameterValues);
                var _rowsAffected = cap.Command.ExecuteNonQuery();
                yield return _rowsAffected;
            }
        }

        /// <summary>
        /// Execute the same command over a series of parameter sets, and produces a sequence of values, once per execution.
        /// </summary>
        public override IEnumerable<T> ExecuteBatch<T>(
            QueryCommand query, 
            IEnumerable<object[]> paramSets, 
            Func<FieldReader, T> fnProjector, 
            MappingEntity entity, 
            int batchSize, 
            bool stream)
        {
            this.StartUsingConnection();
            try
            {
                var result = this.ExecuteBatch(query, paramSets, fnProjector, entity);
                if (!stream || this.ActionOpenedConnection)
                {
                    return result.ToList();
                }
                else
                {
                    return new EnumerateOnce<T>(result);
                }
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        private IEnumerable<T> ExecuteBatch<T>(
            QueryCommand query, 
            IEnumerable<object[]> parameterValueSets, 
            Func<FieldReader, T> fnProjector, 
            MappingEntity entity)
        {
            this.LogCommand(query, null);
            var cap = this.GetCommandAndParameters(query);
            cap.Command.Prepare();

            foreach (var parameterValues in parameterValueSets)
            {
                this.LogParameters(query, parameterValues);
                this.LogMessage("");
                this.SetParameterValues(cap.Parameters, parameterValues);
                var reader = this.ExecuteReader(cap.Command);
                var freader = new DbFieldReader(this.Converter, reader);
                try
                {
                    if (reader.Read())
                        yield return fnProjector(freader);
                }
                finally
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Produces an <see cref="IEnumerable{T}"/> that will execute the command when enumerated.
        /// </summary>
        public override IEnumerable<T> ExecuteDeferred<T>(
            QueryCommand query, 
            Func<FieldReader, T> fnProjector, 
            MappingEntity entity, 
            object[] parameterValues)
        {
            this.LogCommand(query, parameterValues);
            this.StartUsingConnection();
            try
            {
                var cap = this.GetCommandAndParameters(query);
                this.SetParameterValues(cap.Parameters, parameterValues);
                var reader = this.ExecuteReader(cap.Command);
                var freader = new DbFieldReader(this.Converter, reader);
                try
                {
                    while (reader.Read())
                    {
                        yield return fnProjector(freader);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        /// <summary>
        /// Execute a single command with the specified parameter values and return the number of rows affected.
        /// </summary>
        public override int ExecuteCommand(
            QueryCommand query, 
            object?[]? parameterValues = null)
        {
            this.LogCommand(query, parameterValues);
            this.StartUsingConnection();
            try
            {
                var cap = this.GetCommandAndParameters(query);
                if (parameterValues != null)
                    this.SetParameterValues(cap.Parameters, parameterValues);
                var rowsAffected = cap.Command.ExecuteNonQuery();
                return rowsAffected;
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        protected virtual IDataReader ExecuteReader(IDbCommand command)
        {
            return command.ExecuteReader();
        }

        protected virtual IEnumerable<T> Project<T>(
            IDataReader reader, 
            Func<FieldReader, T> fnProjector, 
            MappingEntity entity, 
            bool closeReader)
        {
            var freader = new DbFieldReader(this.Converter, reader);
            try
            {
                while (reader.Read())
                {
                    yield return fnProjector(freader);
                }
            }
            finally
            {
                if (closeReader)
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Get a <see cref="IDbCommand"/> initialized with the command-text and parameters
        /// </summary>
        protected virtual DbCommandAndParameters GetCommandAndParameters(QueryCommand query)
        {
            // create command object (and fill in parameters)
            var dbCommand = this.Connection.CreateCommand();
            dbCommand.CommandText = query.CommandText;
            if (this.Transaction != null)
                dbCommand.Transaction = this.Transaction;
            var dbParameters = this.CreateParameters(dbCommand, query.Parameters);
            return new DbCommandAndParameters(dbCommand, dbParameters);
        }

        protected virtual IReadOnlyList<IDbDataParameter> CreateParameters(
            IDbCommand command,
            IReadOnlyList<QueryParameter> queryParameters)
        {
            return queryParameters
                .Select(qp => CreateParameter(command, qp))
                .ToReadOnly();
        }

        protected virtual IDbDataParameter CreateParameter(
            IDbCommand command, 
            QueryParameter queryParameter)
        {
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = queryParameter.Name;

            var qt = queryParameter.QueryType
                ?? this.TypeSystem.GetQueryType(queryParameter.Type);

            if (qt.TryGetDbType(out var dbType))
                dbParam.DbType = dbType;
            if (qt.Precision != 0)
                dbParam.Precision = (byte)qt.Precision;
            if (qt.Scale != 0)
                dbParam.Scale = (byte)qt.Scale;
            if (qt.Length != 0)
                dbParam.Size = qt.Length;

            return dbParam;
        }

        /// <summary>
        /// Sets the parameter values.
        /// </summary>
        protected virtual void SetParameterValues(
            IReadOnlyList<IDbDataParameter> parameters,
            object?[] values)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (i < values.Length)
                {
                    var p = parameters[i];
                    var v = values[i];
                    if (p.Direction == ParameterDirection.Input
                     || p.Direction == ParameterDirection.InputOutput)
                    {
                        p.Value = v ?? DBNull.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Assigns output parameter values into the values array.
        /// </summary>
        protected virtual void GetParameterValues(
            IReadOnlyList<IDbDataParameter> parameters,
            object?[] values)
        {
            for (int i = 0, n = parameters.Count; i < n; i++)
            {
                if (i < values.Length)
                {
                    var p = parameters[i];
                    if (p.Direction != ParameterDirection.Input)
                    {
                        var value = p.Value;
                        if (value == DBNull.Value)
                            value = null;
                        values[i] = value;
                    }
                }
            }
        }

        protected virtual void LogMessage(string message)
        {
            if (this.Log != null)
            {
                this.Log.WriteLine(message);
            }
        }

        /// <summary>
        /// Write a command and parameters to the log
        /// </summary>
        /// <param name="command"></param>
        /// <param name="paramValues"></param>
        protected virtual void LogCommand(QueryCommand command, object?[]? paramValues)
        {
            if (this.Log != null)
            {
                this.Log.WriteLine(command.CommandText);
                
                if (paramValues != null)
                {
                    this.LogParameters(command, paramValues);
                }

                this.Log.WriteLine();
            }
        }

        protected virtual void LogParameters(QueryCommand command, object?[] paramValues)
        {
            if (this.Log != null && paramValues != null)
            {
                for (int i = 0, n = command.Parameters.Count; i < n; i++)
                {
                    var p = command.Parameters[i];
                    var v = paramValues[i];

                    if (v == null || v == DBNull.Value)
                    {
                        this.Log.WriteLine("-- {0} = NULL", p.Name);
                    }
                    else
                    {
                        this.Log.WriteLine("-- {0} = [{1}]", p.Name, v);
                    }
                }
            }
        }
    }

    public struct DbCommandAndParameters
    {
        public IDbCommand Command { get; }
        public IReadOnlyList<IDbDataParameter> Parameters { get; }

        public DbCommandAndParameters(
            IDbCommand command,
            IEnumerable<IDbDataParameter> parameters)
        {
            this.Command = command;
            this.Parameters = parameters.ToReadOnly();
        }
    }
}