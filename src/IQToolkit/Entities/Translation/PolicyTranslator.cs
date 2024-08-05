// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    /// <summary>
    /// Enforcer of a <see cref="QueryPolicy"/>.
    /// </summary>
    public abstract class PolicyTranslator
    {
        /// <summary>
        /// The <see cref="QueryPolicy"/> being enforced.
        /// </summary>
        public abstract QueryPolicy Policy { get; }

        /// <summary>
        /// Applies the entity specific policy to an expression.
        /// </summary>
        /// <param name="expression">An expression that produces a sequence of entities.</param>
        /// <param name="memberOrType"></param>
        public abstract Expression ApplyEntityPolicy(
            Expression expression,
            MemberInfo memberOrType,
            LanguageTranslator linguist,
            MappingTranslator mapper);

        /// <summary>
        /// Apply additional policy related rewrites.
        /// </summary>
        public abstract Expression ApplyPolicyRewrites(
            Expression expression,
            LanguageTranslator linguist,
            MappingTranslator mapper);
    }
}