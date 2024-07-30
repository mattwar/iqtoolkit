// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace IQToolkit.Data.Access
{
    using AnsiSql;

    /// <summary>
    /// Microsoft Access SQL <see cref="QueryTypeSystem"/>
    /// </summary>
    public sealed class AccessTypeSystem : QueryTypeSystem
    {
        private AccessTypeSystem()
        {
        }

        public static AccessTypeSystem Singleton = 
            new AccessTypeSystem();

        public override QueryType Parse(string typeDeclaration)
        {
            string[]? args = null;
            string? typeName = null;
            string? remainder = null;

            int openParen = typeDeclaration.IndexOf('(');
            if (openParen >= 0)
            {
                typeName = typeDeclaration.Substring(0, openParen).Trim();

                int closeParen = typeDeclaration.IndexOf(')', openParen);
                if (closeParen < openParen) closeParen = typeDeclaration.Length;

                string argstr = typeDeclaration.Substring(openParen + 1, closeParen - (openParen + 1));
                args = argstr.Split(',');
                remainder = typeDeclaration.Substring(closeParen + 1);
            }
            else
            {
                int space = typeDeclaration.IndexOf(' ');
                if (space >= 0)
                {
                    typeName = typeDeclaration.Substring(0, space);
                    remainder = typeDeclaration.Substring(space + 1).Trim();
                }
                else
                {
                    typeName = typeDeclaration;
                }
            }

            bool isNotNull = (remainder != null) 
                ? remainder.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase)
                : false;

            return this.GetQueryType(typeName, args, isNotNull);
        }

        public AccessQueryType GetQueryType(string typeName, string[]? args, bool isNotNull)
        {
            if (!TryGetAccessType(typeName, out var accessType))
            {
                throw new InvalidOperationException(
                    string.Format("The name '{0}' is not a valid Microsoft Access SQL type.", typeName)
                    );
            }

            // TEXT(n) is actually CHAR not TEXT
            if (accessType == AccessType.Text
                && args != null
                && args.Length == 1
                && string.Compare(typeName, "TEXT", true) == 0
                && int.TryParse(args[0], out var length))
            {
                // keep length since it was specified, though it should not make a difference
                return new AccessQueryType(AccessType.Char, notNull: isNotNull, length: length);
            }

            if (accessType == AccessType.Decimal)
            {
                short precision = 0;
                short scale = 0;

                if (args == null
                    || args.Length < 1
                    || !Int16.TryParse(args[0], out precision))
                {
                    precision = 18;
                }

                if (args == null
                    || args.Length < 2
                    || !Int16.TryParse(args[1], out scale))
                {
                    scale = 0;
                }

                return new AccessQueryType(accessType, isNotNull, precision: precision, scale: scale);
            }
            else
            {
                return new AccessQueryType(accessType, notNull: isNotNull);
            }
        }

        /// <summary>
        /// Map of access type names and synonyms to <see cref="AccessType"/> enum.
        /// </summary>
        private static Dictionary<string, AccessType> _typeNameToAccessTypeMap =
            new Dictionary<string, AccessType>(StringComparer.OrdinalIgnoreCase)
            {
                { "BINARY", AccessType.Binary },
                { "VARBINARY", AccessType.Binary },
                { "BINARY VARYING", AccessType.Binary },
                { "BIT VARYING", AccessType.Binary },
                { "BIT", AccessType.Bit },
                { "BOOLEAN", AccessType.Bit },
                { "LOGICAL", AccessType.Bit },
                { "LOGICAL1", AccessType.Bit },
                { "YESNO", AccessType.Bit },
                { "TINYINT", AccessType.TinyInt },
                { "INTEGER1", AccessType.TinyInt },
                { "BYTE", AccessType.TinyInt },
                { "COUNTER", AccessType.Counter },
                { "AUTOINCREMENT", AccessType.Counter },
                { "MONEY", AccessType.Money },
                { "CURRENCY", AccessType.Money },
                { "DATETIME", AccessType.DateTime },
                { "DATE", AccessType.DateTime },
                { "TIME", AccessType.DateTime },
                { "UNIQUEIDENTIFIER", AccessType.UniqueIdentifier },
                { "GUID", AccessType.UniqueIdentifier },
                { "DECIMAL", AccessType.Decimal },
                { "NUMERIC", AccessType.Decimal },
                { "DEC", AccessType.Decimal },
                { "REAL", AccessType.Real },
                { "SINGLE", AccessType.Real },
                { "FLOAT4", AccessType.Real },
                { "IEEESINGLE", AccessType.Real },
                { "FLOAT", AccessType.Float },
                { "DOUBLE", AccessType.Float },
                { "FLOAT8", AccessType.Float },
                { "IEEEDOUBLE", AccessType.Float },
                { "NUMBER", AccessType.Float },
                { "SMALLINT", AccessType.SmallInt },
                { "SHORT", AccessType.SmallInt },
                { "INTEGER2", AccessType.SmallInt },
                { "INTEGER", AccessType.Integer },
                { "LONG", AccessType.Integer },
                { "INT", AccessType.Integer },
                { "INTEGER4", AccessType.Integer },
                { "IMAGE", AccessType.Image },
                { "LONGBINARY", AccessType.Image },
                { "GENERAL", AccessType.Image },
                { "OLEOBJECT", AccessType.Image },
                { "TEXT", AccessType.Text },
                { "LONGTEXT", AccessType.Text },
                { "LONGCHAR", AccessType.Text },
                { "MEMO", AccessType.Text },
                { "NOTE", AccessType.Text },
                { "NTEXT", AccessType.Text },
                { "CHAR", AccessType.Char },
                { "ALPHANUMERIC", AccessType.Char },
                { "CHARACTER", AccessType.Char },
                { "STRING", AccessType.Char },
                { "VARCHAR", AccessType.Char },
                { "CHARACTER VARYING", AccessType.Char },
                { "NCHAR", AccessType.Char },
                { "NATIONAL CHARACTER", AccessType.Char },
                { "NATIONAL CHAR", AccessType.Char },
                { "NATIONAL CHARACTER VARYING", AccessType.Char },
                { "NATIONAL CHAR VARYING", AccessType.Char }
            };

        public bool TryGetAccessType(string typeName, out AccessType accessType)
        {
            // check for actual access type or synonym
            if (_typeNameToAccessTypeMap.TryGetValue(typeName, out accessType))
            {
                return true;
            }

            // look for generic sql type and try to convert to access type
            if (AnsiSqlTypeSystem.Singleton.TryGetSqlType(typeName, out var sqlType)
                && sqlType.TryGetAccessType(out accessType))
            {
                return true;
            }

            accessType = default;
            return false;
        }

        public override string Format(QueryType type, bool suppressSize)
        {
            var accessQueryType = (AccessQueryType)type;
            var accessType = accessQueryType.Type;
            var typeName = accessType.ToString().ToUpper();

            switch (accessType)
            {
                case AccessType.Decimal:
                    if (accessQueryType.Scale == 0)
                    {
                        if (accessQueryType.Precision == 18)
                        {
                            return typeName;
                        }
                        else
                        {
                            return string.Format("{0}({1})", typeName, type.Precision);
                        }
                    }
                    else
                    {
                        return string.Format("{0}({1},{2})", typeName, type.Precision, type.Scale);
                    }

                default:
                    return typeName;
            }
        }

        /// <summary>
        /// Gets the <see cref="QueryType"/> associated with a CLR type.
        /// </summary>
        public override QueryType GetQueryType(Type type)
        {
            bool isNotNull = type.GetTypeInfo().IsValueType && !TypeHelper.IsNullableType(type);
            type = TypeHelper.GetNonNullableType(type);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return new AccessQueryType(AccessType.Bit, isNotNull);
                case TypeCode.Byte:
                    return new AccessQueryType(AccessType.TinyInt, isNotNull);
                case TypeCode.Int16:
                case TypeCode.SByte: // tiny-int is unsigned
                    return new AccessQueryType(AccessType.SmallInt, isNotNull);
                case TypeCode.UInt16: // won't fit in smallint
                case TypeCode.Int32:
                    return new AccessQueryType(AccessType.Integer, isNotNull);
                case TypeCode.UInt32: // won't fit in integer
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return new AccessQueryType(AccessType.Decimal, isNotNull, precision: 28, scale: 0);
                case TypeCode.Single:
                    return new AccessQueryType(AccessType.Real, isNotNull);
                case TypeCode.Double:
                    return new AccessQueryType(AccessType.Float, isNotNull);
                case TypeCode.String:
                case TypeCode.Char:
                    return new AccessQueryType(AccessType.Char, isNotNull);
                case TypeCode.DateTime:
                    return new AccessQueryType(AccessType.DateTime, isNotNull);
                case TypeCode.Decimal:
                    return new AccessQueryType(AccessType.Decimal, isNotNull, precision: 18, scale: 4);
                default:
                    if (type == typeof(byte[]))
                        return new AccessQueryType(AccessType.Binary, isNotNull);
                    else if (type == typeof(char[]))
                        return new AccessQueryType(AccessType.Text, isNotNull);
                    else if (type == typeof(Guid))
                        return new AccessQueryType(AccessType.UniqueIdentifier, isNotNull);
                    else if (type == typeof(DateTimeOffset)
                        || type == typeof(TimeSpan))
                        return new AccessQueryType(AccessType.DateTime, isNotNull);
                    else if (type.GetTypeInfo().IsEnum)
                        return new AccessQueryType(AccessType.Integer, isNotNull);
                    else
                        return QueryType.Unknown;
            }
        }
    }
}