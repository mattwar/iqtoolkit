// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;

namespace IQToolkit.AnsiSql
{
    public static class AnsiSqlTypeExtensions
    {
        public static bool TryGetDbType(this AnsiSqlType sqlType, out DbType dbType)
        {
            dbType = GetDbType(sqlType);
            return dbType != UnknownDbType;
        }

        private static readonly DbType UnknownDbType = (DbType)(-1);

        private static DbType GetDbType(AnsiSqlType sqlType)
        {
            switch (sqlType)
            {
                case AnsiSqlType.BigInt:
                    return DbType.Int64;
                case AnsiSqlType.Binary:
                    return DbType.Binary;
                case AnsiSqlType.Bit:
                    return DbType.Boolean;
                case AnsiSqlType.Char:
                    return DbType.AnsiStringFixedLength;
                case AnsiSqlType.Date:
                    return DbType.Date;
                case AnsiSqlType.DateTime:
                case AnsiSqlType.SmallDateTime:
                    return DbType.DateTime;
                case AnsiSqlType.DateTime2:
                    return DbType.DateTime2;
                case AnsiSqlType.DateTimeOffset:
                    return DbType.DateTimeOffset;
                case AnsiSqlType.Decimal:
                    return DbType.Decimal;
                case AnsiSqlType.Float:
                case AnsiSqlType.Real:
                    return DbType.Double;
                case AnsiSqlType.Image:
                    return DbType.Binary;
                case AnsiSqlType.Integer:
                    return DbType.Int32;
                case AnsiSqlType.Money:
                case AnsiSqlType.SmallMoney:
                    return DbType.Currency;
                case AnsiSqlType.NChar:
                    return DbType.StringFixedLength;
                case AnsiSqlType.NText:
                case AnsiSqlType.NVarChar:
                    return DbType.String;
                case AnsiSqlType.SmallInt:
                    return DbType.Int16;
                case AnsiSqlType.Text:
                    return DbType.AnsiString;
                case AnsiSqlType.Time:
                    return DbType.Time;
                case AnsiSqlType.Timestamp:
                    return DbType.Binary;
                case AnsiSqlType.TinyInt:
                    return DbType.SByte;
                case AnsiSqlType.Udt:
                    return DbType.Object;
                case AnsiSqlType.UniqueIdentifier:
                    return DbType.Guid;
                case AnsiSqlType.VarBinary:
                    return DbType.Binary;
                case AnsiSqlType.VarChar:
                    return DbType.AnsiString;
                case AnsiSqlType.Variant:
                    return DbType.Object;
                case AnsiSqlType.Xml:
                    return DbType.String;
                default:
                    return UnknownDbType;
            }
        }
    }
}