// Copyright(c) Microsoft Corporation.All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
using System.Data;

namespace IQToolkit
{
    using AnsiSql;

    /// <summary>
    /// A scalar type as understood by the database.
    /// </summary>
    public abstract class QueryType
    {
        public abstract QueryTypeSystem TypeSystem { get; }
        
        public abstract bool NotNull { get; }
        public abstract int Length { get; }
        public abstract short Precision { get; }
        public abstract short Scale { get; }

        public static QueryType Unknown = 
            new AnsiSqlQueryType(AnsiSqlType.NVarChar, false, 0, 0, 0);

        /// <summary>
        /// Returns true if this type can be represented as an ANSI SQL type.
        /// </summary>
        public abstract bool TryGetSqlType(out AnsiSqlType sqlType);

        /// <summary>
        /// Returns true if this type can be represented as <see cref="DbType"/>.
        /// </summary>
        public abstract bool TryGetDbType(out DbType dbType);
    }
}