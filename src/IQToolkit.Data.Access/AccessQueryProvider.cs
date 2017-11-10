// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;

namespace IQToolkit.Data.Access
{
    using IQToolkit.Data.Common;
    using IQToolkit.Data.OleDb;

    /// <summary>
    /// A <see cref="DbEntityProvider"/> for Microsoft Access databases
    /// </summary>
    public class AccessQueryProvider : OleDb.OleDbQueryProvider
    {
        /// <summary>
        /// Construct a <see cref="AccessQueryProvider"/>
        /// </summary>
        public AccessQueryProvider(OleDbConnection connection, QueryMapping mapping = null, QueryPolicy policy = null)
            : base(connection, AccessLanguage.Default, mapping, policy)
        {
        }

        /// <summary>
        /// Constructs a <see cref="AccessQueryProvider"/>
        /// </summary>
        public AccessQueryProvider(string connectionStringOrDatabaseFile, QueryMapping mapping = null, QueryPolicy policy = null)
            : this(CreateConnection(connectionStringOrDatabaseFile), mapping, policy)
        {
        }

        protected override DbEntityProvider New(DbConnection connection, QueryMapping mapping, QueryPolicy policy)
        {
            return new AccessQueryProvider((OleDbConnection)connection, mapping, policy);
        }

        /// <summary>
        /// Creates a <see cref="OleDbConnection"/> given a connection string or database file.
        /// </summary>
        public static OleDbConnection CreateConnection(string connectionStringOrDatabaseFile)
        {
            if (!connectionStringOrDatabaseFile.Contains("="))
            {
                connectionStringOrDatabaseFile = GetConnectionString(connectionStringOrDatabaseFile);
            }

            return new OleDbConnection(connectionStringOrDatabaseFile);
        }

        /// <summary>
        /// Gets a connection string appropriate for openning the specified dadtabase file.
        /// </summary>
        public static string GetConnectionString(string databaseFile) 
        {
            databaseFile = Path.GetFullPath(databaseFile);
            string dbLower = databaseFile.ToLower();
            if (dbLower.Contains(".mdb"))
            {
                return GetConnectionString(AccessOleDbProvider2000, databaseFile);
            }
            else if (dbLower.Contains(".accdb"))
            {
                return GetConnectionString(AccessOleDbProvider2007, databaseFile);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Unrecognized file extension on database file '{0}'", databaseFile));
            }
        }

        private static string GetConnectionString(string provider, string databaseFile)
        {
            return string.Format("Provider={0};ole db services=0;Data Source={1}", provider, databaseFile);
        }

        public static readonly string AccessOleDbProvider2000 = "Microsoft.Jet.OLEDB.4.0";
        public static readonly string AccessOleDbProvider2007 = "Microsoft.ACE.OLEDB.12.0";

        protected override QueryExecutor CreateExecutor()
        {
            return new Executor(this);
        }

        public new class Executor : OleDbQueryProvider.Executor
        {
            AccessQueryProvider provider;

            public Executor(AccessQueryProvider provider)
                : base(provider)
            {
                this.provider = provider;
            }

            protected override DbCommand GetCommand(QueryCommand query, object[] paramValues)
            {
                var cmd = (OleDbCommand)this.provider.Connection.CreateCommand();
                cmd.CommandText = query.CommandText;
                
                this.SetParameterValues(query, cmd, paramValues);
                
                if (this.provider.Transaction != null)
                {
                    cmd.Transaction = (OleDbTransaction)this.provider.Transaction;
                }

                return cmd;
            }

            protected override OleDbType GetOleDbType(QueryType type)
            {
                SqlQueryType sqlType = type as SqlQueryType;
                if (sqlType != null)
                {
                    return ToOleDbType(sqlType.SqlType);
                }

                return base.GetOleDbType(type);
            }
        }
    }
}