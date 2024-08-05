// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.AnsiSql
{
    using Expressions.Sql;
    using Entities;
    using Entities.Mapping;
    using Entities.Translation;
    using Utils;

    internal class AnsiSqlLinguist : SqlTranslator
    {
        public AnsiSqlLinguist(QueryLanguage language)
            : base(language)
        {
        }

        public override FormattedQuery Format(SqlExpression expression, QueryOptions? options = null)
        {
            return AnsiSqlFormatter.Default.Format(expression, options);
        }

        public override Expression GetGeneratedIdExpression(MappedColumnMember member)
        {
            return new ScalarFunctionCallExpression(TypeHelper.GetMemberType(member.Member), "@@IDENTITY");
        }
    }
}