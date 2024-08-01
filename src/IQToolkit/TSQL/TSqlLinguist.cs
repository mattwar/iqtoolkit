// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Entities.Translation;
using System.Linq.Expressions;

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
    }
}