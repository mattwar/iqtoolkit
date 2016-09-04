// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IQToolkit.Data
{
    using Common;

    public partial class DbEntityProvider
    {
        public partial class DbQueryExecutor
        {
            public class DbQueryExecutable<T> : IAsyncEnumerable<T>, IEnumerable<T>, IEnumerable
            {
                private readonly DbQueryExecutor executor;
                private readonly QueryCommand query;
                private readonly object[] paramValues;
                private readonly Func<FieldReader, T> projector;

                public DbQueryExecutable(DbQueryExecutor executor, QueryCommand query, object[] paramValues, Func<FieldReader, T> projector)
                {
                    this.executor = executor;
                    this.query = query;
                    this.paramValues = paramValues;
                    this.projector = projector;
                }

                public async Task<IAsyncEnumerator<T>> GetEnumeratorAsync(CancellationToken cancellationToken)
                {
                    DbDataReader reader = null;
                    this.executor.StartUsingConnection();
                    try
                    {
                        this.executor.LogCommand(this.query, this.paramValues);

                        var command = this.executor.GetCommand(this.query, this.paramValues);
                        reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                        return new Enumerator(this.executor, reader, this.projector);
                    }
                    finally
                    {
                        if (reader == null)
                        {
                            this.executor.StopUsingConnection();
                        }
                    }
                }

                public IEnumerator<T> GetEnumerator()
                {
                    DbDataReader reader = null;
                    this.executor.StartUsingConnection();
                    try
                    {
                        this.executor.LogCommand(this.query, this.paramValues);

                        var command = this.executor.GetCommand(this.query, this.paramValues);
                        reader = command.ExecuteReader();

                        return new Enumerator(this.executor, reader, this.projector);
                    }
                    finally
                    {
                        if (reader == null)
                        {
                            this.executor.StopUsingConnection();
                        }
                    }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public class Enumerator : IAsyncEnumerator<T>, IEnumerator<T>, IDisposable
                {
                    private readonly DbQueryExecutor executor;
                    private readonly DbDataReader dataReader;
                    private readonly FieldReader fieldReader;
                    private readonly Func<FieldReader, T> projector;
                    private T current;
                    private bool closed;

                    public Enumerator(DbQueryExecutor executor, DbDataReader reader, Func<FieldReader, T> projector)
                    {
                        this.executor = executor;
                        this.dataReader = reader;
                        this.fieldReader = new DbFieldReader(executor, reader);
                        this.projector = projector;
                    }

                    public T Current
                    {
                        get { return this.current; }
                    }

                    object IEnumerator.Current
                    {
                        get { return this.Current; }
                    }

                    public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (await this.dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            this.current = this.projector(this.fieldReader);
                            return true;
                        }
                        else
                        {
                            this.Dispose();
                            return false;
                        }
                    }

                    public bool MoveNext()
                    {
                        if (this.dataReader.Read())
                        {
                            this.current = this.projector(this.fieldReader);
                            return true;
                        }
                        else
                        {
                            this.Dispose();
                            return false;
                        }
                    }

                    public void Dispose()
                    {
                        if (!this.closed)
                        {
                            this.closed = true;
                            this.dataReader.Close();
                            this.executor.StopUsingConnection();
                        }
                    }

                    public void Reset()
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }
    }
}
