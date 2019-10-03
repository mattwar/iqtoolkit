// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data.Access
{
    using IQToolkit.Data.Common;

    public class AccessTypeSystem : SqlTypeSystem
    {
        public override int StringDefaultSize
        {
            get { return 2000; }
        }

        public override int BinaryDefaultSize
        {
            get { return 4000; }
        }

        public override QueryType GetQueryType(string typeName, string[] args, bool isNotNull)
        {
            if (String.Compare(typeName, "Memo", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return base.GetQueryType("varchar", new [] {"max"}, isNotNull);
            }

            return base.GetQueryType(typeName, args, isNotNull);
        }

        public override SqlType GetSqlType(string typeName)
        {
            if (string.Compare(typeName, "Memo", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.VarChar;
            }
            else if (string.Compare(typeName, "Currency", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.Decimal;
            }
            else if (string.Compare(typeName, "ReplicationID", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.UniqueIdentifier;
            }
            else if (string.Compare(typeName, "YesNo", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.Bit;
            }
            else if (string.Compare(typeName, "LongInteger", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.BigInt;
            }
            else if (string.Compare(typeName, "VarWChar", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SqlType.NVarChar;
            }
            else
            {
                return base.GetSqlType(typeName);
            }
        }

        public override string Format(QueryType type, bool suppressSize)
        {
            StringBuilder sb = new StringBuilder();
            SqlType sqlType = ((SqlQueryType)type).SqlType;

            switch (sqlType)
            {
                case SqlType.BigInt:
                case SqlType.Bit:
                case SqlType.DateTime:
                case SqlType.Int:
                case SqlType.Money:
                case SqlType.SmallDateTime:
                case SqlType.SmallInt:
                case SqlType.SmallMoney:
                case SqlType.Timestamp:
                case SqlType.TinyInt:
                case SqlType.UniqueIdentifier:
                case SqlType.Variant:
                case SqlType.Xml:
                    sb.Append(sqlType);
                    break;
                case SqlType.Binary:
                case SqlType.Char:
                case SqlType.NChar:
                    sb.Append(sqlType);
                    if (type.Length > 0 && !suppressSize)
                    {
                        sb.Append("(");
                        sb.Append(type.Length);
                        sb.Append(")");
                    }
                    break;
                case SqlType.Image:
                case SqlType.NText:
                case SqlType.NVarChar:
                case SqlType.Text:
                case SqlType.VarBinary:
                case SqlType.VarChar:
                    sb.Append(sqlType);
                    if (type.Length > 0 && !suppressSize)
                    {
                        sb.Append("(");
                        sb.Append(type.Length);
                        sb.Append(")");
                    }
                    break;
                case SqlType.Decimal:
                    sb.Append("Currency");
                    break;
                case SqlType.Float:
                case SqlType.Real:
                    sb.Append(sqlType);  
                    if (type.Precision != 0)
                    {
                        sb.Append("(");
                        sb.Append(type.Precision);
                        if (type.Scale != 0)
                        {
                            sb.Append(",");
                            sb.Append(type.Scale);
                        }
                        sb.Append(")");
                    }
                    break;
            }

            return sb.ToString();
        }
    }
}