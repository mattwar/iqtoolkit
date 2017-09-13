// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Data;

namespace IQToolkit.Data
{
    using Common;

    /// <summary>
    /// A <see cref="QueryType"/> defined over <see cref="SqlDbType"/>.
    /// </summary>
    public class DbQueryType : QueryType
    {
        private readonly SqlDbType dbType;
        private readonly bool notNull;
        private readonly int length;
        private readonly short precision;
        private readonly short scale;

        /// <summary>
        /// Construct a <see cref="DbQueryType"/>
        /// </summary>
        public DbQueryType(SqlDbType dbType, bool notNull, int length, short precision, short scale)
        {
            this.dbType = dbType;
            this.notNull = notNull;
            this.length = length;
            this.precision = precision;
            this.scale = scale;
        }

        public DbType DbType
        {
            get { return DbTypeSystem.GetDbType(this.dbType); }
        }

        public SqlDbType SqlDbType
        {
            get { return this.dbType; }
        }

        public override int Length
        {
            get { return this.length; }
        }

        public override bool NotNull
        {
            get { return this.notNull; }
        }

        public override short Precision
        {
            get { return this.precision; }
        }

        public override short Scale
        {
            get { return this.scale; }
        }
    }
}