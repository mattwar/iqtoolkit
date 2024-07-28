// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;
using System.Data.Common;

namespace IQToolkit.Data
{
    using Utils;

    /// <summary>
    /// A <see cref="FieldReader"/> implemented over a <see cref="IDataReader"/>.
    /// </summary>
    public class DbFieldReader : FieldReader
    {
        private readonly TypeConverter _converter;
        private readonly IDataReader _reader;

        public DbFieldReader(TypeConverter converter, IDataReader reader)
        {
            _converter = converter;
            _reader = reader;
            this.Init();
        }

        protected override int FieldCount
        {
            get { return _reader.FieldCount; }
        }

        protected override Type GetFieldType(int ordinal)
        {
            return _reader.GetFieldType(ordinal);
        }

        protected override bool IsDBNull(int ordinal)
        {
            return _reader.IsDBNull(ordinal);
        }

        protected override T GetValue<T>(int ordinal)
        {
            return (T)_converter.Convert(_reader.GetValue(ordinal), typeof(T))!;
        }

        protected override Byte GetByte(int ordinal)
        {
            return _reader.GetByte(ordinal);
        }

        protected override Char GetChar(int ordinal)
        {
            return _reader.GetChar(ordinal);
        }

        protected override DateTime GetDateTime(int ordinal)
        {
            return _reader.GetDateTime(ordinal);
        }

        protected override Decimal GetDecimal(int ordinal)
        {
            return _reader.GetDecimal(ordinal);
        }

        protected override Double GetDouble(int ordinal)
        {
            return _reader.GetDouble(ordinal);
        }

        protected override Single GetSingle(int ordinal)
        {
            return _reader.GetFloat(ordinal);
        }

        protected override Guid GetGuid(int ordinal)
        {
            return _reader.GetGuid(ordinal);
        }

        protected override Int16 GetInt16(int ordinal)
        {
            return _reader.GetInt16(ordinal);
        }

        protected override Int32 GetInt32(int ordinal)
        {
            return _reader.GetInt32(ordinal);
        }

        protected override Int64 GetInt64(int ordinal)
        {
            return _reader.GetInt64(ordinal);
        }

        protected override String GetString(int ordinal)
        {
            return _reader.GetString(ordinal);
        }
    }
}