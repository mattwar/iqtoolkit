// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Data;

namespace IQToolkit.AnsiSql
{
    /// <summary>
    /// A <see cref="QueryType"/> defined over <see cref="Type"/>.
    /// </summary>
    public sealed class AnsiSqlQueryType : QueryType
    {
        public override QueryTypeSystem TypeSystem { get; }

        public AnsiSqlType Type { get; }
        public override bool NotNull { get; }
        public override int Length { get; }
        public override short Precision { get; }
        public override short Scale { get; }

        /// <summary>
        /// Construct a <see cref="AnsiSqlQueryType"/>
        /// </summary>
        public AnsiSqlQueryType(
            QueryTypeSystem typeSystem,
            AnsiSqlType sqlType, 
            bool notNull = false, 
            int length = 0, 
            short precision = 0, 
            short scale = 0
            )
        {
            this.TypeSystem = typeSystem;
            this.Type = sqlType;
            this.NotNull = notNull;
            this.Length = length;
            this.Precision = precision;
            this.Scale = scale;
        }

        /// <summary>
        /// Construct a <see cref="AnsiSqlQueryType"/>
        /// </summary>
        public AnsiSqlQueryType(
            AnsiSqlType sqlType,
            bool notNull = false,
            int length = 0,
            short precision = 0,
            short scale = 0
            )
            : this(
                  AnsiSqlTypeSystem.Singleton,
                  sqlType,
                  notNull,
                  length,
                  precision,
                  scale)
        {
        }

        public override bool TryGetSqlType(out AnsiSqlType sqlType)
        {
            sqlType = this.Type;
            return true;
        }

        public override bool TryGetDbType(out DbType dbType)
        {
            return this.Type.TryGetDbType(out dbType);
        }
    }
}