// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;

namespace IQToolkit.Access
{
    using AnsiSql;

    public static class AccessTypeExtensions
    {
        /// <summary>
        /// Gets the corresponding ANSI SQL type.
        /// </summary>
        public static bool TryGetSqlType(this AccessType accessType, out AnsiSqlType sqlType)
        {
            switch (accessType)
            {
                case AccessType.Binary:
                    sqlType = AnsiSqlType.Binary;
                    return true;
                case AccessType.Bit:
                    sqlType = AnsiSqlType.Bit;
                    return true;
                case AccessType.TinyInt:
                    sqlType = AnsiSqlType.TinyInt;
                    return true;
                case AccessType.Money:
                    sqlType = AnsiSqlType.Money;
                    return true;
                case AccessType.DateTime:
                    sqlType = AnsiSqlType.DateTime;
                    return true;
                case AccessType.UniqueIdentifier:
                    sqlType = AnsiSqlType.UniqueIdentifier;
                    return true;
                case AccessType.Real:
                    sqlType = AnsiSqlType.Real;
                    return true;
                case AccessType.Float:
                    sqlType = AnsiSqlType.Float;
                    return true;
                case AccessType.SmallInt:
                    sqlType = AnsiSqlType.SmallInt;
                    return true;
                case AccessType.Integer:
                    sqlType = AnsiSqlType.Integer;
                    return true;
                case AccessType.Text:
                    sqlType = AnsiSqlType.Text;
                    return true;
                case AccessType.Image:
                    sqlType = AnsiSqlType.Image;
                    return true;
                case AccessType.Char:
                    sqlType = AnsiSqlType.NVarChar;
                    return true;
                default:
                    sqlType = default;
                    return false;
            }
        }

        /// <summary>
        /// Gets the corresponding Microsoft Access SQL type.
        /// </summary>
        public static bool TryGetAccessType(this AnsiSqlType sqlType, out AccessType accessType)
        {
            switch (sqlType)
            {
                case AnsiSqlType.Binary:
                case AnsiSqlType.VarBinary:
                    accessType = AccessType.Binary;
                    break;
                case AnsiSqlType.Bit:
                    accessType = AccessType.Bit;
                    break;
                case AnsiSqlType.TinyInt:
                    accessType = AccessType.TinyInt;
                    break;
                case AnsiSqlType.Money:
                case AnsiSqlType.SmallMoney:
                    accessType = AccessType.Money;
                    break;
                case AnsiSqlType.Date:
                case AnsiSqlType.DateTime:
                case AnsiSqlType.DateTime2:
                case AnsiSqlType.DateTimeOffset:
                case AnsiSqlType.SmallDateTime:
                case AnsiSqlType.Time:
                case AnsiSqlType.Timestamp:
                    accessType = AccessType.DateTime;
                    break;
                case AnsiSqlType.UniqueIdentifier:
                    accessType = AccessType.UniqueIdentifier;
                    break;
                case AnsiSqlType.Decimal:
                case AnsiSqlType.BigInt:
                    accessType = AccessType.Decimal;
                    break;
                case AnsiSqlType.Real:
                    accessType = AccessType.Real;
                    break;
                case AnsiSqlType.Float:
                    accessType = AccessType.Float;
                    break;
                case AnsiSqlType.SmallInt:
                    accessType = AccessType.SmallInt;
                    break;
                case AnsiSqlType.Integer:
                    accessType = AccessType.Integer;
                    break;
                case AnsiSqlType.Image:
                    accessType = AccessType.Image;
                    break;
                case AnsiSqlType.Char:
                case AnsiSqlType.VarChar:
                case AnsiSqlType.NChar:
                case AnsiSqlType.NVarChar:
                    accessType = AccessType.Char;
                    break;
                default:
                    accessType = default;
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the corresponding <see cref="DbType"/>.
        /// </summary>
        public static bool TryGetDbType(this AccessType accessType, out DbType dbType)
        {
            if (TryGetSqlType(accessType, out var sqlType))
            {
                return sqlType.TryGetDbType(out dbType);
            }

            dbType = default;
            return false;
        }
    }
}