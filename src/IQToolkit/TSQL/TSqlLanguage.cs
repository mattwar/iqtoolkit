// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.TSql
{
    using Entities;
    using Expressions;
    using Expressions.Sql;
    using IQToolkit.Entities.Translation;
    using Utils;

    /// <summary>
    /// Microsoft Transact SQL (TSQL) <see cref="QueryLanguage"/>
    /// </summary>
    public sealed class TSqlLanguage : QueryLanguage, ICreateLanguageRewriter
    {
        public TSqlLanguage()
        {
        }

        public static readonly TSqlLanguage Singleton =
            new TSqlLanguage();

        public override QueryTypeSystem TypeSystem => 
            AnsiSql.AnsiSqlTypeSystem.Singleton;

        public override QueryFormatter Formatter =>
            TSqlFormatter.Singleton;

        QueryLanguageRewriter ICreateLanguageRewriter.CreateLanguageTranslator(QueryTranslator translator) =>
            new TSqlLanguageRewriter(translator, this);

        public override string Quote(string name)
        {
            if (name.StartsWith("[") && name.EndsWith("]"))
            {
                return name;
            }
            else if (name.IndexOf('.') > 0)
            {
                return "[" + string.Join("].[", name.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)) + "]";
            }
            else
            {
                return "[" + name + "]";
            }
        }

        private static readonly char[] splitChars = new char[] { '.' };

        public override bool AllowsMultipleCommands
        {
            get { return true; }
        }

        public override bool AllowSubqueryInSelectWithoutFrom
        {
            get { return true; }
        }

        public override bool AllowDistinctInAggregates
        {
            get { return true; }
        }

        public override Expression GetGeneratedIdExpression(MemberInfo member)
        {
            return new ScalarFunctionCallExpression(TypeHelper.GetMemberType(member), "SCOPE_IDENTITY()", null);
        }
    }
}