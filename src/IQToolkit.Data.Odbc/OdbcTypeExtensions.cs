using System;
using System.Data.Odbc;

namespace IQToolkit.Data.Odbc
{
    using Sql;

    public static class OdbcTypeExtensions
    {
        /// <summary>
        /// Gets the corresponding <see cref="SqlType"/>.
        /// </summary>
        public static bool TryGetSqlType(this OdbcType odbcType, out SqlType sqlType)
        {
            switch (odbcType)
            {
                case OdbcType.BigInt:
                    sqlType = SqlType.BigInt;
                    return true;
                case OdbcType.Binary:
                    sqlType = SqlType.Binary;
                    return true;
                case OdbcType.Bit:
                    sqlType = SqlType.Bit;
                    return true;
                case OdbcType.Char:
                    sqlType = SqlType.Char;
                    return true;
                case OdbcType.Date:
                    sqlType = SqlType.Date;
                    return true;
                case OdbcType.DateTime:
                    sqlType = SqlType.DateTime;
                    return true;
                case OdbcType.Decimal:
                    sqlType = SqlType.Decimal;
                    return true;
                case OdbcType.Double:
                    sqlType = SqlType.Float;
                    return true;
                case OdbcType.Image:
                    sqlType = SqlType.Image;
                    return true;
                case OdbcType.Int:
                    sqlType = SqlType.Integer;
                    return true;
                case OdbcType.NChar:
                    sqlType = SqlType.NChar;
                    return true;
                case OdbcType.NText:
                    sqlType = SqlType.NText;
                    return true;
                case OdbcType.NVarChar:
                    sqlType = SqlType.NVarChar;
                    return true;
                case OdbcType.Real:
                    sqlType = SqlType.Real;
                    return true;
                case OdbcType.SmallDateTime:
                    sqlType = SqlType.SmallDateTime;
                    return true;
                case OdbcType.SmallInt:
                    sqlType = SqlType.SmallInt;
                    return true;
                case OdbcType.Text:
                    sqlType = SqlType.Text;
                    return true;
                case OdbcType.Time:
                    sqlType = SqlType.Time;
                    return true;
                case OdbcType.Timestamp:
                    sqlType = SqlType.Timestamp;
                    return true;
                case OdbcType.TinyInt:
                    sqlType = SqlType.TinyInt;
                    return true;
                case OdbcType.UniqueIdentifier:
                    sqlType = SqlType.UniqueIdentifier;
                    return true;
                case OdbcType.VarBinary:
                    sqlType = SqlType.VarBinary;
                    return true;
                case OdbcType.VarChar:
                    sqlType = SqlType.VarChar;
                    return true;
                default:
                    sqlType = default;
                    return false;
            }
        }

        /// <summary>
        /// Gets the correspondg <see cref="OdbcType"/>.
        /// </summary>
        public static bool TryGetOdbcType(this SqlType sqlType, out OdbcType odbcType)
        {
            switch (sqlType)
            {
                case SqlType.BigInt:
                    odbcType = OdbcType.BigInt;
                    return true;
                case SqlType.Binary:
                    odbcType = OdbcType.Binary;
                    return true;
                case SqlType.Bit:
                    odbcType = OdbcType.Bit;
                    return true;
                case SqlType.Char:
                    odbcType = OdbcType.Char;
                    return true;
                case SqlType.DateTime:
                case SqlType.DateTime2:
                case SqlType.DateTimeOffset:
                    odbcType = OdbcType.DateTime;
                    return true;
                case SqlType.Decimal:
                case SqlType.Money:
                case SqlType.SmallMoney:
                    odbcType = OdbcType.Decimal;
                    return true;
                case SqlType.Real:
                    odbcType = OdbcType.Real;
                    return true;
                case SqlType.Float:
                    odbcType = OdbcType.Double;
                    return true;
                case SqlType.Image:
                    odbcType = OdbcType.Image;
                    return true;
                case SqlType.Integer:
                    odbcType = OdbcType.Int;
                    return true;
                case SqlType.NChar:
                    odbcType = OdbcType.NChar;
                    return true;
                case SqlType.NText:
                    odbcType = OdbcType.NText;
                    return true;
                case SqlType.NVarChar:
                    odbcType = OdbcType.NVarChar;
                    return true;
                case SqlType.UniqueIdentifier:
                    odbcType = OdbcType.UniqueIdentifier;
                    return true;
                case SqlType.SmallDateTime:
                    odbcType = OdbcType.SmallDateTime;
                    return true;
                case SqlType.SmallInt:
                    odbcType = OdbcType.SmallInt;
                    return true;
                case SqlType.Text:
                    odbcType = OdbcType.Text;
                    return true;
                case SqlType.Timestamp:
                    odbcType = OdbcType.Timestamp;
                    return true;
                case SqlType.TinyInt:
                    odbcType = OdbcType.TinyInt;
                    return true;
                case SqlType.VarBinary:
                    odbcType = OdbcType.VarBinary;
                    return true;
                case SqlType.VarChar:
                    odbcType = OdbcType.VarChar;
                    return true;
                case SqlType.Date:
                    odbcType = OdbcType.Date;
                    return true;
                case SqlType.Time:
                    odbcType = OdbcType.Time;
                    return true;
                case SqlType.Variant:
                case SqlType.Xml:
                case SqlType.Udt:
                case SqlType.Structured:
                default:
                    odbcType = default;
                    return false;
            }
        }
    }
}
