// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Entities;
using IQToolkit.Entities.Translation;
using IQToolkit.Expressions.Sql;
using IQToolkit.Utils;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.TSql
{
    /// <summary>
    /// Microsoft Transact SQL (TSQL) <see cref="QueryLinguist"/>
    /// </summary>
    internal class TSqlLinguist : QueryLinguist
    {
        public TSqlLinguist(TSqlLanguage language)
            : base(language)
        {
        }

        public override Expression Apply(
            Expression expression, 
            QueryMapper mapper, 
            QueryPolice police)
        {
            // fix up any order-by's
            expression = expression.MoveOrderByToOuterSelect(this.Language);

            expression = base.Apply(expression, mapper, police);

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

        public override Expression GetGeneratedIdExpression(MemberInfo member)
        {
            return new ScalarFunctionCallExpression(TypeHelper.GetMemberType(member), "SCOPE_IDENTITY()", null);
        }
    }
}