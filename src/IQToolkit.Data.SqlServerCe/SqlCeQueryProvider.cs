// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;

namespace IQToolkit.Data.SqlServerCe
{
    using IQToolkit.Data.Common;

    /// <summary>
    /// A <see cref="DbEntityProvider"/> for Microsoft SQL Server Compact Edition databases
    /// </summary>
    public class SqlCeQueryProvider : DbEntityProvider
    {
        /// <summary>
        /// Constructs a <see cref="SqlCeQueryProvider"/>
        /// </summary>
        public SqlCeQueryProvider(SqlCeConnection connection, QueryMapping mapping = null, QueryPolicy policy = null)
            : base(connection, SqlCeLanguage.Default, mapping, policy)
        {
        }

        /// <summary>
        /// Constructs a <see cref="SqlCeQueryProvider"/>
        /// </summary>
        public SqlCeQueryProvider(string connectionStringOrDatabaseFile, QueryMapping mapping = null, QueryPolicy policy = null)
            : this(CreateConnection(connectionStringOrDatabaseFile), mapping, policy)
        {
        }

        protected override DbEntityProvider New(DbConnection connection, QueryMapping mapping, QueryPolicy policy)
        {
            return new SqlCeQueryProvider((SqlCeConnection)connection, mapping, policy);
        }

        /// <summary>
        /// Creates a <see cref="SqlCeConnection"/> for the corresponding connection string or database file.
        /// </summary>
        public static SqlCeConnection CreateConnection(string connectionStringOrDatabaseFile)
        {
            if (!connectionStringOrDatabaseFile.Contains('='))
            {
                connectionStringOrDatabaseFile = GetConnectionString(connectionStringOrDatabaseFile);
            }

            return new SqlCeConnection(connectionStringOrDatabaseFile);
        }

        /// <summary>
        /// Gets a connection string that will access the specified database file.
        /// </summary>
        public static string GetConnectionString(string databaseFile)
        {
            databaseFile = Path.GetFullPath(databaseFile);
            return string.Format(@"Data Source='{0}'", databaseFile);
        }

        protected override QueryExecutor CreateExecutor()
        {
            return new Executor(this);
        }

        new class Executor : DbEntityProvider.Executor
        {
            public Executor(SqlCeQueryProvider provider)
                : base(provider)
            {
            }

            protected override void AddParameter(DbCommand command, QueryParameter parameter, object value)
            {
                SqlQueryType sqlType = (SqlQueryType)parameter.QueryType;
                if (sqlType == null)
                {
                    sqlType = (SqlQueryType)this.Provider.Language.TypeSystem.GetColumnType(parameter.Type);
                }

                var p = ((SqlCeCommand)command).Parameters.Add("@" + parameter.Name, (System.Data.SqlDbType)(int)sqlType.SqlType, sqlType.Length);
                if (sqlType.Precision != 0)
                {
                    p.Precision = (byte)sqlType.Precision;
                }

                if (sqlType.Scale != 0)
                {
                    p.Scale = (byte)sqlType.Scale;
                }

                p.Value = value ?? DBNull.Value;
            }
        }
    }
}