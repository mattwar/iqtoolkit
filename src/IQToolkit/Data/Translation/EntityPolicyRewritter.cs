// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;
using IQToolkit.Expressions;

namespace IQToolkit.Data
{
    using Expressions;
    using Translation;

    public class EntityPolicyRewritter : QueryPolicyRewriter
    {
        private readonly EntityPolicy _policy;

        public EntityPolicyRewritter(QueryTranslator translator, EntityPolicy policy)
            : base(translator, policy)
        {
            _policy = policy;
        }

        public override Expression ApplyPolicy(Expression expression, MemberInfo member)
        {
            var operations = _policy.GetOperations(member);
            if (operations.Count > 0)
            {
                var result = expression;

                foreach (var fnOp in operations)
                {
                    var pop = PartialEvaluator.Eval(fnOp, this.Translator.MappingRewriter.Mapping.CanBeEvaluatedLocally);
                    result = this.Translator.MappingRewriter.ApplyMapping(Expression.Invoke(pop, result));
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
    }
}