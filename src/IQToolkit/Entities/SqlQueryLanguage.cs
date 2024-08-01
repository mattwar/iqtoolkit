// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Expressions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities
{
    using IQToolkit.Entities.Mapping;
    using System.Collections.Generic;
    using Translation;

    /// <summary>
    /// Defines the language rules for a query provider.
    /// </summary>
    public abstract class SqlQueryLanguage : QueryLanguage
    {
        protected abstract QueryLinguist Linguist { get; }

        protected virtual bool TryGetMapper(EntityMapping mapping, out QueryMapper mapper)
        {
            if (mapping is IHaveMapper cmt)
            {
                mapper = cmt.Mapper;
                return true;
            }

            if (mapping is AdvancedEntityMapping advMapping)
            {
                mapper = new AdvancedMapper(advMapping);
                return true;
            }
            else if (mapping is BasicEntityMapping basicMapping)
            {
                mapper = new BasicMapper(basicMapping);
                return true;
            }
            else
            {
                mapper = null!;
                return false;
                //// TODO: create unknown mapping translator...
                //throw new InvalidOperationException(
                //    string.Format("Unhandled mapping kind: {0}", this.Mapping.GetType().Name)
                //    );
            }
        }

        protected virtual bool TryGetPolice(QueryPolicy policy, out QueryPolice police)
        {
            if (policy is IHavePolice cpt)
            {
                police = cpt.Police;
                return true;
            }

            if (policy is EntityPolicy entityPolicy)
            {
                police = new EntityPolice(entityPolicy);
                return true;
            }
            else
            {
                police = new QueryPolice(policy);
                return true;
            }
        }

        public override QueryPlan GetQueryPlan(
            Expression query,
            IEntityProvider provider)
        {
            // remove possible lambda and add back later
            var lambda = query as LambdaExpression;
            if (lambda != null)
                query = lambda.Body;

            var linguist = this.Linguist;
            var mapping = provider.Mapping;
            var policy = provider.Policy;

            if (!this.TryGetMapper(mapping, out var mapper))
            {
                return new QueryPlan(query, new[] { new Diagnostic($"Cannot determine mapping applier.") });
            }

            if (!this.TryGetPolice(policy, out var police))
            {
                return new QueryPlan(query, new[] { new Diagnostic($"Cannot determine policy applier.") });
            }

            // translate query into client & server parts

            // pre-evaluate local sub-trees
            var evaluated = PartialEvaluator.Eval(query, mapper.Mapping.CanBeEvaluatedLocally);

            // convert LINQ operators to SqlExpressions (with initial mapping & policy)
            var sqlized = evaluated.ConvertLinqOperatorToSqlExpressions(linguist, mapper, police);

            var mapped = mapper.Apply(sqlized, linguist, police);
            var policed = police.Apply(mapped, linguist, mapper);
            var translation = linguist.Apply(policed, mapper, police);

#if false
            var parameters = lambda?.Parameters;
            var provider = this.Find(query, parameters, typeof(EntityProvider));
            if (provider == null)
            {
                var rootQueryable = this.Find(query, parameters, typeof(IQueryable));
                var providerProperty = typeof(IQueryable).GetProperty("Provider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                provider = Expression.Property(rootQueryable, providerProperty);
            }
#endif

            // add back lambda
            if (lambda != null)
                translation = lambda.Update(lambda.Type, translation, lambda.Parameters);

            // build the plan
            return QueryPlanBuilder.Build(
                provider,
                linguist,
                translation
                );
        }

        /// <summary>
        /// Find the expression of the specified type, either in the specified expression or parameters.
        /// </summary>
        private Expression? Find(Expression expression, IReadOnlyList<ParameterExpression>? parameters, Type type)
        {
            if (parameters != null)
            {
                Expression found = parameters.FirstOrDefault(p => type.IsAssignableFrom(p.Type));
                if (found != null)
                    return found;
            }

            return expression.FindFirstUpOrDefault(expr => type.IsAssignableFrom(expr.Type));
        }
    }
}