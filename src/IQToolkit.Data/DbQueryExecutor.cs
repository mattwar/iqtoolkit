// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IQToolkit.Data
{
    using Common;

    public partial class DbEntityProvider
    {
        public partial class DbQueryExecutor : QueryExecutor
        {
            DbEntityProvider provider;
            int rowsAffected;

            public DbQueryExecutor(DbEntityProvider provider)
            {
                this.provider = provider;
            }

            public DbEntityProvider Provider
            {
                get { return this.provider; }
            }

            public override int RowsAffected
            {
                get { return this.rowsAffected; }
            }

            public virtual bool BufferResultRows
            {
                get { return false; }
            }

            public bool ActionOpenedConnection
            {
                get { return this.provider.actionOpenedConnection; }
            }

            public void StartUsingConnection()
            {
                this.provider.StartUsingConnection();
            }

            public void StopUsingConnection()
            {
                this.provider.StopUsingConnection();
            }

            public override object Convert(object value, Type type)
            {
                if (value == null)
                {
                    return TypeHelper.GetDefault(type);
                }
                type = TypeHelper.GetNonNullableType(type);
                Type vtype = value.GetType();
                if (type != vtype)
                {
                    if (type.IsEnum)
                    {
                        if (vtype == typeof(string))
                        {
                            return Enum.Parse(type, (string)value);
                        }
                        else
                        {
                            Type utype = Enum.GetUnderlyingType(type);
                            if (utype != vtype)
                            {
                                value = System.Convert.ChangeType(value, utype);
                            }
                            return Enum.ToObject(type, value);
                        }
                    }
                    return System.Convert.ChangeType(value, type);
                }
                return value;
            }

            public override IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
            {
                var executable = new DbQueryExecutable<T>(this, command, paramValues, fnProjector);
                if (this.BufferResultRows)
                {
                    return new ResultQuery<T>(new BufferedEnumerable<T>(executable));
                }
                else
                {
                    return new ResultQuery<T>(executable);
                }
            }

            public override int ExecuteCommand(QueryCommand query, object[] paramValues)
            {
                this.StartUsingConnection();
                try
                {
                    this.LogCommand(query, paramValues);
                    DbCommand cmd = this.GetCommand(query, paramValues);
                    this.rowsAffected = cmd.ExecuteNonQuery();
                    return this.rowsAffected;
                }
                finally
                {
                    this.StopUsingConnection();
                }
            }

            public override IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream)
            {
                var batch = new DbBatchCommandExecutable(this, query, paramSets.ToArray());
                if (!stream)
                {
                    return new BufferedEnumerable<int>(batch);
                }
                else
                {
                    return batch;
                }
            }

            public override IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<FieldReader, T> fnProjector, MappingEntity entity, int batchSize, bool stream)
            {
                var batch = new DbBatchQueryExecutable<T>(this, query, paramSets.ToArray(), fnProjector);
                if (!stream)
                {
                    return new BufferedEnumerable<T>(batch);
                }
                else
                {
                    return batch;
                }
            }

            public override IEnumerable<T> ExecuteDeferred<T>(QueryCommand query, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
            {
                return new BufferedEnumerable<T>(new DbQueryExecutable<T>(this, query, paramValues, fnProjector));
            }

            /// <summary>
            /// Get an ADO command object initialized with the command-text and parameters
            /// </summary>
            /// <param name="commandText"></param>
            /// <param name="paramNames"></param>
            /// <param name="paramValues"></param>
            /// <returns></returns>
            public virtual DbCommand GetCommand(QueryCommand query, object[] paramValues = null)
            {
                // create command object (and fill in parameters)
                DbCommand cmd = this.provider.Connection.CreateCommand();
                cmd.CommandText = query.CommandText;
                if (this.provider.Transaction != null)
                    cmd.Transaction = this.provider.Transaction;
                this.SetParameterValues(query, cmd, paramValues);
                return cmd;
            }

            public virtual void SetParameterValues(QueryCommand query, DbCommand command, object[] paramValues)
            {
                if (query.Parameters.Count > 0 && command.Parameters.Count == 0)
                {
                    for (int i = 0, n = query.Parameters.Count; i < n; i++)
                    {
                        this.AddParameter(command, query.Parameters[i], paramValues != null ? paramValues[i] : null);
                    }
                }
                else if (paramValues != null)
                {
                    for (int i = 0, n = command.Parameters.Count; i < n; i++)
                    {
                        DbParameter p = command.Parameters[i];
                        if (p.Direction == System.Data.ParameterDirection.Input
                         || p.Direction == System.Data.ParameterDirection.InputOutput)
                        {
                            p.Value = paramValues[i] ?? DBNull.Value;
                        }
                    }
                }
            }

            protected virtual void AddParameter(DbCommand command, QueryParameter parameter, object value)
            {
                DbParameter p = command.CreateParameter();
                p.ParameterName = parameter.Name;
                p.Value = value ?? DBNull.Value;
                command.Parameters.Add(p);
            }

            public virtual void GetParameterValues(DbCommand command, object[] paramValues)
            {
                if (paramValues != null)
                {
                    for (int i = 0, n = command.Parameters.Count; i < n; i++)
                    {
                        if (command.Parameters[i].Direction != System.Data.ParameterDirection.Input)
                        {
                            object value = command.Parameters[i].Value;
                            if (value == DBNull.Value)
                                value = null;
                            paramValues[i] = value;
                        }
                    }
                }
            }

            public virtual void LogMessage(string message)
            {
                if (this.provider.Log != null)
                {
                    this.provider.Log.WriteLine(message);
                }
            }

            /// <summary>
            /// Write a command & parameters to the log
            /// </summary>
            /// <param name="command"></param>
            /// <param name="paramValues"></param>
            public virtual void LogCommand(QueryCommand command, object[] parameters = null)
            {
                if (this.provider.Log != null)
                {
                    this.provider.Log.WriteLine(command.CommandText);
                    if (parameters != null)
                    {
                        this.LogParameters(command, parameters);
                    }

                    this.provider.Log.WriteLine();
                }
            }

            public virtual void LogParameters(QueryCommand command, object[] parameters)
            {
                if (this.provider.Log != null)
                {
                    for (int i = 0, n = command.Parameters.Count; i < n; i++)
                    {
                        var p = command.Parameters[i];
                        var v = parameters[i];

                        if (v == null || v == DBNull.Value)
                        {
                            this.provider.Log.WriteLine("-- {0} = NULL", p.Name);
                        }
                        else
                        {
                            this.provider.Log.WriteLine("-- {0} = [{1}]", p.Name, v);
                        }
                    }
                }
            }
        }
    }
}