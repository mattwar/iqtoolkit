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
    public class OdbcQueryProvider : EntityProvider
    {
        protected OdbcQueryProvider(
            QueryExecutor executor,
            QueryLanguage? language,
            QueryMapping? mapping,
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

        public OdbcQueryProvider(
            QueryExecutor executor,
            QueryLanguage? language = null)
            : this(executor, language, null, null, null, null)
        {
        }

        public OdbcQueryProvider(
            OdbcConnection connection,
            QueryLanguage? language = null)
            : this(new OdbcQueryExecutor(connection), language)
        {
        }

        public new OdbcQueryProvider WithExecutor(QueryExecutor executor) =>
            (OdbcQueryProvider)With(executor: executor);

        public new OdbcQueryProvider WithLanguage(QueryLanguage language) =>
            (OdbcQueryProvider)With(language: language);

        public new OdbcQueryProvider WithMapping(QueryMapping mapping) =>
            (OdbcQueryProvider)With(mapping: mapping);

        public new OdbcQueryProvider WithPolicy(QueryPolicy policy) =>
            (OdbcQueryProvider)With(policy: policy);

        public new OdbcQueryProvider WithLog(TextWriter? log) =>
            (OdbcQueryProvider)With(log: log);

        public new OdbcQueryProvider WithCache(QueryCache? cache) =>
            (OdbcQueryProvider)With(cache: cache);

        protected override EntityProvider Construct(
            QueryExecutor executor,
            QueryLanguage language,
            QueryMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache)
        {
            return new OdbcQueryProvider(executor, language, mapping, policy, log, cache);
        }

        protected override FormattingOptions FormattingOptions { get; } =
            FormattingOptions.Default.WithIsOdbc(true);
    }
}