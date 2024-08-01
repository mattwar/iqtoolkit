// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;

    public class EntityPolice : QueryPolice
    {
        private readonly EntityPolicy _policy;

        public EntityPolice(EntityPolicy policy)
            : base(policy)
        {
            _policy = policy;
        }

        public override Expression ApplyPolicy(
            Expression expression, 
            MemberInfo member,
            QueryLinguist linguist,
            QueryMapper mapper)
        {
            var operations = _policy.GetOperations(member);
            if (operations.Count > 0)
            {
                var result = expression;

                foreach (var fnOp in operations)
                {
                    var pop = PartialEvaluator.Eval(fnOp, mapper.Mapping.CanBeEvaluatedLocally);
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
    }
}