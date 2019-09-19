using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace IQToolkit.Data.OleDb
{
    using IQToolkit.Data.Common;

    /// <summary>
    /// A base <see cref="DbEntityProvider"/> for OLEDB database providers
    /// </summary>
    public abstract class OleDbQueryProvider : DbEntityProvider
    {
        public OleDbQueryProvider(OleDbConnection connection, QueryLanguage language, QueryMapping mapping, QueryPolicy policy)
            : base(connection, language, mapping, policy)
        {
        }

        protected override QueryExecutor CreateExecutor()
        {
            return new Executor(this);
        }

        public new class Executor : DbEntityProvider.Executor
        {
            OleDbQueryProvider provider;

            public Executor(OleDbQueryProvider provider)
                : base(provider)
            {
                this.provider = provider;
            }

            protected override void AddParameter(DbCommand command, QueryParameter parameter, object value)
            {
                QueryType qt = parameter.QueryType;
                if (qt == null)
                {
                    qt = this.provider.Language.TypeSystem.GetColumnType(parameter.Type);
                }

                var p = ((OleDbCommand)command).Parameters.Add(parameter.Name, this.GetOleDbType(qt), qt.Length);
                if (qt.Precision != 0)
                {
                    p.Precision = (byte)qt.Precision;
                }

                if (qt.Scale != 0)
                {
                    p.Scale = (byte)qt.Scale;
                }

                p.Value = value ?? DBNull.Value;
            }

            protected virtual OleDbType GetOleDbType(QueryType type)
            {
                return ToOleDbType(((SqlQueryType)type).SqlType);
            }
        }

        public static OleDbType ToOleDbType(SqlType dbType)
        {
            switch (dbType)
            {
                case SqlType.BigInt:
                    return OleDbType.BigInt;
                case SqlType.Binary:
                    return OleDbType.Binary;
                case SqlType.Bit:
                    return OleDbType.Boolean;
                case SqlType.Char:
                    return OleDbType.Char;
                case SqlType.Date:
                    return OleDbType.Date;
                case SqlType.DateTime:
                case SqlType.SmallDateTime:
                case SqlType.DateTime2:
                case SqlType.DateTimeOffset:
                    return OleDbType.DBTimeStamp;
                case SqlType.Decimal:
                    return OleDbType.Decimal;
                case SqlType.Float:
                case SqlType.Real:
                    return OleDbType.Double;
                case SqlType.Image:
                    return OleDbType.LongVarBinary;
                case SqlType.Int:
                    return OleDbType.Integer;
                case SqlType.Money:
                case SqlType.SmallMoney:
                    return OleDbType.Currency;
                case SqlType.NChar:
                    return OleDbType.WChar;
                case SqlType.NText:
                    return OleDbType.LongVarChar;
                case SqlType.NVarChar:
                    return OleDbType.VarWChar;
                case SqlType.SmallInt:
                    return OleDbType.SmallInt;
                case SqlType.Text:
                    return OleDbType.LongVarChar;
                case SqlType.Time:
                    return OleDbType.DBTime;
                case SqlType.Timestamp:
                    return OleDbType.Binary;
                case SqlType.TinyInt:
                    return OleDbType.TinyInt;
                case SqlType.Udt:
                    return OleDbType.Variant;
                case SqlType.UniqueIdentifier:
                    return OleDbType.Guid;
                case SqlType.VarBinary:
                    return OleDbType.VarBinary;
                case SqlType.VarChar:
                    return OleDbType.VarChar;
                case SqlType.Variant:
                    return OleDbType.Variant;
                case SqlType.Xml:
                    return OleDbType.VarWChar;
                default:
                    throw new InvalidOperationException(string.Format("Unhandled sql type: {0}", dbType));
            }
        }

        public static OleDbType ToOleDbType(DbType type)
        {
            switch (type)
            {
                case DbType.AnsiString:
                    return OleDbType.VarChar;
                case DbType.AnsiStringFixedLength:
                    return OleDbType.Char;
                case DbType.Binary:
                    return OleDbType.Binary;
                case DbType.Boolean:
                    return OleDbType.Boolean;
                case DbType.Byte:
                    return OleDbType.UnsignedTinyInt;
                case DbType.Currency:
                    return OleDbType.Currency;
                case DbType.Date:
                    return OleDbType.Date;
                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                    return OleDbType.DBTimeStamp;
                case DbType.Decimal:
                    return OleDbType.Decimal;
                case DbType.Double:
                    return OleDbType.Double;
                case DbType.Guid:
                    return OleDbType.Guid;
                case DbType.Int16:
                    return OleDbType.SmallInt;
                case DbType.Int32:
                    return OleDbType.Integer;
                case DbType.Int64:
                    return OleDbType.BigInt;
                case DbType.Object:
                    return OleDbType.Variant;
                case DbType.SByte:
                    return OleDbType.TinyInt;
                case DbType.Single:
                    return OleDbType.Single;
                case DbType.String:
                    return OleDbType.VarWChar;
                case DbType.StringFixedLength:
                    return OleDbType.WChar;
                case DbType.Time:
                    return OleDbType.DBTime;
                case DbType.UInt16:
                    return OleDbType.UnsignedSmallInt;
                case DbType.UInt32:
                    return OleDbType.UnsignedInt;
                case DbType.UInt64:
                    return OleDbType.UnsignedBigInt;
                case DbType.VarNumeric:
                    return OleDbType.Numeric;
                case DbType.Xml:
                    return OleDbType.VarWChar;
                default:
                    throw new InvalidOperationException(string.Format("Unhandled db type '{0}'.", type));
            }
        }
    }
}