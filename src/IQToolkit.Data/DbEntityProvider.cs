// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Data
{
    using Common;
    using Mapping;

    /// <summary>
    /// The base type for <see cref="EntityProvider"/>'s that use a System.Data.<see cref="DbConnection"/>.
    /// </summary>
    public abstract class DbEntityProvider : EntityProvider
    {
        private readonly DbConnection connection;
        private DbTransaction transaction;
        private IsolationLevel isolation = IsolationLevel.ReadCommitted;

        private int nConnectedActions = 0;
        private bool actionOpenedConnection = false;

        /// <summary>
        /// Constructs a new <see cref="DbEntityProvider"/>
        /// </summary>
        protected DbEntityProvider(DbConnection connection, QueryLanguage language, QueryMapping mapping, QueryPolicy policy)
            : base(language, mapping ?? new AttributeMapping(), policy ?? QueryPolicy.Default)
        {
            if (connection == null)
                throw new InvalidOperationException("Connection not specified");
            this.connection = connection;
        }

        /// <summary>
        /// The <see cref="DbConnection"/> used for executing queries.
        /// </summary>
        public virtual DbConnection Connection
        {
            get { return this.connection; }
        }

        /// <summary>
        /// The <see cref="DbTransaction"/> to use for updates.
        /// </summary>
        public virtual DbTransaction Transaction
        {
            get
            {
                return this.transaction;
            }

            set
            {
                if (value != null && value.Connection != this.connection)
                    throw new InvalidOperationException("Transaction does not match connection.");
                this.transaction = value;
            }
        }

        /// <summary>
        /// The <see cref="System.Data.IsolationLevel"/> used for transactions.
        /// </summary>
        public IsolationLevel Isolation
        {
            get { return this.isolation; }
            set { this.isolation = value; }
        }

        protected virtual DbEntityProvider New(DbConnection connection, QueryMapping mapping, QueryPolicy policy)
        {
            return (DbEntityProvider)Activator.CreateInstance(this.GetType(), new object[] { connection, mapping, policy });
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DbEntityProvider"/> that uses the specified <see cref="QueryMapping"/>.
        /// </summary>
        public DbEntityProvider WithMapping(QueryMapping mapping)
        {
            var n = New(this.Connection, mapping, this.Policy);
            n.Log = this.Log;
            return n;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DbEntityProvider"/> that uses the mapping specified by the mapping string.
        /// </summary>
        /// <param name="mapping">This is typically a name of a context type with <see cref="MappingAttribute"/>'s or a path to a mapping file, 
        /// but may have custom meaning for the provider.</param>
        /// <returns></returns>
        public DbEntityProvider WithMapping(string mapping)
        {
            return WithMapping(CreateMapping(mapping));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DbEntityProvider"/> that uses the specified <see cref="QueryPolicy"/>.
        /// </summary>
        public DbEntityProvider WithPolicy(QueryPolicy policy)
        {
            var n = New(this.Connection, this.Mapping, policy);
            n.Log = this.Log;
            return n;
        }

        /// <summary>
        /// Create a new <see cref="DbEntityProvider"/> from the specified <see cref="EntityProviderSettings"/>.
        /// </summary>
        public static DbEntityProvider FromSettings(EntityProviderSettings settings = null)
        {
            settings = settings ?? EntityProviderSettings.FromApplicationSettings();

            DbEntityProvider provider = settings.Provider != null
                ? From(settings.Provider, settings.Connection)
                : From(settings.Connection);

            if (settings.Mapping != null)
            {
                provider = provider.WithMapping(settings.Mapping);
            }

            return provider;
        }
        
        /// <summary>
        /// Create a new <see cref="DbEntityProvider"/>.
        /// </summary>
        /// <param name="connectionStringOrDatabaseFile">The ADO provider connection string or file path for a database file.</param>
        public static DbEntityProvider From(string connectionStringOrDatabaseFile)
        {
            return From(InferProviderName(connectionStringOrDatabaseFile), connectionStringOrDatabaseFile);
        }

        /// <summary>
        /// Create a new <see cref="DbEntityProvider"/>.
        /// </summary>
        /// <param name="providerName">The type name of the query provider (typically the name of the assembly it is defined in, not the class name).</param>
        /// <param name="connectionStringOrDatabaseFile">The ADO provider connection string or file path to a database file.</param>
        public static DbEntityProvider From(string providerName, string connectionStringOrDatabaseFile)
        {
            if (providerName == null)
                throw new ArgumentNullException(nameof(providerName));
            if (connectionStringOrDatabaseFile == null)
                throw new ArgumentNullException(nameof(connectionStringOrDatabaseFile));

            Type providerType = GetProviderType(providerName);
            if (providerType == null)
                throw new InvalidOperationException(string.Format("Unable to find query provider '{0}'", providerName));

            return From(providerType, connectionStringOrDatabaseFile);
        }

        /// <summary>
        /// Infers the known query provider by examining the connection string.
        /// </summary>
        private static string InferProviderName(string connectionString)
        {
            var clower = connectionString.ToLower();

            // try sniffing connection to figure out provider
            if (clower.Contains(".mdb") || clower.Contains(".accdb"))
            {
                return "IQToolkit.Data.Access";
            }
            else if (clower.Contains(".sdf"))
            {
                return "IQToolkit.Data.SqlServerCe";
            }
            else if (clower.Contains(".sl3") || clower.Contains(".db3"))
            {
                return "IQToolkit.Data.SQLite";
            }
            else if (clower.Contains(".mdf"))
            {
                return "IQToolkit.Data.SqlClient";
            }
            else
            {
                throw new InvalidOperationException(string.Format("Query provider not specified and cannot be inferred."));
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="DbEntityProvider"/>.
        /// </summary>
        /// <param name="queryProviderType">The query provider type.</param>
        /// <param name="connectionStringOrDatabaseFile">The ADO provider connection string or file path for a database file.</param>
        public static DbEntityProvider From(Type queryProviderType, string connectionStringOrDatabaseFile)
        {
            if (queryProviderType == null)
                throw new ArgumentNullException(nameof(queryProviderType));
            if (connectionStringOrDatabaseFile == null)
                throw new ArgumentNullException(nameof(connectionStringOrDatabaseFile));

            Type adoConnectionType = GetAdoConnectionType(queryProviderType);
            if (adoConnectionType == null)
                throw new InvalidOperationException(string.Format("Unable to deduce DbConnection type for '{0}'", queryProviderType.Name));

            DbConnection connection = (DbConnection)Activator.CreateInstance(adoConnectionType);

            // is the connection string just a filename?
            if (!connectionStringOrDatabaseFile.Contains('='))
            {
                MethodInfo gcs = queryProviderType.GetMethod("GetConnectionString", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
                if (gcs != null)
                {
                    var getConnectionString = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), gcs);
                    connectionStringOrDatabaseFile = getConnectionString(connectionStringOrDatabaseFile);
                }
            }

            connection.ConnectionString = connectionStringOrDatabaseFile;

            return (DbEntityProvider)Activator.CreateInstance(queryProviderType, new object[] { connection, null, null });
        }

        private static Type GetAdoConnectionType(Type providerType)
        {
            // sniff constructors 
            foreach (var con in providerType.GetConstructors())
            {
                foreach (var arg in con.GetParameters())
                {
                    if (arg.ParameterType.IsSubclassOf(typeof(DbConnection)))
                        return arg.ParameterType;
                }
            }
            return null;
        }

        protected bool ActionOpenedConnection
        {
            get { return this.actionOpenedConnection; }
        }

        protected void StartUsingConnection()
        {
            if (this.connection.State == ConnectionState.Closed)
            {
                this.connection.Open();
                this.actionOpenedConnection = true;
            }
            this.nConnectedActions++;
        }

        protected void StopUsingConnection()
        {
            System.Diagnostics.Debug.Assert(this.nConnectedActions > 0);
            this.nConnectedActions--;
            if (this.nConnectedActions == 0 && this.actionOpenedConnection)
            {
                this.connection.Close();
                this.actionOpenedConnection = false;
            }
        }

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

        public override int ExecuteCommand(string commandText)
        {
            if (this.Log != null)
            {
                this.Log.WriteLine(commandText);
            }
            this.StartUsingConnection();
            try
            {
                DbCommand cmd = this.Connection.CreateCommand();
                cmd.CommandText = commandText;
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        protected override QueryExecutor CreateExecutor()
        {
            return new Executor(this);
        }

        public class Executor : QueryExecutor
        {
            DbEntityProvider provider;
            int rowsAffected;

            public Executor(DbEntityProvider provider)
            {
                this.provider = provider;
            }

            public DbEntityProvider Provider
            {
                get { return this.provider; }
            }

            public override int RowsAffected
            {
                get { return this.rowsAffected; }
            }

            protected virtual bool BufferResultRows
            {
                get { return false; }
            }

            protected bool ActionOpenedConnection
            {
                get { return this.provider.actionOpenedConnection; }
            }

            protected void StartUsingConnection()
            {
                this.provider.StartUsingConnection();
            }

            protected void StopUsingConnection()
            {
                this.provider.StopUsingConnection();
            }

            public override object Convert(object value, Type type)
            {
                if (value == null)
                {
                    return TypeHelper.GetDefault(type);
                }
                type = TypeHelper.GetNonNullableType(type);
                Type vtype = value.GetType();
                if (type != vtype)
                {
                    if (type.IsEnum)
                    {
                        if (vtype == typeof(string))
                        {
                            return Enum.Parse(type, (string)value);
                        }
                        else
                        {
                            Type utype = Enum.GetUnderlyingType(type);
                            if (utype != vtype)
                            {
                                value = System.Convert.ChangeType(value, utype);
                            }
                            return Enum.ToObject(type, value);
                        }
                    }
                    return System.Convert.ChangeType(value, type);
                }
                return value;
            }

            public override IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
            {
                this.LogCommand(command, paramValues);
                this.StartUsingConnection();
                try
                {
                    DbCommand cmd = this.GetCommand(command, paramValues);
                    DbDataReader reader = this.ExecuteReader(cmd);
                    var result = Project(reader, fnProjector, entity, true);
                    if (this.provider.ActionOpenedConnection)
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

            protected virtual DbDataReader ExecuteReader(DbCommand command)
            {
                var reader = command.ExecuteReader();
                if (this.BufferResultRows)
                {
                    // use data table to buffer results
                    var ds = new DataSet();
                    ds.EnforceConstraints = false;
                    var table = new DataTable();
                    ds.Tables.Add(table);
                    ds.EnforceConstraints = false;
                    table.Load(reader);
                    reader = table.CreateDataReader();
                }
                return reader;
            }

            protected virtual IEnumerable<T> Project<T>(DbDataReader reader, Func<FieldReader, T> fnProjector, MappingEntity entity, bool closeReader)
            {
                var freader = new DbFieldReader(this, reader);
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

            public override int ExecuteCommand(QueryCommand query, object[] paramValues)
            {
                this.LogCommand(query, paramValues);
                this.StartUsingConnection();
                try
                {
                    DbCommand cmd = this.GetCommand(query, paramValues);
                    this.rowsAffected = cmd.ExecuteNonQuery();
                    return this.rowsAffected;
                }
                finally
                {
                    this.StopUsingConnection();
                }
            }

            public override IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream)
            {
                this.StartUsingConnection();
                try
                {
                    var result = this.ExecuteBatch(query, paramSets);
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

            private IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets)
            {
                this.LogCommand(query, null);
                DbCommand cmd = this.GetCommand(query, null);
                foreach (var paramValues in paramSets)
                {
                    this.LogParameters(query, paramValues);
                    this.LogMessage("");
                    this.SetParameterValues(query, cmd, paramValues);
                    this.rowsAffected = cmd.ExecuteNonQuery();
                    yield return this.rowsAffected;
                }
            }

            public override IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<FieldReader, T> fnProjector, MappingEntity entity, int batchSize, bool stream)
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

            private IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<FieldReader, T> fnProjector, MappingEntity entity)
            {
                this.LogCommand(query, null);
                DbCommand cmd = this.GetCommand(query, null);
                cmd.Prepare();
                foreach (var paramValues in paramSets)
                {
                    this.LogParameters(query, paramValues);
                    this.LogMessage("");
                    this.SetParameterValues(query, cmd, paramValues);
                    var reader = this.ExecuteReader(cmd);
                    var freader = new DbFieldReader(this, reader);
                    try
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            yield return fnProjector(freader);
                        }
                        else
                        {
                            yield return default(T);
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }

            public override IEnumerable<T> ExecuteDeferred<T>(QueryCommand query, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
            {
                this.LogCommand(query, paramValues);
                this.StartUsingConnection();
                try
                {
                    DbCommand cmd = this.GetCommand(query, paramValues);
                    var reader = this.ExecuteReader(cmd);
                    var freader = new DbFieldReader(this, reader);
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
            /// Get an ADO command object initialized with the command-text and parameters
            /// </summary>
            protected virtual DbCommand GetCommand(QueryCommand query, object[] paramValues)
            {
                // create command object (and fill in parameters)
                DbCommand cmd = this.provider.Connection.CreateCommand();
                cmd.CommandText = query.CommandText;
                if (this.provider.Transaction != null)
                    cmd.Transaction = this.provider.Transaction;
                this.SetParameterValues(query, cmd, paramValues);
                return cmd;
            }

            protected virtual void SetParameterValues(QueryCommand query, DbCommand command, object[] paramValues)
            {
                if (query.Parameters.Count > 0 && command.Parameters.Count == 0)
                {
                    for (int i = 0, n = query.Parameters.Count; i < n; i++)
                    {
                        this.AddParameter(command, query.Parameters[i], paramValues != null ? paramValues[i] : null);
                    }
                }
                else if (paramValues != null)
                {
                    for (int i = 0, n = command.Parameters.Count; i < n; i++)
                    {
                        DbParameter p = command.Parameters[i];
                        if (p.Direction == System.Data.ParameterDirection.Input
                         || p.Direction == System.Data.ParameterDirection.InputOutput)
                        {
                            p.Value = paramValues[i] ?? DBNull.Value;
                        }
                    }
                }
            }

            protected virtual void AddParameter(DbCommand command, QueryParameter parameter, object value)
            {
                DbParameter p = command.CreateParameter();
                p.ParameterName = parameter.Name;
                p.Value = value ?? DBNull.Value;
                command.Parameters.Add(p);
            }

            protected virtual void GetParameterValues(DbCommand command, object[] paramValues)
            {
                if (paramValues != null)
                {
                    for (int i = 0, n = command.Parameters.Count; i < n; i++)
                    {
                        if (command.Parameters[i].Direction != System.Data.ParameterDirection.Input)
                        {
                            object value = command.Parameters[i].Value;
                            if (value == DBNull.Value)
                                value = null;
                            paramValues[i] = value;
                        }
                    }
                }
            }

            protected virtual void LogMessage(string message)
            {
                if (this.provider.Log != null)
                {
                    this.provider.Log.WriteLine(message);
                }
            }

            /// <summary>
            /// Write a command and parameters to the log
            /// </summary>
            /// <param name="command"></param>
            /// <param name="paramValues"></param>
            protected virtual void LogCommand(QueryCommand command, object[] paramValues)
            {
                if (this.provider.Log != null)
                {
                    this.provider.Log.WriteLine(command.CommandText);
                    if (paramValues != null)
                    {
                        this.LogParameters(command, paramValues);
                    }
                    this.provider.Log.WriteLine();
                }
            }

            protected virtual void LogParameters(QueryCommand command, object[] paramValues)
            {
                if (this.provider.Log != null && paramValues != null)
                {
                    for (int i = 0, n = command.Parameters.Count; i < n; i++)
                    {
                        var p = command.Parameters[i];
                        var v = paramValues[i];

                        if (v == null || v == DBNull.Value)
                        {
                            this.provider.Log.WriteLine("-- {0} = NULL", p.Name);
                        }
                        else
                        {
                            this.provider.Log.WriteLine("-- {0} = [{1}]", p.Name, v);
                        }
                    }
                }
            }
        }
    }
}
