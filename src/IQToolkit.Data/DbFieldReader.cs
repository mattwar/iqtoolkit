// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data.Common;

namespace IQToolkit.Data
{
    using Common;

    /// <summary>
    /// A <see cref="FieldReader"/> implemented over a <see cref="DbDataReader"/>.
    /// </summary>
    public class DbFieldReader : FieldReader
    {
        private readonly QueryExecutor executor;
        private readonly DbDataReader reader;

        public DbFieldReader(QueryExecutor executor, DbDataReader reader)
        {
            this.executor = executor;
            this.reader = reader;
            this.Init();
        }

        protected override int FieldCount
        {
            get { return this.reader.FieldCount; }
        }

        protected override Type GetFieldType(int ordinal)
        {
            return this.reader.GetFieldType(ordinal);
        }

        protected override bool IsDBNull(int ordinal)
        {
            return this.reader.IsDBNull(ordinal);
        }

        protected override T GetValue<T>(int ordinal)
        {
            return (T)this.executor.Convert(this.reader.GetValue(ordinal), typeof(T));
        }

        protected override Byte GetByte(int ordinal)
        {
            return this.reader.GetByte(ordinal);
        }

        protected override Char GetChar(int ordinal)
        {
            return this.reader.GetChar(ordinal);
        }

        protected override DateTime GetDateTime(int ordinal)
        {
            return this.reader.GetDateTime(ordinal);
        }

        protected override Decimal GetDecimal(int ordinal)
        {
            return this.reader.GetDecimal(ordinal);
        }

        protected override Double GetDouble(int ordinal)
        {
            return this.reader.GetDouble(ordinal);
        }

        protected override Single GetSingle(int ordinal)
        {
            return this.reader.GetFloat(ordinal);
        }

        protected override Guid GetGuid(int ordinal)
        {
            return this.reader.GetGuid(ordinal);
        }

        protected override Int16 GetInt16(int ordinal)
        {
            return this.reader.GetInt16(ordinal);
        }

        protected override Int32 GetInt32(int ordinal)
        {
            return this.reader.GetInt32(ordinal);
        }

        protected override Int64 GetInt64(int ordinal)
        {
            return this.reader.GetInt64(ordinal);
        }

        protected override String GetString(int ordinal)
        {
            return this.reader.GetString(ordinal);
        }
    }
}