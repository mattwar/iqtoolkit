// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    /// <summary>
    /// Enforcer of a <see cref="QueryPolicy"/>.
    /// </summary>
    public class QueryPolicyRewriter
    {
        /// <summary>
        /// The related <see cref="QueryTranslator"/>
        /// </summary>
        public QueryTranslator Translator { get; }

        /// <summary>
        /// The <see cref="QueryPolicy"/> being enforced.
        /// </summary>
        public QueryPolicy Policy { get; }

        /// <summary>
        /// Construct a new <see cref="QueryPolicyRewriter"/> instance.
        /// </summary>
        public QueryPolicyRewriter(QueryTranslator translator, QueryPolicy policy)
        {
            this.Translator = translator;
            this.Policy = policy;
        }

        /// <summary>
        /// Applies the member specific policy to a projection.
        /// </summary>
        public virtual Expression ApplyPolicy(Expression projection, MemberInfo member)
        {
            // default: do nothing
            return projection;
        }

        /// <summary>
        /// Rewrites the query expression to include changes that enforce the policy.
        /// This is where choices about inclusion of related objects and how heirarchies are materialized affect the definition of the queries.
        /// </summary>
        public virtual Expression Rewrite(Expression expression)
        {
            // add included relationships to client projection
            var included = expression.AddIncludedRelationships(this.Policy, this.Translator.MappingRewriter);

            // convert any singleton (1:1 or n:1) projections into server-side joins (cardinality is preserved)
            var singletonsConverted = included.ConvertSingletonProjections(this.Translator.Language, this.Translator.Mapping);

            // convert projections into client-side joins
            var nestedConverted = singletonsConverted.ConvertNestedProjectionsToClientJoins(this.Policy, this.Translator.Language);

            return nestedConverted;
        }
    }
}