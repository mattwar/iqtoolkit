// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.AnsiSql
{
    using Entities;
    using Entities.Translation;
    using IQToolkit.Expressions.Sql;
    using IQToolkit.Utils;
    using System.Reflection;

    internal class AnsiSqlLinguist : QueryLinguist
    {
        public AnsiSqlLinguist(QueryLanguage language)
            : base(language)
        {
        }

        public override FormattedQuery Format(SqlExpression expression, QueryOptions? options = null)
        {
            return AnsiSqlFormatter.Default.Format(expression, options);
        }

        public override Expression GetGeneratedIdExpression(MemberInfo member)
        {
            return new ScalarFunctionCallExpression(TypeHelper.GetMemberType(member), "@@IDENTITY");
        }
    }
}