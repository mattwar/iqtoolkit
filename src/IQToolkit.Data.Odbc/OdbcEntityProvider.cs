// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;
using System.Data.Odbc;
using System.IO;

namespace IQToolkit.Data.Odbc
{
    /// <summary>
    /// A base <see cref="DbEntityProvider"/> for OLEDB database providers
    /// </summary>
    public class OdbcEntityProvider : DbEntityProvider
    {
        protected OdbcEntityProvider(
            OdbcQueryExecutor executor,
            QueryLanguage? language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache)
            : base(
                  executor,
                  language,
                  mapping,
                  policy,
                  log,
                  cache)
        {
        }

        public OdbcEntityProvider(
            OdbcQueryExecutor executor,
            QueryLanguage? language = null)
            : this(executor, language, null, null, null, null)
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

        protected override EntityProvider Construct(
            QueryExecutor executor,
            QueryLanguage language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache)
        {
            return new OdbcEntityProvider(
                (OdbcQueryExecutor)executor, 
                language, 
                mapping, 
                policy, 
                log, 
                cache
                );
        }

        protected override FormattingOptions FormattingOptions { get; } =
            FormattingOptions.Default.WithIsOdbc(true);
    }
}