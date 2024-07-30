// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data.AnsiSql
{
    using Utils;

    /// <summary>
    /// A <see cref="QueryTypeSystem"/> for types based on <see cref="AnsiSqlType"/>.
    /// Default parser, format implementations assume a type syntax similar to TSQL.
    /// </summary>
    public class AnsiSqlTypeSystem : QueryTypeSystem
    {
        protected AnsiSqlTypeSystem()
        {
        }

        public static AnsiSqlTypeSystem Singleton = new AnsiSqlTypeSystem();

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
        public virtual QueryType GetQueryType(string typeName, string[]? args, bool isNotNull)
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

            if (!this.TryGetSqlType(typeName, out var sqlType))
            {
                throw new InvalidOperationException(
                    string.Format("The type name '{0}' is not a valid SQL type name", typeName)
                    );
            }

            int length = 0;
            short precision = 0;
            short scale = 0;

            switch (sqlType)
            {
                case AnsiSqlType.Binary:
                case AnsiSqlType.Char:
                case AnsiSqlType.Image:
                case AnsiSqlType.NChar:
                case AnsiSqlType.NVarChar:
                case AnsiSqlType.VarBinary:
                case AnsiSqlType.VarChar:
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
                case AnsiSqlType.Money:
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
                case AnsiSqlType.Decimal:
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
                case AnsiSqlType.Float:
                case AnsiSqlType.Real:
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

            return NewType(sqlType, isNotNull, length, precision, scale);
        }

        /// <summary>
        /// Construct a new <see cref="QueryType"/> instance from 
        /// </summary>
        protected virtual QueryType NewType(AnsiSqlType type, bool isNotNull, int length, short precision, short scale)
        {
            return new AnsiSqlQueryType(this, type, isNotNull, length, precision, scale);
        }

        /// <summary>
        /// Gets the <see cref="AnsiSqlType"/> given the type name (same name as <see cref="AnsiSqlType"/> members)
        /// </summary>
        public virtual bool TryGetSqlType(string typeName, out AnsiSqlType type)
        {
            return Enum.TryParse(typeName, true, out type);
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
        public override QueryType GetQueryType(Type type)
        {
            bool isNotNull = type.GetTypeInfo().IsValueType && !TypeHelper.IsNullableType(type);
            type = TypeHelper.GetNonNullableType(type);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return NewType(AnsiSqlType.Bit, isNotNull, 0, 0, 0);
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return NewType(AnsiSqlType.TinyInt, isNotNull, 0, 0, 0);
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return NewType(AnsiSqlType.SmallInt, isNotNull, 0, 0, 0);
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return NewType(AnsiSqlType.Integer, isNotNull, 0, 0, 0);
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return NewType(AnsiSqlType.BigInt, isNotNull, 0, 0, 0);
                case TypeCode.Single:
                case TypeCode.Double:
                    return NewType(AnsiSqlType.Float, isNotNull, 0, 0, 0);
                case TypeCode.String:
                    return NewType(AnsiSqlType.NVarChar, isNotNull, this.StringDefaultSize, 0, 0);
                case TypeCode.Char:
                    return NewType(AnsiSqlType.NChar, isNotNull, 1, 0, 0);
                case TypeCode.DateTime:
                    return NewType(AnsiSqlType.DateTime, isNotNull, 0, 0, 0);
                case TypeCode.Decimal:
                    return NewType(AnsiSqlType.Decimal, isNotNull, 0, 29, 4);
                default:
                    if (type == typeof(byte[]))
                        return NewType(AnsiSqlType.VarBinary, isNotNull, this.BinaryDefaultSize, 0, 0);
                    else if (type == typeof(Guid))
                        return NewType(AnsiSqlType.UniqueIdentifier, isNotNull, 0, 0, 0);
                    else if (type == typeof(DateTimeOffset))
                        return NewType(AnsiSqlType.DateTimeOffset, isNotNull, 0, 0, 0);
                    else if (type == typeof(TimeSpan))
                        return NewType(AnsiSqlType.Time, isNotNull, 0, 0, 0);
                    else if (type.GetTypeInfo().IsEnum)
                        return NewType(AnsiSqlType.Integer, isNotNull, 0, 0, 0);
                    else
                        return QueryType.Unknown;
            }
        }

        /// <summary>
        /// True if the <see cref="AnsiSqlType"/> is a variable length type.
        /// </summary>
        public static bool IsVariableLength(AnsiSqlType dbType)
        {
            switch (dbType)
            {
                case AnsiSqlType.Image:
                case AnsiSqlType.NText:
                case AnsiSqlType.NVarChar:
                case AnsiSqlType.Text:
                case AnsiSqlType.VarBinary:
                case AnsiSqlType.VarChar:
                case AnsiSqlType.Xml:
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
            var sqlType = (AnsiSqlQueryType)type;
            StringBuilder sb = new StringBuilder();
            sb.Append(sqlType.Type.ToString().ToUpper());

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