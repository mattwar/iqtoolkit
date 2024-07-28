// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data.OleDb;

namespace IQToolkit.Data.Access
{
    /// <summary>
    /// A <see cref="DbEntityProvider"/> for Microsoft Access databases
    /// </summary>
    public class AccessQueryProvider : OleDbQueryProvider
    {
        /// <summary>
        /// Construct a <see cref="AccessQueryProvider"/>
        /// </summary>
        private AccessQueryProvider(
            QueryExecutor executor,
            QueryLanguage? language,
            QueryMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache)
            : base(
                  executor,
                  language ?? AccessLanguage.Default,
                  mapping,
                  policy,
                  log,
                  cache)
        {
        }

        /// <summary>
        /// Construct a <see cref="AccessQueryProvider"/>
        /// </summary>
        public AccessQueryProvider(
            QueryExecutor executor)
            : this(
                  executor,
                  language: null, 
                  mapping: null,
                  policy: null, 
                  log: null, 
                  cache: null)
        {
        }

        /// <summary>
        /// Construct a <see cref="AccessQueryProvider"/>
        /// </summary>
        public AccessQueryProvider(
            OleDbConnection connection)
            : this(new OleDbQueryExecutor(connection, language: AccessLanguage.Default))
        {
        }

        /// <summary>
        /// Construct a <see cref="AccessQueryProvider"/>
        /// </summary>
        public AccessQueryProvider(
            string connectionStringOrFile)
            : this(CreateConnection(connectionStringOrFile))
        {
        }

        public new AccessQueryProvider WithExecutor(QueryExecutor executor) =>
            (AccessQueryProvider)With(executor: executor);

        public new AccessQueryProvider WithLanguage(QueryLanguage language) =>
            (AccessQueryProvider)With(language: language);

        public new AccessQueryProvider WithMapping(QueryMapping mapping) =>
            (AccessQueryProvider)With(mapping: mapping);

        public new AccessQueryProvider WithPolicy(QueryPolicy policy) =>
            (AccessQueryProvider)With(policy: policy);

        public new AccessQueryProvider WithLog(TextWriter? log) =>
            (AccessQueryProvider)With(log: log);

        public new AccessQueryProvider WithCache(QueryCache? cache) =>
            (AccessQueryProvider)With(cache: cache);

        protected override EntityProvider Construct(
            QueryExecutor executor,
            QueryLanguage language,
            QueryMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache)
        {
            return new AccessQueryProvider(executor, language, mapping, policy, log, cache);
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
    }
}