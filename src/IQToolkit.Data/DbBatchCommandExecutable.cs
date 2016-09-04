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
            public class DbBatchCommandExecutable : IAsyncEnumerable<int>
            {
                private readonly DbQueryExecutor executor;
                private readonly QueryCommand query;
                private readonly object[][] paramSets;

                public DbBatchCommandExecutable(DbQueryExecutor executor, QueryCommand query, object[][] paramSets)
                {
                    this.executor = executor;
                    this.query = query;
                    this.paramSets = paramSets;
                }

                public Task<IAsyncEnumerator<int>> GetEnumeratorAsync(CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Task.FromResult<IAsyncEnumerator<int>>(new Enumerator(this));
                }

                public IEnumerator<int> GetEnumerator()
                {
                    return new Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public class Enumerator : IAsyncEnumerator<int>, IEnumerator<int>, IDisposable
                {
                    private readonly DbBatchCommandExecutable parent;
                    private DbCommand command;
                    private int paramSet;
                    private int current;
                    private bool closed;

                    public Enumerator(DbBatchCommandExecutable parent)
                    {
                        this.parent = parent;
                        this.parent.executor.LogCommand(this.parent.query);
                        this.parent.executor.StartUsingConnection();
                        this.command = this.parent.executor.GetCommand(this.parent.query);
                        this.command.Prepare();
                        this.paramSet = -1;
                    }

                    public int Current
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

                        if (this.paramSet < this.parent.paramSets.Length - 1)
                        {
                            this.paramSet++;
                            this.parent.executor.LogParameters(this.parent.query, this.parent.paramSets[this.paramSet]);
                            this.parent.executor.SetParameterValues(this.parent.query, this.command, this.parent.paramSets[this.paramSet]);
                            this.current = await this.command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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
                        if (this.paramSet < this.parent.paramSets.Length - 1)
                        {
                            this.paramSet++;
                            this.parent.executor.LogParameters(this.parent.query, this.parent.paramSets[this.paramSet]);
                            this.parent.executor.SetParameterValues(this.parent.query, this.command, this.parent.paramSets[this.paramSet]);
                            this.current = this.command.ExecuteNonQuery();
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
