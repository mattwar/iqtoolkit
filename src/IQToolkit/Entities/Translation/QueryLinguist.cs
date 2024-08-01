// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Entities.Translation
{
    /// <summary>
    /// Applies language specific rules to a query.
    /// </summary>
    public class QueryLinguist
    {
        public QueryLanguage Language { get; }

        /// <summary>
        /// Construct a <see cref="QueryLinguist"/>
        /// </summary>
        public QueryLinguist(QueryLanguage language)
        {
            this.Language = language;
        }

        /// <summary>
        /// Apply additional language rewrites.
        /// </summary>
        public virtual Expression Apply(
            Expression expression,
            QueryMapper mapper,
            QueryPolice police)
        {
            // pre-simplify to help 
            var simplified = expression.SimplifyQueries();

            // convert cross-apply and outer-apply joins into inner & left-outer-joins if possible
            var crossApplied = simplified.ConvertCrossApplyToInnerJoin(this.Language);

            // convert cross joins into inner joins
            var crossJoined = crossApplied.ConvertCrossJoinToInnerJoin();

            return crossJoined;
        }
    }
}