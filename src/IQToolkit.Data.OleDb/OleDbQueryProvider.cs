// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data.OleDb;
using System.IO;

namespace IQToolkit.Data.OleDb
{
    /// <summary>
    /// A base <see cref="DbEntityProvider"/> for OLEDB database providers
    /// </summary>
    public class OleDbQueryProvider : EntityProvider
    {
        protected OleDbQueryProvider(
            QueryExecutor executor,
            QueryLanguage language,
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

        public OleDbQueryProvider(
            QueryExecutor executor,
            QueryLanguage language)
            : this(executor, language, null, null, null, null)
        {
        }

        public OleDbQueryProvider(
            OleDbConnection connection,
            QueryLanguage language)
            : this(new OleDbQueryExecutor(connection), language)
        {
        }

        public new OleDbQueryProvider WithExecutor(QueryExecutor executor) =>
            (OleDbQueryProvider)With(executor: executor);

        public new OleDbQueryProvider WithLanguage(QueryLanguage language) =>
            (OleDbQueryProvider)With(language: language);

        public new OleDbQueryProvider WithMapping(QueryMapping mapping) =>
            (OleDbQueryProvider)With(mapping: mapping);

        public new OleDbQueryProvider WithPolicy(QueryPolicy policy) =>
            (OleDbQueryProvider)With(policy: policy);

        public new OleDbQueryProvider WithLog(TextWriter? log) =>
            (OleDbQueryProvider)With(log: log);

        public new OleDbQueryProvider WithCache(QueryCache? cache) =>
            (OleDbQueryProvider)With(cache: cache);

        protected override EntityProvider Construct(
            QueryExecutor executor,
            QueryLanguage language,
            QueryMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache)
        {
            return new OleDbQueryProvider(executor, language, mapping, policy, log, cache);
        }
    }
}