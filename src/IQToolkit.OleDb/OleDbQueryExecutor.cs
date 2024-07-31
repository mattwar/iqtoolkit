// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace IQToolkit.OleDb
{
    using Data;
    using Entities;
    using Utils;

    public class OleDbQueryExecutor : DbQueryExecutor
    {
        protected OleDbQueryExecutor(
            OleDbConnection connection,
            IsolationLevel isolation,
            IDbTransaction? transaction,
            TypeConverter converter,
            QueryTypeSystem typeSystem,
            TextWriter? log)
            : base(connection, isolation, transaction, converter, typeSystem, log)
        {
        }

        public OleDbQueryExecutor(
            OleDbConnection connection,
            IsolationLevel isolation = IsolationLevel.ReadCommitted,
            TypeConverter? converter = null,
            QueryTypeSystem? typeSystem = null,
            TextWriter? log = null)
            : base(connection, isolation, null, converter, typeSystem, log)
        {
        }

        /// <summary>
        /// The current <see cref="OleDbConnection"/>
        /// </summary>
        public new OleDbConnection Connection =>
            (OleDbConnection)base.Connection;

        /// <summary>
        /// Creates a new <see cref="OleDbQueryExecutor"/> with the <see cref="Converter"/> property assigned.
        /// </summary>
        public new OleDbQueryExecutor WithConverter(TypeConverter converter) =>
            (OleDbQueryExecutor)base.WithConverter(converter);

        /// <summary>
        /// Creates a new <see cref="OleDbQueryExecutor"/> with the <see cref="TypeSystem"/> property assigned.
        /// </summary>
        public new OleDbQueryExecutor WithTypeSystem(QueryTypeSystem typeSystem) =>
            (OleDbQueryExecutor)base.WithTypeSystem(typeSystem);

        /// <summary>
        /// Creates a new <see cref="OleDbQueryExecutor"/> with the <see cref="Log"/> property assigned.
        /// </summary>
        public new DbQueryExecutor WithLog(TextWriter? log) =>
            (OleDbQueryExecutor)base.WithLog(log);

        protected override QueryExecutor Construct(
            TypeConverter converter, 
            QueryTypeSystem typeSystem, 
            TextWriter? log)
        {
            return new OleDbQueryExecutor(
                this.Connection,
                this.Isolation,
                this.Transaction,
                converter,
                typeSystem,
                log
                );
        }

        protected override IDbDataParameter CreateParameter(
            IDbCommand command, 
            QueryParameter queryParameter)
        {
            var oleDbCommand = (OleDbCommand)command;
            var oleDbParameter = new OleDbParameter();

            var type = queryParameter.QueryType
                ?? this.TypeSystem.GetQueryType(queryParameter.Type);
            if (TryGetOleDbType(type, out var oleDbType))
                oleDbParameter.OleDbType = oleDbType;
            if (type.Precision != 0)
                oleDbParameter.Precision = (byte)type.Precision;
            if (type.Scale != 0)
                oleDbParameter.Scale = (byte)type.Scale;

            oleDbCommand.Parameters.Add(oleDbParameter);
            return oleDbParameter;
        }

        protected virtual bool TryGetOleDbType(QueryType type, out OleDbType oleDbType)
        {
            if (type.TryGetSqlType(out var sqlType)
                && sqlType.TryGetOleDbType(out oleDbType))
            {
                return true;
            }
            else
            {
                oleDbType = default;
                return false;
            }
        }
    }
}