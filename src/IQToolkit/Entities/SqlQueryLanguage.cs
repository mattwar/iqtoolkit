// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Expressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Entities
{
    using IQToolkit.Entities.Mapping;
    using System.Collections.Generic;
    using Translation;
    using Utils;

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

            if (mapping is StandardMapping stdMapping)
            {
                mapper = new StandardMapper(stdMapping);
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
            var evaluated = PartialEvaluator.Eval(query, this.CanBeEvaluatedLocally);

            // convert LINQ operators to SqlExpressions (with initial mapping & policy)
            var sqlized = evaluated.ConvertLinqOperatorToSqlExpressions(linguist, mapper, police);

            var mapped = mapper.Apply(sqlized, linguist, police);
            var policed = police.Apply(mapped, linguist, mapper);
            var translation = linguist.Apply(policed, mapper, police);

            // look for and use executor found in query itself. (for session executor)
            var runtimeProvider = Find(evaluated, lambda?.Parameters, typeof(IEntityProvider))
                ?? (Find(evaluated, lambda?.Parameters, typeof(IQueryable)) is { } rootQueryable
                        ? Expression.Property(rootQueryable, TypeHelper.FindDeclaredProperty(typeof(IQueryable), "Provider"))
                        : null);

            var executorValue = runtimeProvider != null
                ? Expression.Property(Expression.Convert(runtimeProvider, typeof(IEntityProvider)), "Executor")
                : (Expression)Expression.Constant(provider.Executor);

            // add back lambda
            if (lambda != null)
                translation = lambda.Update(lambda.Type, translation, lambda.Parameters);

            // build the plan
            return QueryPlanBuilder.Build(
                provider,
                linguist,
                translation,
                executorValue
                );
        }

        /// <summary>
        /// Find the expression of the specified type, either in the specified expression or parameters.
        /// </summary>
        private static Expression? Find(Expression expression, IReadOnlyList<ParameterExpression>? parameters, Type type)
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