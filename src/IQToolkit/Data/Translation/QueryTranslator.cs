// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Translation
{
    /// <summary>
    /// Translates query expressions by applying rules supplied by 
    /// a <see cref="LanguageRewriter"/>, <see cref="Mapping"/> and <see cref="PolicyRewriter"/>. 
    /// </summary>
    public class QueryTranslator
    {
        public QueryLanguageRewriter LanguageRewriter { get; }
        public QueryMappingRewriter MappingRewriter { get; }
        public QueryPolicyRewriter PolicyRewriter { get; }

        public QueryLanguage Language => this.LanguageRewriter.Language;
        public EntityMapping Mapping => this.MappingRewriter.Mapping;
        public QueryPolicy Policy => this.PolicyRewriter.Policy;

        /// <summary>
        /// Constructs a new <see cref="QueryTranslator"/>.
        /// </summary>
        public QueryTranslator(
            Func<QueryTranslator, QueryLanguageRewriter> fnLinguist,
            Func<QueryTranslator, QueryMappingRewriter> fnMapper,
            Func<QueryTranslator, QueryPolicyRewriter> fnPolice)
        {
            this.LanguageRewriter = fnLinguist(this);
            this.MappingRewriter = fnMapper(this);
            this.PolicyRewriter = fnPolice(this);
        }

        /// <summary>
        /// Translates a query expression.
        /// </summary>
        public virtual Expression Translate(Expression expression)
        {
            // pre-evaluate local sub-trees
            var evaluated = PartialEvaluator.Eval(expression, this.Mapping.CanBeEvaluatedLocally);

            // apply mapping (binds LINQ operators too)
            var mapped = this.MappingRewriter.Rewrite(evaluated);

            // any policy specific translations or validations
            var policed = this.PolicyRewriter.Rewrite(mapped);

            // any language specific translations or validations
            var translated = this.LanguageRewriter.Rewrite(policed);

            return translated;
        }
    }
}