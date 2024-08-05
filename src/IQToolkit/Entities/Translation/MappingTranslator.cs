// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Mapping;

    /// <summary>
    /// Applies mapping translations to queries.
    /// </summary>
    public abstract class MappingTranslator
    {
        /// <summary>
        /// The mapping to apply.
        /// </summary>
        public abstract EntityMapping Mapping { get; }

        /// <summary>
        /// Applies additional mapping related rewrites to the entire query.
        /// </summary>
        public abstract Expression ApplyMappingRewrites(
            Expression query,
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Get a query expression that selects all entities from a table
        /// </summary>
        public abstract Expression GetQueryExpression(
            MappedEntity entity, 
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Gets an expression that constructs an entity instance relative to a root.
        /// The root is most often a TableExpression, but may be any other experssion such as
        /// a ConstantExpression.
        /// </summary>
        public abstract Expression GetEntityExpression(
            Expression root,
            MappedEntity entity,
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Get an expression for a mapped property relative to a root expression. 
        /// The root is either a TableExpression or an expression defining an entity instance.
        /// </summary>
        public abstract Expression GetMemberExpression(
            Expression root, 
            MappedMember member,
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Get an expression that represents the insert operation for the specified instance.
        /// </summary>
        public abstract Expression GetInsertExpression(
            MappedEntity entity, 
            Expression instance, 
            LambdaExpression? selector,
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Get an expression that represents the update operation for the specified instance.
        /// </summary>
        public abstract Expression GetUpdateExpression(
            MappedEntity entity, 
            Expression instance, 
            LambdaExpression? updateCheck, 
            LambdaExpression? selector, 
            Expression? @else,
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Get an expression that represents the insert-or-update operation for the specified instance.
        /// </summary>
        public abstract Expression GetInsertOrUpdateExpression(
            MappedEntity entity, 
            Expression instance, 
            LambdaExpression? updateCheck, 
            LambdaExpression? resultSelector,
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Get an expression that represents the delete operation for the specified instance.
        /// </summary>
        public abstract Expression GetDeleteExpression(
            MappedEntity entity, 
            Expression? instance, 
            LambdaExpression? deleteCheck,
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Recreate the type projection with the additional members included
        /// </summary>
        public abstract Expression IncludeMembers(
            Expression entity, 
            Func<MemberInfo, bool> fnIsIncluded,
            LanguageTranslator linguist,
            PolicyTranslator police);

        /// <summary>
        /// Return true if the entity expression has included members.
        /// </summary>
        public abstract bool HasIncludedMembers(
            Expression entity, 
            QueryPolicy policy);
    }
}