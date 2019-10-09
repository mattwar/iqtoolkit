// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Applies mapping rules to queries.
    /// </summary>
    public abstract class QueryMapper
    {
        /// <summary>
        /// The mapping to apply.
        /// </summary>
        public abstract QueryMapping Mapping { get; }

        /// <summary>
        /// The overall <see cref="QueryTranslator"/>.
        /// </summary>
        public abstract QueryTranslator Translator { get; }

        /// <summary>
        /// Get a query expression that selects all entities from a table
        /// </summary>
        public abstract ProjectionExpression GetQueryExpression(MappingEntity entity);

        /// <summary>
        /// Gets an expression that constructs an entity instance relative to a root.
        /// The root is most often a TableExpression, but may be any other experssion such as
        /// a ConstantExpression.
        /// </summary>
        public abstract EntityExpression GetEntityExpression(Expression root, MappingEntity entity);

        /// <summary>
        /// Get an expression for a mapped property relative to a root expression. 
        /// The root is either a TableExpression or an expression defining an entity instance.
        /// </summary>
        public abstract Expression GetMemberExpression(Expression root, MappingEntity entity, MemberInfo member);

        /// <summary>
        /// Get an expression that represents the insert operation for the specified instance.
        /// </summary>
        /// <param name="entity">The mapping for the entity.</param>
        /// <param name="instance">The instance to insert.</param>
        /// <param name="selector">A lambda expression that computes a return value from the operation.</param>
        /// <returns></returns>
        public abstract Expression GetInsertExpression(MappingEntity entity, Expression instance, LambdaExpression selector);

        /// <summary>
        /// Get an expression that represents the update operation for the specified instance.
        /// </summary>
        public abstract Expression GetUpdateExpression(MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression selector, Expression @else);

        /// <summary>
        /// Get an expression that represents the insert-or-update operation for the specified instance.
        /// </summary>
        public abstract Expression GetInsertOrUpdateExpression(MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression resultSelector);

        /// <summary>
        /// Get an expression that represents the delete operation for the specified instance.
        /// </summary>
        public abstract Expression GetDeleteExpression(MappingEntity entity, Expression instance, LambdaExpression deleteCheck);

        /// <summary>
        /// Recreate the type projection with the additional members included
        /// </summary>
        public abstract EntityExpression IncludeMembers(EntityExpression entity, Func<MemberInfo, bool> fnIsIncluded);

        /// <summary>
        /// Return true if the entity expression has included members.
        /// </summary>
        public abstract bool HasIncludedMembers(EntityExpression entity);

        /// <summary>
        /// Apply mapping to a sub query expression
        /// </summary>
        public virtual Expression ApplyMapping(Expression expression)
        {
            return QueryBinder.Bind(this, expression);
        }

        /// <summary>
        /// Apply mapping translations to this expression
        /// </summary>
        public virtual Expression Translate(Expression expression)
        {
            // convert references to LINQ operators into query specific nodes
            var bound = QueryBinder.Bind(this, expression);

            // move aggregate computations so they occur in same select as group-by
            var aggmoved = AggregateRewriter.Rewrite(this.Translator.Linguist.Language, bound);

            // do reduction so duplicate association's are likely to be clumped together
            var reduced = UnusedColumnRemover.Remove(aggmoved);
            reduced = RedundantColumnRemover.Remove(reduced);
            reduced = RedundantSubqueryRemover.Remove(reduced);
            reduced = RedundantJoinRemover.Remove(reduced);

            // convert references to association properties into correlated queries
            var rbound = RelationshipBinder.Bind(this, reduced);
            if (rbound != reduced)
            {
                // clean up after ourselves! (multiple references to same association property)
                rbound = RedundantColumnRemover.Remove(rbound);
                rbound = RedundantJoinRemover.Remove(rbound);
            }

            // rewrite comparision checks between entities and multi-valued constructs
            var result = ComparisonRewriter.Rewrite(this.Mapping, rbound);

            return result;
        }
    }
}