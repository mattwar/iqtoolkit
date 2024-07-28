using System;
using System.Data;
using System.Data.OleDb;

namespace IQToolkit.Data.OleDb
{
    using Sql;

    public static class OleDbTypeExtensions
    {
        /// <summary>
        /// Get the corresponding <see cref="OleDbType"/>.
        /// </summary>
        public static bool TryGetOleDbType(this SqlType sqlType, out OleDbType oleDbType)
        {
            switch (sqlType)
            {
                case SqlType.BigInt:
                    oleDbType = OleDbType.BigInt;
                    return true;
                case SqlType.Binary:
                    oleDbType = OleDbType.Binary;
                    return true;
                case SqlType.Bit:
                    oleDbType = OleDbType.Boolean;
                    return true;
                case SqlType.Char:
                    oleDbType = OleDbType.Char;
                    return true;
                case SqlType.Date:
                    oleDbType = OleDbType.Date;
                    return true;
                case SqlType.DateTime:
                case SqlType.SmallDateTime:
                case SqlType.DateTime2:
                case SqlType.DateTimeOffset:
                    oleDbType = OleDbType.DBTimeStamp;
                    return true;
                case SqlType.Decimal:
                    oleDbType = OleDbType.Decimal;
                    return true;
                case SqlType.Float:
                case SqlType.Real:
                    oleDbType = OleDbType.Double;
                    return true;
                case SqlType.Image:
                    oleDbType = OleDbType.LongVarBinary;
                    return true;
                case SqlType.Integer:
                    oleDbType = OleDbType.Integer;
                    return true;
                case SqlType.Money:
                case SqlType.SmallMoney:
                    oleDbType = OleDbType.Currency;
                    return true;
                case SqlType.NChar:
                    oleDbType = OleDbType.WChar;
                    return true;
                case SqlType.NText:
                    oleDbType = OleDbType.LongVarChar;
                    return true;
                case SqlType.NVarChar:
                    oleDbType = OleDbType.VarWChar;
                    return true;
                case SqlType.SmallInt:
                    oleDbType = OleDbType.SmallInt;
                    return true;
                case SqlType.Text:
                    oleDbType = OleDbType.LongVarChar;
                    return true;
                case SqlType.Time:
                    oleDbType = OleDbType.DBTime;
                    return true;
                case SqlType.Timestamp:
                    oleDbType = OleDbType.Binary;
                    return true;
                case SqlType.TinyInt:
                    oleDbType = OleDbType.TinyInt;
                    return true;
                case SqlType.Udt:
                    oleDbType = OleDbType.Variant;
                    return true;
                case SqlType.UniqueIdentifier:
                    oleDbType = OleDbType.Guid;
                    return true;
                case SqlType.VarBinary:
                    oleDbType = OleDbType.VarBinary;
                    return true;
                case SqlType.VarChar:
                    oleDbType = OleDbType.VarChar;
                    return true;
                case SqlType.Variant:
                    oleDbType = OleDbType.Variant;
                    return true;
                case SqlType.Xml:
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