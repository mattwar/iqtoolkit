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
            public class DbBatchQueryExecutable<T> : IAsyncEnumerable<T>
            {
                private readonly DbQueryExecutor executor;
                private readonly QueryCommand query;
                private readonly object[][] paramSets;
                private readonly Func<FieldReader, T> projector;

                public DbBatchQueryExecutable(DbQueryExecutor executor, QueryCommand query, object[][] paramSets, Func<FieldReader, T> projector)
                {
                    this.executor = executor;
                    this.query = query;
                    this.paramSets = paramSets;
                    this.projector = projector;
                }

                public Task<IAsyncEnumerator<T>> GetEnumeratorAsync(CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Task.FromResult<IAsyncEnumerator<T>>(new Enumerator(this));
                }

                public IEnumerator<T> GetEnumerator()
                {
                    return new Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public class Enumerator : IAsyncEnumerator<T>, IEnumerator<T>, IDisposable
                {
                    private readonly DbBatchQueryExecutable<T> parent;
                    private DbCommand command;
                    private DbDataReader dataReader;
                    private FieldReader fieldReader;
                    private int paramSet;
                    private T current;
                    private bool closed;

                    public Enumerator(DbBatchQueryExecutable<T> parent)
                    {
                        this.parent = parent;
                        this.parent.executor.LogCommand(this.parent.query);
                        this.parent.executor.StartUsingConnection();
                        this.command = this.parent.executor.GetCommand(this.parent.query);
                        this.command.Prepare();
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

                        nextBatch:
                        if (this.dataReader == null)
                        {
                            this.parent.executor.LogParameters(this.parent.query, this.parent.paramSets[this.paramSet]);
                            this.parent.executor.SetParameterValues(this.parent.query, this.command, this.parent.paramSets[this.paramSet]);
                            try
                            {
                                this.dataReader = await this.command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                                this.fieldReader = new DbFieldReader(this.parent.executor, this.dataReader);
                            }
                            finally
                            {
                                if (this.dataReader == null)
                                {
                                    this.Dispose();
                                }
                            }
                        }

                        if (await this.dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            this.current = this.parent.projector(this.fieldReader);
                            return true;
                        }
                        else if (this.paramSet < this.parent.paramSets.Length - 1)
                        {
                            this.paramSet++;
                            this.dataReader = null;
                            this.fieldReader = null;
                            goto nextBatch;
                        }
                        else
                        {
                            this.Dispose();
                            return false;
                        }
                    }

                    public bool MoveNext()
                    {
                        nextBatch:
                        if (this.dataReader == null)
                        {
                            this.parent.executor.LogParameters(this.parent.query, this.parent.paramSets[this.paramSet]);
                            this.parent.executor.SetParameterValues(this.parent.query, this.command, this.parent.paramSets[this.paramSet]);
                            try
                            {
                                this.dataReader = this.command.ExecuteReader();
                                this.fieldReader = new DbFieldReader(this.parent.executor, this.dataReader);
                            }
                            finally
                            {
                                if (this.dataReader == null)
                                {
                                    this.Dispose();
                                }
                            }
                        }

                        if (this.dataReader.Read())
                        {
                            this.current = this.parent.projector(this.fieldReader);
                            return true;
                        }
                        else if (this.paramSet < this.parent.paramSets.Length - 1)
                        {
                            this.paramSet++;
                            this.dataReader = null;
                            this.fieldReader = null;
                            goto nextBatch;
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
                            this.parent.executor.StopUsingConnection();
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
