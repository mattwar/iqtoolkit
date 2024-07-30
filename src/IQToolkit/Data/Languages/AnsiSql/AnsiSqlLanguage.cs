// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.AnsiSql
{
    using Expressions;
    using Utils;

    /// <summary>
    /// ANSI SQL <see cref="QueryLanguage"/>
    /// </summary>
    public sealed class AnsiSqlLanguage : QueryLanguage
    {
        public AnsiSqlLanguage()
        {
        }

        public static readonly AnsiSqlLanguage Singleton =
            new AnsiSqlLanguage();

        public override QueryTypeSystem TypeSystem =>
            AnsiSqlTypeSystem.Singleton;

        public override QueryFormatter Formatter =>
            AnsiSqlFormatter.Default;
       
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

        public override Expression GetGeneratedIdExpression(MemberInfo member)
        {
            return new DbFunctionCallExpression(TypeHelper.GetMemberType(member), "@@IDENTITY");
        }
    }
}