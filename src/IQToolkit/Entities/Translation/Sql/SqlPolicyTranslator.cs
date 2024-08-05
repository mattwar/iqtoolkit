// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;

    /// <summary>
    /// A <see cref="PolicyTranslator"/> that applies policy rules to <see cref="SqlExpression"/> based queries.
    /// </summary>
    public class SqlPolicyTranslator : PolicyTranslator
    {
        private readonly EntityPolicy _policy;

        public override QueryPolicy Policy => _policy;

        public SqlPolicyTranslator(EntityPolicy policy)
        {
            _policy = policy;
        }

        public override Expression ApplyEntityPolicy(
            Expression expression, 
            MemberInfo member,
            LanguageTranslator linguist,
            MappingTranslator mapper)
        {
            var operations = _policy.GetOperations(member);
            if (operations.Count > 0)
            {
                var result = expression;

                foreach (var fnOp in operations)
                {
                    var pop = PartialEvaluator.Eval(fnOp, linguist.Language.CanBeEvaluatedLocally);
                    var invoked = Expression.Invoke(pop, result);
                    result = invoked.ConvertLinqOperatorToSqlExpressions(linguist, mapper, this, isQueryFragment: true);
                }

                var projection = (ClientProjectionExpression)result;
                if (projection.Type != expression.Type)
                {
                    var fnAgg = Aggregator.GetAggregator(expression.Type, projection.Type);
                    projection = new ClientProjectionExpression(projection.Select, projection.Projector, fnAgg);
                }

                return projection;
            }

            return expression;
        }

        public override Expression ApplyPolicyRewrites(
            Expression expression,
            LanguageTranslator linguist,
            MappingTranslator mapper)
        {
            // add included relationships to client projection
            var included = expression.AddIncludedRelationships(linguist, mapper, this);

            // convert any singleton (1:1 or n:1) projections into server-side joins
            var singletonsConverted = included.ConvertSingletonProjections(linguist, mapper.Mapping);

            // convert projections into client-side joins
            var nestedConverted = singletonsConverted.ConvertNestedProjectionsToClientJoins(linguist, this);

            return nestedConverted;
        }
    }
}