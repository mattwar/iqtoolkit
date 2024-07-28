// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;

namespace IQToolkit.Data.Access
{
    using Sql;

    public static class AccessTypeExtensions
    {
        /// <summary>
        /// Gets the corresponding ANSI SQL type.
        /// </summary>
        public static bool TryGetSqlType(this AccessType accessType, out SqlType sqlType)
        {
            switch (accessType)
            {
                case AccessType.Binary:
                    sqlType = SqlType.Binary;
                    return true;
                case AccessType.Bit:
                    sqlType = SqlType.Bit;
                    return true;
                case AccessType.TinyInt:
                    sqlType = SqlType.TinyInt;
                    return true;
                case AccessType.Money:
                    sqlType = SqlType.Money;
                    return true;
                case AccessType.DateTime:
                    sqlType = SqlType.DateTime;
                    return true;
                case AccessType.UniqueIdentifier:
                    sqlType = SqlType.UniqueIdentifier;
                    return true;
                case AccessType.Real:
                    sqlType = SqlType.Real;
                    return true;
                case AccessType.Float:
                    sqlType = SqlType.Float;
                    return true;
                case AccessType.SmallInt:
                    sqlType = SqlType.SmallInt;
                    return true;
                case AccessType.Integer:
                    sqlType = SqlType.Integer;
                    return true;
                case AccessType.Text:
                    sqlType = SqlType.Text;
                    return true;
                case AccessType.Image:
                    sqlType = SqlType.Image;
                    return true;
                case AccessType.Char:
                    sqlType = SqlType.NVarChar;
                    return true;
                default:
                    sqlType = default;
                    return false;
            }
        }

        /// <summary>
        /// Gets the corresponding Microsoft Access SQL type.
        /// </summary>
        public static bool TryGetAccessType(this SqlType sqlType, out AccessType accessType)
        {
            switch (sqlType)
            {
                case SqlType.Binary:
                case SqlType.VarBinary:
                    accessType = AccessType.Binary;
                    break;
                case SqlType.Bit:
                    accessType = AccessType.Bit;
                    break;
                case SqlType.TinyInt:
                    accessType = AccessType.TinyInt;
                    break;
                case SqlType.Money:
                case SqlType.SmallMoney:
                    accessType = AccessType.Money;
                    break;
                case SqlType.Date:
                case SqlType.DateTime:
                case SqlType.DateTime2:
                case SqlType.DateTimeOffset:
                case SqlType.SmallDateTime:
                case SqlType.Time:
                case SqlType.Timestamp:
                    accessType = AccessType.DateTime;
                    break;
                case SqlType.UniqueIdentifier:
                    accessType = AccessType.UniqueIdentifier;
                    break;
                case SqlType.Decimal:
                case SqlType.BigInt:
                    accessType = AccessType.Decimal;
                    break;
                case SqlType.Real:
                    accessType = AccessType.Real;
                    break;
                case SqlType.Float:
                    accessType = AccessType.Float;
                    break;
                case SqlType.SmallInt:
                    accessType = AccessType.SmallInt;
                    break;
                case SqlType.Integer:
                    accessType = AccessType.Integer;
                    break;
                case SqlType.Image:
                    accessType = AccessType.Image;
                    break;
                case SqlType.Char:
                case SqlType.VarChar:
                case SqlType.NChar:
                case SqlType.NVarChar:
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