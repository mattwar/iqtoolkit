using System;
using System.Data;
using System.Data.OleDb;

namespace IQToolkit.OleDb
{
    using AnsiSql;

    public static class OleDbTypeExtensions
    {
        /// <summary>
        /// Get the corresponding <see cref="OleDbType"/>.
        /// </summary>
        public static bool TryGetOleDbType(this AnsiSqlType sqlType, out OleDbType oleDbType)
        {
            switch (sqlType)
            {
                case AnsiSqlType.BigInt:
                    oleDbType = OleDbType.BigInt;
                    return true;
                case AnsiSqlType.Binary:
                    oleDbType = OleDbType.Binary;
                    return true;
                case AnsiSqlType.Bit:
                    oleDbType = OleDbType.Boolean;
                    return true;
                case AnsiSqlType.Char:
                    oleDbType = OleDbType.Char;
                    return true;
                case AnsiSqlType.Date:
                    oleDbType = OleDbType.Date;
                    return true;
                case AnsiSqlType.DateTime:
                case AnsiSqlType.SmallDateTime:
                case AnsiSqlType.DateTime2:
                case AnsiSqlType.DateTimeOffset:
                    oleDbType = OleDbType.DBTimeStamp;
                    return true;
                case AnsiSqlType.Decimal:
                    oleDbType = OleDbType.Decimal;
                    return true;
                case AnsiSqlType.Float:
                case AnsiSqlType.Real:
                    oleDbType = OleDbType.Double;
                    return true;
                case AnsiSqlType.Image:
                    oleDbType = OleDbType.LongVarBinary;
                    return true;
                case AnsiSqlType.Integer:
                    oleDbType = OleDbType.Integer;
                    return true;
                case AnsiSqlType.Money:
                case AnsiSqlType.SmallMoney:
                    oleDbType = OleDbType.Currency;
                    return true;
                case AnsiSqlType.NChar:
                    oleDbType = OleDbType.WChar;
                    return true;
                case AnsiSqlType.NText:
                    oleDbType = OleDbType.LongVarChar;
                    return true;
                case AnsiSqlType.NVarChar:
                    oleDbType = OleDbType.VarWChar;
                    return true;
                case AnsiSqlType.SmallInt:
                    oleDbType = OleDbType.SmallInt;
                    return true;
                case AnsiSqlType.Text:
                    oleDbType = OleDbType.LongVarChar;
                    return true;
                case AnsiSqlType.Time:
                    oleDbType = OleDbType.DBTime;
                    return true;
                case AnsiSqlType.Timestamp:
                    oleDbType = OleDbType.Binary;
                    return true;
                case AnsiSqlType.TinyInt:
                    oleDbType = OleDbType.TinyInt;
                    return true;
                case AnsiSqlType.Udt:
                    oleDbType = OleDbType.Variant;
                    return true;
                case AnsiSqlType.UniqueIdentifier:
                    oleDbType = OleDbType.Guid;
                    return true;
                case AnsiSqlType.VarBinary:
                    oleDbType = OleDbType.VarBinary;
                    return true;
                case AnsiSqlType.VarChar:
                    oleDbType = OleDbType.VarChar;
                    return true;
                case AnsiSqlType.Variant:
                    oleDbType = OleDbType.Variant;
                    return true;
                case AnsiSqlType.Xml:
                    oleDbType = OleDbType.VarWChar;
                    return true;
                default:
                    oleDbType = default;
                    return false;
            }
        }

        /// <summary>
        /// Get the corresponding <see cref="OleDbType"/>.
        /// </summary>
        public static bool TryGetOleDbType(this DbType dbType, out OleDbType type)
        {
            switch (dbType)
            {
                case DbType.AnsiString:
                    type = OleDbType.VarChar;
                    return true;
                case DbType.AnsiStringFixedLength:
                    type = OleDbType.Char;
                    return true;
                case DbType.Binary:
                    type = OleDbType.Binary;
                    return true;
                case DbType.Boolean:
                    type = OleDbType.Boolean;
                    return true;
                case DbType.Byte:
                    type = OleDbType.UnsignedTinyInt;
                    return true;
                case DbType.Currency:
                    type = OleDbType.Currency;
                    return true;
                case DbType.Date:
                    type = OleDbType.Date;
                    return true;
                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                    type = OleDbType.DBTimeStamp;
                    return true;
                case DbType.Decimal:
                    type = OleDbType.Decimal;
                    return true;
                case DbType.Double:
                    type = OleDbType.Double;
                    return true;
                case DbType.Guid:
                    type = OleDbType.Guid;
                    return true;
                case DbType.Int16:
                    type = OleDbType.SmallInt;
                    return true;
                case DbType.Int32:
                    type = OleDbType.Integer;
                    return true;
                case DbType.Int64:
                    type = OleDbType.BigInt;
                    return true;
                case DbType.Object:
                    type = OleDbType.Variant;
                    return true;
                case DbType.SByte:
                    type = OleDbType.TinyInt;
                    return true;
                case DbType.Single:
                    type = OleDbType.Single;
                    return true;
                case DbType.String:
                    type = OleDbType.VarWChar;
                    return true;
                case DbType.StringFixedLength:
                    type = OleDbType.WChar;
                    return true;
                case DbType.Time:
                    type = OleDbType.DBTime;
                    return true;
                case DbType.UInt16:
                    type = OleDbType.UnsignedSmallInt;
                    return true;
                case DbType.UInt32:
                    type = OleDbType.UnsignedInt;
                    return true;
                case DbType.UInt64:
                    type = OleDbType.UnsignedBigInt;
                    return true;
                case DbType.VarNumeric:
                    type = OleDbType.Numeric;
                    return true;
                case DbType.Xml:
                    type = OleDbType.VarWChar;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }
    }
}