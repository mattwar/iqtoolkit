using System;
using System.Data.Odbc;

namespace IQToolkit.Odbc
{
    using AnsiSql;

    public static class OdbcTypeExtensions
    {
        /// <summary>
        /// Gets the corresponding <see cref="AnsiSqlType"/>.
        /// </summary>
        public static bool TryGetSqlType(this OdbcType odbcType, out AnsiSqlType sqlType)
        {
            switch (odbcType)
            {
                case OdbcType.BigInt:
                    sqlType = AnsiSqlType.BigInt;
                    return true;
                case OdbcType.Binary:
                    sqlType = AnsiSqlType.Binary;
                    return true;
                case OdbcType.Bit:
                    sqlType = AnsiSqlType.Bit;
                    return true;
                case OdbcType.Char:
                    sqlType = AnsiSqlType.Char;
                    return true;
                case OdbcType.Date:
                    sqlType = AnsiSqlType.Date;
                    return true;
                case OdbcType.DateTime:
                    sqlType = AnsiSqlType.DateTime;
                    return true;
                case OdbcType.Decimal:
                    sqlType = AnsiSqlType.Decimal;
                    return true;
                case OdbcType.Double:
                    sqlType = AnsiSqlType.Float;
                    return true;
                case OdbcType.Image:
                    sqlType = AnsiSqlType.Image;
                    return true;
                case OdbcType.Int:
                    sqlType = AnsiSqlType.Integer;
                    return true;
                case OdbcType.NChar:
                    sqlType = AnsiSqlType.NChar;
                    return true;
                case OdbcType.NText:
                    sqlType = AnsiSqlType.NText;
                    return true;
                case OdbcType.NVarChar:
                    sqlType = AnsiSqlType.NVarChar;
                    return true;
                case OdbcType.Real:
                    sqlType = AnsiSqlType.Real;
                    return true;
                case OdbcType.SmallDateTime:
                    sqlType = AnsiSqlType.SmallDateTime;
                    return true;
                case OdbcType.SmallInt:
                    sqlType = AnsiSqlType.SmallInt;
                    return true;
                case OdbcType.Text:
                    sqlType = AnsiSqlType.Text;
                    return true;
                case OdbcType.Time:
                    sqlType = AnsiSqlType.Time;
                    return true;
                case OdbcType.Timestamp:
                    sqlType = AnsiSqlType.Timestamp;
                    return true;
                case OdbcType.TinyInt:
                    sqlType = AnsiSqlType.TinyInt;
                    return true;
                case OdbcType.UniqueIdentifier:
                    sqlType = AnsiSqlType.UniqueIdentifier;
                    return true;
                case OdbcType.VarBinary:
                    sqlType = AnsiSqlType.VarBinary;
                    return true;
                case OdbcType.VarChar:
                    sqlType = AnsiSqlType.VarChar;
                    return true;
                default:
                    sqlType = default;
                    return false;
            }
        }

        /// <summary>
        /// Gets the correspondg <see cref="OdbcType"/>.
        /// </summary>
        public static bool TryGetOdbcType(this AnsiSqlType sqlType, out OdbcType odbcType)
        {
            switch (sqlType)
            {
                case AnsiSqlType.BigInt:
                    odbcType = OdbcType.BigInt;
                    return true;
                case AnsiSqlType.Binary:
                    odbcType = OdbcType.Binary;
                    return true;
                case AnsiSqlType.Bit:
                    odbcType = OdbcType.Bit;
                    return true;
                case AnsiSqlType.Char:
                    odbcType = OdbcType.Char;
                    return true;
                case AnsiSqlType.DateTime:
                case AnsiSqlType.DateTime2:
                case AnsiSqlType.DateTimeOffset:
                    odbcType = OdbcType.DateTime;
                    return true;
                case AnsiSqlType.Decimal:
                case AnsiSqlType.Money:
                case AnsiSqlType.SmallMoney:
                    odbcType = OdbcType.Decimal;
                    return true;
                case AnsiSqlType.Real:
                    odbcType = OdbcType.Real;
                    return true;
                case AnsiSqlType.Float:
                    odbcType = OdbcType.Double;
                    return true;
                case AnsiSqlType.Image:
                    odbcType = OdbcType.Image;
                    return true;
                case AnsiSqlType.Integer:
                    odbcType = OdbcType.Int;
                    return true;
                case AnsiSqlType.NChar:
                    odbcType = OdbcType.NChar;
                    return true;
                case AnsiSqlType.NText:
                    odbcType = OdbcType.NText;
                    return true;
                case AnsiSqlType.NVarChar:
                    odbcType = OdbcType.NVarChar;
                    return true;
                case AnsiSqlType.UniqueIdentifier:
                    odbcType = OdbcType.UniqueIdentifier;
                    return true;
                case AnsiSqlType.SmallDateTime:
                    odbcType = OdbcType.SmallDateTime;
                    return true;
                case AnsiSqlType.SmallInt:
                    odbcType = OdbcType.SmallInt;
                    return true;
                case AnsiSqlType.Text:
                    odbcType = OdbcType.Text;
                    return true;
                case AnsiSqlType.Timestamp:
                    odbcType = OdbcType.Timestamp;
                    return true;
                case AnsiSqlType.TinyInt:
                    odbcType = OdbcType.TinyInt;
                    return true;
                case AnsiSqlType.VarBinary:
                    odbcType = OdbcType.VarBinary;
                    return true;
                case AnsiSqlType.VarChar:
                    odbcType = OdbcType.VarChar;
                    return true;
                case AnsiSqlType.Date:
                    odbcType = OdbcType.Date;
                    return true;
                case AnsiSqlType.Time:
                    odbcType = OdbcType.Time;
                    return true;
                case AnsiSqlType.Variant:
                case AnsiSqlType.Xml:
                case AnsiSqlType.Udt:
                case AnsiSqlType.Structured:
                default:
                    odbcType = default;
                    return false;
            }
        }
    }
}
