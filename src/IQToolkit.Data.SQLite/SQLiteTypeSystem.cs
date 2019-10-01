using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace IQToolkit.Data.SQLite
{
    using IQToolkit.Data.Common;

    public class SQLiteTypeSystem : SqlTypeSystem
    {
        public override SqlType GetSqlType(string typeName)
        {
            if (string.Compare(typeName, "TEXT", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(typeName, "CHAR", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(typeName, "CLOB", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(typeName, "VARYINGCHARACTER", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(typeName, "NATIONALVARYINGCHARACTER", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.VarChar;
            }
            else if (string.Compare(typeName, "INT", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(typeName, "INTEGER", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.BigInt;
            }
            else if (string.Compare(typeName, "BLOB", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.Binary;
            }
            else if (string.Compare(typeName, "BOOLEAN", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.Bit;
            }
            else if (string.Compare(typeName, "NUMERIC", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.Decimal;
            }
            else
            {
                return base.GetSqlType(typeName);
            }
        }

        public override string Format(QueryType type, bool suppressSize)
        {
            StringBuilder sb = new StringBuilder();
            SqlQueryType sqlType = (SqlQueryType)type;
            SqlType sqlDbType = sqlType.SqlType;

            switch (sqlDbType)
            {
                case SqlType.BigInt:
                case SqlType.SmallInt:
                case SqlType.Int:
                case SqlType.TinyInt:
                    sb.Append("INTEGER");
                    break;
                case SqlType.Bit:
                    sb.Append("BOOLEAN");
                    break;
                case SqlType.SmallDateTime:
                    sb.Append("DATETIME");
                    break;
                case SqlType.Char:
                case SqlType.NChar:
                    sb.Append("CHAR");
                    if (type.Length > 0 && !suppressSize)
                    {
                        sb.Append("(");
                        sb.Append(type.Length);
                        sb.Append(")");
                    }
                    break;
                case SqlType.Variant:
                case SqlType.Binary:
                case SqlType.Image:
                case SqlType.UniqueIdentifier: //There is a setting to make it string, look at later
                    sb.Append("BLOB");
                    if (type.Length > 0 && !suppressSize)
                    {
                        sb.Append("(");
                        sb.Append(type.Length);
                        sb.Append(")");
                    }
                    break;
                case SqlType.Xml:
                case SqlType.NText:
                case SqlType.NVarChar:
                case SqlType.Text:
                case SqlType.VarBinary:
                case SqlType.VarChar:
                    sb.Append("TEXT");
                    if (type.Length > 0 && !suppressSize)
                    {
                        sb.Append("(");
                        sb.Append(type.Length);
                        sb.Append(")");
                    }
                    break;
                case SqlType.Decimal:
                case SqlType.Money:
                case SqlType.SmallMoney:
                    sb.Append("NUMERIC");
                    if (type.Precision != 0)
                    {
                        sb.Append("(");
                        sb.Append(type.Precision);
                        sb.Append(")");
                    }
                    break;
                case SqlType.Float:
                case SqlType.Real:
                    sb.Append("FLOAT");
                    if (type.Precision != 0)
                    {
                        sb.Append("(");
                        sb.Append(type.Precision);
                        sb.Append(")");
                    }
                    break;
                case SqlType.Date:
                case SqlType.DateTime:
                case SqlType.Timestamp:
                default:
                    sb.Append(sqlDbType);
                    break;
            }
            return sb.ToString();
        }
    }
}
