// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Expressions.Sql;
    using Mapping;

    /// <summary>
    /// Applies mapping rules to queries.
    /// </summary>
    public abstract class QueryMapper
    {
        /// <summary>
        /// The mapping to apply.
        /// </summary>
        public abstract EntityMapping Mapping { get; }

        /// <summary>
        /// Get a query expression that selects all entities from a table
        /// </summary>
        public abstract ClientProjectionExpression GetQueryExpression(
            MappedEntity entity, 
            QueryLinguist linguist,
            QueryPolice police);

        /// <summary>
        /// Gets an expression that constructs an entity instance relative to a root.
        /// The root is most often a TableExpression, but may be any other experssion such as
        /// a ConstantExpression.
        /// </summary>
        public abstract EntityExpression GetEntityExpression(
            Expression root,
            MappedEntity entity,
            QueryLinguist linguist,
            QueryPolice police);

        /// <summary>
        /// Get an expression for a mapped property relative to a root expression. 
        /// The root is either a TableExpression or an expression defining an entity instance.
        /// </summary>
        public abstract Expression GetMemberExpression(
            Expression root, 
            MappedMember member,
            QueryLinguist linguist,
            QueryPolice police);

        /// <summary>
        /// Get an expression that represents the insert operation for the specified instance.
        /// </summary>
        public abstract Expression GetInsertExpression(
            MappedEntity entity, 
            Expression instance, 
            LambdaExpression? selector,
            QueryLinguist linguist,
            QueryPolice police);

        /// <summary>
        /// Get an expression that represents the update operation for the specified instance.
        /// </summary>
        public abstract Expression GetUpdateExpression(
            MappedEntity entity, 
            Expression instance, 
            LambdaExpression? updateCheck, 
            LambdaExpression? selector, 
            Expression? @else,
            QueryLinguist linguist,
            QueryPolice police);

        /// <summary>
        /// Get an expression that represents the insert-or-update operation for the specified instance.
        /// </summary>
        public abstract Expression GetInsertOrUpdateExpression(
            MappedEntity entity, 
            Expression instance, 
            LambdaExpression? updateCheck, 
            LambdaExpression? resultSelector,
            QueryLinguist linguist,
            QueryPolice police);

        /// <summary>
        /// Get an expression that represents the delete operation for the specified instance.
        /// </summary>
        public abstract Expression GetDeleteExpression(
            MappedEntity entity, 
            Expression? instance, 
            LambdaExpression? deleteCheck,
            QueryLinguist linguist,
            QueryPolice police);

        /// <summary>
        /// Recreate the type projection with the additional members included
        /// </summary>
        public abstract EntityExpression IncludeMembers(
            EntityExpression entity, 
            Func<MemberInfo, bool> fnIsIncluded,
            QueryLinguist linguist,
            QueryPolice police);

        /// <summary>
        /// Return true if the entity expression has included members.
        /// </summary>
        public abstract bool HasIncludedMembers(
            EntityExpression entity, 
            QueryPolicy policy);

        /// <summary>
        /// Applies additional mapping related rewrites to the entire query.
        /// </summary>
        public virtual Expression Apply(
            Expression expression, 
            QueryLinguist linguist, 
            QueryPolice police)
        {
            // convert references to association properties into correlated queries
            var related = expression.RewriteRelationshipMembers(linguist, this, police);

            // rewrite comparision checks between entities and multi-valued constructs
            var result = related.ConvertEntityComparisons(this.Mapping);

            return result;
        }
    }
}