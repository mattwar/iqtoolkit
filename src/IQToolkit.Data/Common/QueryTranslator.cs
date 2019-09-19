// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Translates query expressions by applying rules supplied by 
    /// a <see cref="Linguist"/>, <see cref="Mapping"/> and <see cref="Police"/>. 
    /// </summary>
    public class QueryTranslator
    {
        public QueryLinguist Linguist { get; }
        public QueryMapper Mapper { get; }
        public QueryPolice Police { get; }

        /// <summary>
        /// Constructs a new <see cref="QueryTranslator"/>.
        /// </summary>
        public QueryTranslator(QueryLanguage language, QueryMapping mapping, QueryPolicy policy)
        {
            this.Linguist = language.CreateLinguist(this);
            this.Mapper = mapping.CreateMapper(this);
            this.Police = policy.CreatePolice(this);
        }

        /// <summary>
        /// Translates a query expression using rules defined by the <see cref="Linguist"/>, <see cref="Mapping"/> and <see cref="Police"/>.
        /// </summary>
        public virtual Expression Translate(Expression expression)
        {
            // pre-evaluate local sub-trees
            expression = PartialEvaluator.Eval(expression, this.Mapper.Mapping.CanBeEvaluatedLocally);

            // apply mapping (binds LINQ operators too)
            expression = this.Mapper.Translate(expression);

            // any policy specific translations or validations
            expression = this.Police.Translate(expression);

            // any language specific translations or validations
            expression = this.Linguist.Translate(expression);

            return expression;
        }
    }
}