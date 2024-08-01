// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;
using System.IO;

namespace IQToolkit.Entities.Data
{
    /// <summary>
    /// An <see cref="EntityProvider"/> using an <see cref="IDbConnection"/>.
    /// </summary>
    public class DbEntityProvider : EntityProvider
    {
        protected DbEntityProvider(
            DbQueryExecutor executor,
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

        public DbEntityProvider(
            DbQueryExecutor executor,
            QueryLanguage? language = null)
            : this(executor, language, null, null, null, null)
        {
        }

        public DbEntityProvider(
            IDbConnection connection,
            QueryLanguage? language = null)
            : this(new DbQueryExecutor(connection), language)
        {
        }

        public new DbQueryExecutor Executor =>
            (DbQueryExecutor)base.Executor;

        public new DbEntityProvider WithExecutor(QueryExecutor executor) =>
            (DbEntityProvider)With(executor: executor);

        public new DbEntityProvider WithLanguage(QueryLanguage language) =>
            (DbEntityProvider)With(language: language);

        public new DbEntityProvider WithMapping(EntityMapping mapping) =>
            (DbEntityProvider)With(mapping: mapping);

        public new DbEntityProvider WithPolicy(QueryPolicy policy) =>
            (DbEntityProvider)With(policy: policy);

        public new DbEntityProvider WithLog(TextWriter? log) =>
            (DbEntityProvider)With(log: log);

        public new DbEntityProvider WithCache(QueryCache? cache) =>
            (DbEntityProvider)With(cache: cache);

        protected override EntityProvider Construct(
            QueryExecutor executor,
            QueryLanguage language,
            EntityMapping? mapping,
            QueryPolicy? policy,
            TextWriter? log,
            QueryCache? cache)
        {
            return new DbEntityProvider(
                (DbQueryExecutor)executor,
                language,
                mapping,
                policy,
                log,
                cache
                );
        }
    }
}