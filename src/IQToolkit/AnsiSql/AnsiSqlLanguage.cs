// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.AnsiSql
{
    using Entities;
    using Entities.Translation;
    using Expressions;
    using Expressions.Sql;
    using Utils;

    /// <summary>
    /// ANSI SQL <see cref="QueryLanguage"/>
    /// </summary>
    public sealed class AnsiSqlLanguage : SqlQueryLanguage
    {
        protected override QueryLinguist Linguist { get; }

        public AnsiSqlLanguage()
        {
            this.Linguist = new AnsiSqlLinguist(this);
        }

        public static readonly AnsiSqlLanguage Singleton =
            new AnsiSqlLanguage();

        public override QueryTypeSystem TypeSystem =>
            AnsiSqlTypeSystem.Singleton;
    }
}