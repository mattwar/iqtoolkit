// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;

namespace IQToolkit.Data
{
    public static class DbTypeHelper
    {
        public static DbType ToDbType(this SqlType sqlType)
        {
            switch (sqlType)
            {
                case SqlType.BigInt:
                    return DbType.Int64;
                case SqlType.Binary:
                    return DbType.Binary;
                case SqlType.Bit:
                    return DbType.Boolean;
                case SqlType.Char:
                    return DbType.AnsiStringFixedLength;
                case SqlType.Date:
                    return DbType.Date;
                case SqlType.DateTime:
                case SqlType.SmallDateTime:
                    return DbType.DateTime;
                case SqlType.DateTime2:
                    return DbType.DateTime2;
                case SqlType.DateTimeOffset:
                    return DbType.DateTimeOffset;
                case SqlType.Decimal:
                    return DbType.Decimal;
                case SqlType.Float:
                case SqlType.Real:
                    return DbType.Double;
                case SqlType.Image:
                    return DbType.Binary;
                case SqlType.Int:
                    return DbType.Int32;
                case SqlType.Money:
                case SqlType.SmallMoney:
                    return DbType.Currency;
                case SqlType.NChar:
                    return DbType.StringFixedLength;
                case SqlType.NText:
                case SqlType.NVarChar:
                    return DbType.String;
                case SqlType.SmallInt:
                    return DbType.Int16;
                case SqlType.Text:
                    return DbType.AnsiString;
                case SqlType.Time:
                    return DbType.Time;
                case SqlType.Timestamp:
                    return DbType.Binary;
                case SqlType.TinyInt:
                    return DbType.SByte;
                case SqlType.Udt:
                    return DbType.Object;
                case SqlType.UniqueIdentifier:
                    return DbType.Guid;
                case SqlType.VarBinary:
                    return DbType.Binary;
                case SqlType.VarChar:
                    return DbType.AnsiString;
                case SqlType.Variant:
                    return DbType.Object;
                case SqlType.Xml:
                    return DbType.String;
                default:
                    throw new InvalidOperationException(string.Format("Unhandled sql type: {0}", sqlType));
            }
        }
    }
}