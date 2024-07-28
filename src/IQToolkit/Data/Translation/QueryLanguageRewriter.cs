// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    /// <summary>
    /// Applies language specific rules to a query.
    /// </summary>
    public class QueryLanguageRewriter
    {
        public QueryTranslator Translator { get; }
        public QueryLanguage Language { get; }

        /// <summary>
        /// Construct a <see cref="QueryLanguageRewriter"/>
        /// </summary>
        public QueryLanguageRewriter(QueryTranslator translator, QueryLanguage language)
        {
            this.Translator = translator;
            this.Language = language;
        }

        /// <summary>
        /// Applies language-specific translations.
        /// </summary>
        public virtual Expression Rewrite(Expression expression)
        {
            // pre-simplify to help 
            var simplified = expression.SimplifyQueries();

            // convert cross-apply and outer-apply joins into inner & left-outer-joins if possible
            var crossApplied = simplified.ConvertCrossApplyToInnerJoin(this.Language);

            // convert cross joins into inner joins
            var crossJoined = crossApplied.ConvertCrossJoinToInnerJoin();

            return crossJoined;
        }

        /// <summary>
        /// Determine which sub-expressions must be parameters
        /// </summary>
        public virtual Expression Parameterize(Expression expression)
        {
            return ClientParameterRewriter.Rewrite(this.Language, expression);
        }
    }
}