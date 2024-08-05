// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.TSql
{
    using Entities;
    using Entities.Mapping;
    using Entities.Translation;
    using Expressions.Sql;
    using Utils;

    /// <summary>
    /// Microsoft Transact SQL (TSQL) <see cref="LanguageTranslator"/>
    /// </summary>
    internal class TSqlLinguist : SqlTranslator
    {
        public TSqlLinguist(TSqlLanguage language)
            : base(language)
        {
        }

        public override Expression ApplyLanguageRewrites(
            Expression expression, 
            MappingTranslator mapper, 
            PolicyTranslator police)
        {
            // fix up any order-by's
            expression = expression.MoveOrderByToOuterSelect(this.Language);

            expression = base.ApplyLanguageRewrites(expression, mapper, police);

            // convert skip/take info into RowNumber pattern
            expression = expression.ConvertSkipTakeToTop(this.Language);

            // fix up any order-by's we may have changed
            expression = expression.MoveOrderByToOuterSelect(this.Language);

            return expression;
        }

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

        public override FormattedQuery Format(SqlExpression expression, QueryOptions? options = null)
        {
            return TSqlFormatter.Singleton.Format(expression, options);
        }

        public override Expression GetGeneratedIdExpression(MappedColumnMember member)
        {
            return new ScalarFunctionCallExpression(TypeHelper.GetMemberType(member.Member), "SCOPE_IDENTITY()", null);
        }
    }
}