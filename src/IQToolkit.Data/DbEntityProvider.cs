// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data
{
    using Common;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class DbEntityProvider : EntityProvider
    {
        DbConnection connection;
        DbTransaction transaction;
        IsolationLevel isolation = IsolationLevel.ReadCommitted;

        int nConnectedActions = 0;
        bool actionOpenedConnection = false;

        public DbEntityProvider(DbConnection connection, QueryLanguage language, QueryMapping mapping, QueryPolicy policy)
            : base(language, mapping, policy)
        {
            if (connection == null)
                throw new InvalidOperationException("Connection not specified");
            this.connection = connection;
        }

        public virtual DbConnection Connection
        {
            get { return this.connection; }
        }

        public virtual DbTransaction Transaction
        {
            get { return this.transaction; }
            set
            {
                if (value != null && value.Connection != this.connection)
                    throw new InvalidOperationException("Transaction does not match connection.");
                this.transaction = value;
            }
        }

        public IsolationLevel Isolation
        {
            get { return this.isolation; }
            set { this.isolation = value; }
        }

        public virtual DbEntityProvider New(DbConnection connection, QueryMapping mapping, QueryPolicy policy)
        {
            return (DbEntityProvider)Activator.CreateInstance(this.GetType(), new object[] { connection, mapping, policy });
        }

        public virtual DbEntityProvider New(DbConnection connection)
        {
            var n = New(connection, this.Mapping, this.Policy);
            n.Log = this.Log;
            return n;
        }

        public virtual DbEntityProvider New(QueryMapping mapping)
        {
            var n = New(this.Connection, mapping, this.Policy);
            n.Log = this.Log;
            return n;
        }

        public virtual DbEntityProvider New(QueryPolicy policy)
        {
            var n = New(this.Connection, this.Mapping, policy);
            n.Log = this.Log;
            return n;
        }

        public static DbEntityProvider FromApplicationSettings()
        {
            var provider = System.Configuration.ConfigurationManager.AppSettings["Provider"];
            var connection = System.Configuration.ConfigurationManager.AppSettings["Connection"];
            var mapping = System.Configuration.ConfigurationManager.AppSettings["Mapping"];
            return From(provider, connection, mapping);
        }

        public static DbEntityProvider From(string connectionString, string mappingId)
        {
            return From(connectionString, mappingId, QueryPolicy.Default);
        }

        public static DbEntityProvider From(string connectionString, string mappingId, QueryPolicy policy)
        {
            return From(null, connectionString, mappingId, policy);
        }

        public static DbEntityProvider From(string connectionString, QueryMapping mapping, QueryPolicy policy)
        {
            return From((string)null, connectionString, mapping, policy);
        }

        public static DbEntityProvider From(string provider, string connectionString, string mappingId)
        {
            return From(provider, connectionString, mappingId, QueryPolicy.Default);
        }

        public static DbEntityProvider From(string provider, string connectionString, string mappingId, QueryPolicy policy)
        {
            return From(provider, connectionString, GetMapping(mappingId), policy);
        }

        public static DbEntityProvider From(string provider, string connectionString, QueryMapping mapping, QueryPolicy policy)
        {
            if (provider == null)
            {
                var clower = connectionString.ToLower();
                // try sniffing connection to figure out provider
                if (clower.Contains(".mdb") || clower.Contains(".accdb"))
                {
                    provider = "IQToolkit.Data.Access";
                }
                else if (clower.Contains(".sdf"))
                {
                    provider = "IQToolkit.Data.SqlServerCe";
                }
                else if (clower.Contains(".sl3") || clower.Contains(".db3"))
                {
                    provider = "IQToolkit.Data.SQLite";
                }
                else if (clower.Contains(".mdf"))
                {
                    provider = "IQToolkit.Data.SqlClient";
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Query provider not specified and cannot be inferred."));
                }
            }

            Type providerType = GetProviderType(provider);
            if (providerType == null)
                throw new InvalidOperationException(string.Format("Unable to find query provider '{0}'", provider));

            return From(providerType, connectionString, mapping, policy);
        }

        public static DbEntityProvider From(Type providerType, string connectionString, QueryMapping mapping, QueryPolicy policy)
        {
            Type adoConnectionType = GetAdoConnectionType(providerType);
            if (adoConnectionType == null)
                throw new InvalidOperationException(string.Format("Unable to deduce ADO provider for '{0}'", providerType.Name));
            DbConnection connection = (DbConnection)Activator.CreateInstance(adoConnectionType);

            // is the connection string just a filename?
            if (!connectionString.Contains('='))
            {
                MethodInfo gcs = providerType.GetMethod("GetConnectionString", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
                if (gcs != null)
                {
                    var getConnectionString = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), gcs);
                    connectionString = getConnectionString(connectionString);
                }
            }

            connection.ConnectionString = connectionString;

            return (DbEntityProvider)Activator.CreateInstance(providerType, new object[] { connection, mapping, policy });
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

        public override async Task DoConnectedAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            this.StartUsingConnection();
            try
            {
                await action(cancellationToken).ConfigureAwait(false);
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

        public override async Task DoTransactedAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
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
                        await action(cancellationToken).ConfigureAwait(false);
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
                    await action(cancellationToken).ConfigureAwait(false);
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

        public override async Task<int> ExecuteCommandAsync(string commandText, CancellationToken cancellationToken)
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
                return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                this.StopUsingConnection();
            }
        }

        protected override QueryExecutor CreateExecutor()
        {
            return new DbQueryExecutor(this);
        }
    }
}
