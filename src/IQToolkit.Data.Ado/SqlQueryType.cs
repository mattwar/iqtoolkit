// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Data;

namespace IQToolkit.Data
{
    using Common;
    using System;

    /// <summary>
    /// A <see cref="QueryType"/> defined over <see cref="SqlType"/>.
    /// </summary>
    public class SqlQueryType : QueryType
    {
        private readonly SqlType sqlType;
        private readonly bool notNull;
        private readonly int length;
        private readonly short precision;
        private readonly short scale;

        /// <summary>
        /// Construct a <see cref="SqlQueryType"/>
        /// </summary>
        public SqlQueryType(SqlType sqlType, bool notNull, int length, short precision, short scale)
        {
            this.sqlType = sqlType;
            this.notNull = notNull;
            this.length = length;
            this.precision = precision;
            this.scale = scale;
        }

        public SqlType SqlType
        {
            get { return this.sqlType; }
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