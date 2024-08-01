// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data.Odbc;
using System.IO;

namespace IQToolkit.Odbc
{
    using Entities;
    using Entities.Data;

    /// <summary>
    /// A entity provider for ODBC drivers.
    /// </summary>
    public class OdbcEntityProvider : DbEntityProvider
    {
        protected OdbcEntityProvider(
            OdbcQueryExecutor executor,
            QueryLanguage? language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache,
            QueryOptions? options)
            : base(
                  executor,
                  language,
                  mapping,
                  policy,
                  log,
                  cache,
                  (options ?? QueryOptions.Default).WithIsOdbc(true))
        {
        }

        public OdbcEntityProvider(
            OdbcQueryExecutor executor,
            QueryLanguage? language = null)
            : this(executor, language, null, null, null, null, null)
        {
        }

        public OdbcEntityProvider(
            OdbcConnection connection,
            QueryLanguage? language = null)
            : this(new OdbcQueryExecutor(connection), language)
        {
        }

        public new OdbcQueryExecutor Executor =>
            (OdbcQueryExecutor)base.Executor;

        public new OdbcEntityProvider WithExecutor(QueryExecutor executor) =>
            (OdbcEntityProvider)With(executor: executor);

        public new OdbcEntityProvider WithLanguage(QueryLanguage language) =>
            (OdbcEntityProvider)With(language: language);

        public new OdbcEntityProvider WithMapping(EntityMapping mapping) =>
            (OdbcEntityProvider)With(mapping: mapping);

        public new OdbcEntityProvider WithPolicy(QueryPolicy policy) =>
            (OdbcEntityProvider)With(policy: policy);

        public new OdbcEntityProvider WithLog(TextWriter? log) =>
            (OdbcEntityProvider)With(log: log);

        public new OdbcEntityProvider WithCache(QueryCache? cache) =>
            (OdbcEntityProvider)With(cache: cache);

        public new OdbcEntityProvider WithOptions(QueryOptions options) =>
            (OdbcEntityProvider)With(options: options);

        protected override EntityProvider Construct(
            QueryExecutor executor,
            QueryLanguage language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache,
            QueryOptions? options)
        {
            return new OdbcEntityProvider(
                (OdbcQueryExecutor)executor, 
                language, 
                mapping, 
                policy, 
                log, 
                cache,
                options
                );
        }
    }
}