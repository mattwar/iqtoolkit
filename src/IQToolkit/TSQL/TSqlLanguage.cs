// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.TSql
{
    using Entities;
    using IQToolkit.Entities.Translation;

    /// <summary>
    /// Microsoft Transact SQL (TSQL) <see cref="QueryLanguage"/>
    /// </summary>
    public sealed class TSqlLanguage : SqlQueryLanguage
    {
        protected override QueryLinguist Linguist { get; }

        public TSqlLanguage()
        {
            this.Linguist = new TSqlLinguist(this);
        }

        public static readonly TSqlLanguage Singleton =
            new TSqlLanguage();

        public override QueryTypeSystem TypeSystem => 
            AnsiSql.AnsiSqlTypeSystem.Singleton;
    }
}