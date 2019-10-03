// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Data;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data
{
    using Common;
    using System.Globalization;

    /// <summary>
    /// A <see cref="QueryTypeSystem"/> for types based on <see cref="SqlType"/>.
    /// Default parser, format implementations assume a type system similar to TSQL.
    /// </summary>
    public class SqlTypeSystem : QueryTypeSystem
    {
        public override QueryType Parse(string typeDeclaration)
        {
            string[] args = null;
            string typeName = null;
            string remainder = null;

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

            bool isNotNull = (remainder != null) ? remainder.ToUpper().Contains("NOT NULL") : false;

            return this.GetQueryType(typeName, args, isNotNull);
        }

        /// <summary>
        /// Gets the <see cref="QueryType"/> for a know database type.
        /// This API does not parse the type name.
        /// Arguments to the type are specified by the <see cref="P:args"/> parameter.
        /// </summary>
        /// <param name="typeName">The base name of a type in the databases language.</param>
        /// <param name="args">Any additional arguments (like length of a text type)</param>
        /// <param name="isNotNull">Determines if the type cannot be null.</param>
        public virtual QueryType GetQueryType(string typeName, string[] args, bool isNotNull)
        {
            if (String.Compare(typeName, "rowversion", StringComparison.OrdinalIgnoreCase) == 0)
            {
                typeName = "Timestamp";
            }

            if (String.Compare(typeName, "numeric", StringComparison.OrdinalIgnoreCase) == 0)
            {
                typeName = "Decimal";
            }

            if (String.Compare(typeName, "sql_variant", StringComparison.OrdinalIgnoreCase) == 0)
            {
                typeName = "Variant";
            }

            SqlType dbType = this.GetSqlType(typeName);

            int length = 0;
            short precision = 0;
            short scale = 0;

            switch (dbType)
            {
                case SqlType.Binary:
                case SqlType.Char:
                case SqlType.Image:
                case SqlType.NChar:
                case SqlType.NVarChar:
                case SqlType.VarBinary:
                case SqlType.VarChar:
                    if (args == null || args.Length < 1)
                    {
                        length = 80;
                    }
                    else if (string.Compare(args[0], "max", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        length = Int32.MaxValue;
                    }
                    else
                    {
                        length = Int32.Parse(args[0]);
                    }
                    break;
                case SqlType.Money:
                    if (args == null || args.Length < 1)
                    {
                        precision = 29;
                    }
                    else
                    {
                        precision = Int16.Parse(args[0], NumberFormatInfo.InvariantInfo);
                    }
                    if (args == null || args.Length < 2)
                    {
                        scale = 4;
                    }
                    else
                    {
                        scale = Int16.Parse(args[1], NumberFormatInfo.InvariantInfo);
                    }
                    break;
                case SqlType.Decimal:
                    if (args == null || args.Length < 1)
                    {
                        precision = 29;
                    }
                    else
                    {
                        precision = Int16.Parse(args[0], NumberFormatInfo.InvariantInfo);
                    }
                    if (args == null || args.Length < 2)
                    {
                        scale = 0;
                    }
                    else
                    {
                        scale = Int16.Parse(args[1], NumberFormatInfo.InvariantInfo);
                    }
                    break;
                case SqlType.Float:
                case SqlType.Real:
                    if (args == null || args.Length < 1)
                    {
                        precision = 29;
                    }
                    else
                    {
                        precision = Int16.Parse(args[0], NumberFormatInfo.InvariantInfo);
                    }
                    break;
            }

            return NewType(dbType, isNotNull, length, precision, scale);
        }

        /// <summary>
        /// Construct a new <see cref="QueryType"/> instance from 
        /// </summary>
        protected virtual QueryType NewType(SqlType type, bool isNotNull, int length, short precision, short scale)
        {
            return new SqlQueryType(type, isNotNull, length, precision, scale);
        }

        /// <summary>
        /// Gets the <see cref="SqlType"/> given the type name (same name as <see cref="SqlType"/> members)
        /// </summary>
        public virtual SqlType GetSqlType(string typeName)
        {
            return (SqlType)Enum.Parse(typeof(SqlType), typeName, true);
        }

        /// <summary>
        /// Default maximum size of a text data type.
        /// </summary>
        public virtual int StringDefaultSize
        {
            get { return Int32.MaxValue; }
        }

        /// <summary>
        /// Default maximum size of a binary data type.
        /// </summary>
        public virtual int BinaryDefaultSize
        {
            get { return Int32.MaxValue; }
        }

        /// <summary>
        /// Gets the <see cref="QueryType"/> associated with a CLR type.
        /// </summary>
        public override QueryType GetColumnType(Type type)
        {
            bool isNotNull = type.GetTypeInfo().IsValueType && !TypeHelper.IsNullableType(type);
            type = TypeHelper.GetNonNullableType(type);

            switch (TypeHelper.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return NewType(SqlType.Bit, isNotNull, 0, 0, 0);
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return NewType(SqlType.TinyInt, isNotNull, 0, 0, 0);
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return NewType(SqlType.SmallInt, isNotNull, 0, 0, 0);
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return NewType(SqlType.Int, isNotNull, 0, 0, 0);
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return NewType(SqlType.BigInt, isNotNull, 0, 0, 0);
                case TypeCode.Single:
                case TypeCode.Double:
                    return NewType(SqlType.Float, isNotNull, 0, 0, 0);
                case TypeCode.String:
                    return NewType(SqlType.NVarChar, isNotNull, this.StringDefaultSize, 0, 0);
                case TypeCode.Char:
                    return NewType(SqlType.NChar, isNotNull, 1, 0, 0);
                case TypeCode.DateTime:
                    return NewType(SqlType.DateTime, isNotNull, 0, 0, 0);
                case TypeCode.Decimal:
                    return NewType(SqlType.Decimal, isNotNull, 0, 29, 4);
                default:
                    if (type == typeof(byte[]))
                        return NewType(SqlType.VarBinary, isNotNull, this.BinaryDefaultSize, 0, 0);
                    else if (type == typeof(Guid))
                        return NewType(SqlType.UniqueIdentifier, isNotNull, 0, 0, 0);
                    else if (type == typeof(DateTimeOffset))
                        return NewType(SqlType.DateTimeOffset, isNotNull, 0, 0, 0);
                    else if (type == typeof(TimeSpan))
                        return NewType(SqlType.Time, isNotNull, 0, 0, 0);
                    else if (type.GetTypeInfo().IsEnum)
                        return NewType(SqlType.Int, isNotNull, 0, 0, 0);
                    else
                        return null;
            }
        }

        /// <summary>
        /// True if the <see cref="SqlType"/> is a variable length type.
        /// </summary>
        public static bool IsVariableLength(SqlType dbType)
        {
            switch (dbType)
            {
                case SqlType.Image:
                case SqlType.NText:
                case SqlType.NVarChar:
                case SqlType.Text:
                case SqlType.VarBinary:
                case SqlType.VarChar:
                case SqlType.Xml:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Format the <see cref="QueryType"/> as if specified in the database language.
        /// </summary>
        public override string Format(QueryType type, bool suppressSize)
        {
            var sqlType = (SqlQueryType)type;
            StringBuilder sb = new StringBuilder();
            sb.Append(sqlType.SqlType.ToString().ToUpper());

            if (sqlType.Length > 0 && !suppressSize)
            {
                if (sqlType.Length == Int32.MaxValue)
                {
                    sb.Append("(max)");
                }
                else
                {
                    sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", sqlType.Length);
                }
            }
            else if (sqlType.Precision != 0)
            {
                if (sqlType.Scale != 0)
                {
                    sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0},{1})", sqlType.Precision, sqlType.Scale);
                }
                else
                {
                    sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", sqlType.Precision);
                }
            }

            return sb.ToString();
        }
    }
}