// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Enforcer of a <see cref="QueryPolicy"/>.
    /// </summary>
    public class QueryPolice
    {
        /// <summary>
        /// The <see cref="QueryPolicy"/> being enforced.
        /// </summary>
        public QueryPolicy Policy { get; }

        /// <summary>
        /// The <see cref="QueryTranslator"/> used to enforce policy.
        /// </summary>
        public QueryTranslator Translator { get; }

        /// <summary>
        /// Construct a new <see cref="QueryPolice"/> instance.
        /// </summary>
        public QueryPolice(QueryPolicy policy, QueryTranslator translator)
        {
            this.Policy = policy;
            this.Translator = translator;
        }

        /// <summary>
        /// Applies the member specific policy to an projection.
        /// </summary>
        public virtual Expression ApplyPolicy(Expression projection, MemberInfo member)
        {
            // default: do nothing
            return projection;
        }

        /// <summary>
        /// Translates the query expression to include changes that enforce the policy.
        /// This is where choices about inclusion of related objects and how heirarchies are materialized affect the definition of the queries.
        /// </summary>
        public virtual Expression Translate(Expression expression)
        {
            // add included relationships to client projection
            var rewritten = RelationshipIncluder.Include(this.Translator.Mapper, expression);
            if (rewritten != expression)
            {
                expression = rewritten;
                expression = UnusedColumnRemover.Remove(expression);
                expression = RedundantColumnRemover.Remove(expression);
                expression = RedundantSubqueryRemover.Remove(expression);
                expression = RedundantJoinRemover.Remove(expression);
            }

            // convert any singleton (1:1 or n:1) projections into server-side joins (cardinality is preserved)
            rewritten = SingletonProjectionRewriter.Rewrite(this.Translator.Linguist.Language, expression);
            if (rewritten != expression)
            {
                expression = rewritten;
                expression = UnusedColumnRemover.Remove(expression);
                expression = RedundantColumnRemover.Remove(expression);
                expression = RedundantSubqueryRemover.Remove(expression);
                expression = RedundantJoinRemover.Remove(expression);
            }

            // convert projections into client-side joins
            rewritten = ClientJoinedProjectionRewriter.Rewrite(this.Policy, this.Translator.Linguist.Language, expression);
            if (rewritten != expression)
            {
                expression = rewritten;
                expression = UnusedColumnRemover.Remove(expression);
                expression = RedundantColumnRemover.Remove(expression);
                expression = RedundantSubqueryRemover.Remove(expression);
                expression = RedundantJoinRemover.Remove(expression);
            }

            return expression;
        }

        /// <summary>
        /// Converts a query into an execution plan.
        /// The plan is an function that executes the query and builds the resulting objects.
        /// </summary>
        /// <param name="query">The <see cref="Expression"/> that encapsulates the query.</param>
        /// <param name="provider">An <see cref="Expression"/> the references the current <see cref="EntityProvider"/>.</param>
        public virtual Expression BuildExecutionPlan(Expression query, Expression provider)
        {
            return ExecutionBuilder.Build(this.Translator.Linguist, this.Policy, query, provider);
        }
    }
}