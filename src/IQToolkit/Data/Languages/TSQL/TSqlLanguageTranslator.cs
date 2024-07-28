// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Data.Translation;
using System.Linq.Expressions;

namespace IQToolkit.Data.TSql
{
    /// <summary>
    /// Microsoft Transact SQL (TSQL) <see cref="QueryLanguageRewriter"/>
    /// </summary>
    internal class TSqlLanguageTranslator : QueryLanguageRewriter
    {
        public TSqlLanguageTranslator(QueryTranslator translator, TSqlLanguage language)
            : base(translator, language)
        {
        }

        public override Expression Rewrite(Expression expression)
        {
            // fix up any order-by's
            expression = expression.MoveOrderByToOuterSelect(this.Language);

            expression = base.Rewrite(expression);

            // convert skip/take info into RowNumber pattern
            expression = expression.ConvertSkipTakeToTop(this.Language);

            // fix up any order-by's we may have changed
            expression = expression.MoveOrderByToOuterSelect(this.Language);

            return expression;
        }
    }
}