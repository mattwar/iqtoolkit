// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Access
{
    using Sql;
    using System.Data;

    /// <summary>
    /// Microsoft Access SQL <see cref="QueryType"/>
    /// </summary>
    public class AccessQueryType : QueryType
    {
        public AccessType Type { get; }
        public override bool NotNull { get; }
        public override int Length { get; }
        public override short Precision { get; }
        public override short Scale { get; }

        public AccessQueryType(
            AccessType accessType,
            bool notNull = false,
            int length = 0,
            short precision = 0,
            short scale = 0)
        {
            this.Type = accessType;
            this.NotNull = notNull;
            this.Length = length;
            this.Precision = precision;
            this.Scale = scale;
        }

        public override QueryTypeSystem TypeSystem =>
            AccessTypeSystem.Singleton;

        public override bool TryGetSqlType(out SqlType sqlType)
        {
            return this.Type.TryGetSqlType(out sqlType);
        }

        public override bool TryGetDbType(out DbType dbType)
        {
            return this.Type.TryGetDbType(out dbType);
        }
    }
}