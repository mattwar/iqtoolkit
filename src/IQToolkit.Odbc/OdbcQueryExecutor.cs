// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.IO;

namespace IQToolkit.Odbc
{
    using AnsiSql;
    using Entities;
    using Entities.Data;
    using Utils;

    public class OdbcQueryExecutor : DbQueryExecutor
    {
        protected OdbcQueryExecutor(
            OdbcConnection connection,
            IsolationLevel isolation,
            DbTransaction? transaction,
            TypeConverter? converter,
            QueryTypeSystem? typeSystem,
            TextWriter? log)
            : base(connection, isolation, transaction, converter, typeSystem, log)
        {
        }

        /// <summary>
        /// Creates a new <see cref="OdbcQueryExecutor"/> for the specified connection.
        /// </summary>
        public OdbcQueryExecutor(
            OdbcConnection connection,
            IsolationLevel isolation = IsolationLevel.ReadCommitted,
            TypeConverter? converter = null,
            QueryTypeSystem? typeSystem = null,
            TextWriter? log = null)
            : base(connection, isolation, converter, typeSystem, log)
        {
        }

        /// <summary>
        /// The <see cref="OdbcConnection"/> used to execute queries.
        /// </summary>
        public new OdbcConnection Connection =>
            (OdbcConnection)base.Connection;

        /// <summary>
        /// Creates a new <see cref="OdbcQueryExecutor"/> with the <see cref="Converter"/> property assigned.
        /// </summary>
        public new OdbcQueryExecutor WithConverter(TypeConverter converter) =>
            (OdbcQueryExecutor)base.WithConverter(converter);

        /// <summary>
        /// Creates a new <see cref="OdbcQueryExecutor"/> with the <see cref="TypeSystem"/> property assigned.
        /// </summary>
        public new OdbcQueryExecutor WithTypeSystem(QueryTypeSystem typeSystem) =>
            (OdbcQueryExecutor)base.WithTypeSystem(typeSystem);

        /// <summary>
        /// Creates a new <see cref="OdbcQueryExecutor"/> with the <see cref="Log"/> property assigned.
        /// </summary>
        public new OdbcQueryExecutor WithLog(TextWriter? log) =>
            (OdbcQueryExecutor)base.WithLog(log);

        protected override IDbDataParameter CreateParameter(
            IDbCommand command, 
            QueryParameter queryParameter)
        {
            var odbcParameter = new OdbcParameter();

            var qt = queryParameter.QueryType
                ?? this.TypeSystem.GetQueryType(queryParameter.Type);
            if (TryGetOdbcType(qt, out var odbcType))
                odbcParameter.OdbcType = odbcType;
            if (qt.Precision != 0)
                odbcParameter.Precision = (byte)qt.Precision;
            if (qt.Scale != 0)
                odbcParameter.Scale = (byte)qt.Scale;

            command.Parameters.Add(odbcParameter);
            return odbcParameter;
        }

        /// <summary>
        /// Override this in a custom <see cref="OdbcQueryExecutor"/>
        /// if you have a better way to go from language specific <see cref="QueryType"/>
        /// to <see cref="OdbcType"/> than first converting to <see cref="AnsiSqlType"/>.
        /// </summary>
        protected virtual bool TryGetOdbcType(QueryType type, out OdbcType odbcType)
        {
            if (type.TryGetSqlType(out var sqlType)
                && sqlType.TryGetOdbcType(out odbcType))
            {
                return true;
            }
            else
            {
                odbcType = default;
                return false;
            }
        }
    }
}