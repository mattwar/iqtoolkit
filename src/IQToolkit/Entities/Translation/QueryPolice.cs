// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
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
        /// Construct a new <see cref="QueryPolice"/> instance.
        /// </summary>
        public QueryPolice(QueryPolicy policy)
        {
            this.Policy = policy;
        }

        /// <summary>
        /// Applies the member specific policy to a projection.
        /// </summary>
        public virtual Expression ApplyPolicy(
            Expression projection, 
            MemberInfo member,
            QueryLinguist linguist,
            QueryMapper mapper)
        {
            // default: do nothing
            return projection;
        }

        /// <summary>
        /// Apply additional policy related rewrites.
        /// </summary>
        public virtual Expression Apply(
            Expression expression,
            QueryLinguist linguist,
            QueryMapper mapper)
        {
            // add included relationships to client projection
            var included = expression.AddIncludedRelationships(linguist, mapper, this);

            // convert any singleton (1:1 or n:1) projections into server-side joins
            var singletonsConverted = included.ConvertSingletonProjections(linguist.Language, mapper.Mapping);

            // convert projections into client-side joins
            var nestedConverted = singletonsConverted.ConvertNestedProjectionsToClientJoins(this.Policy, linguist.Language);

            return nestedConverted;
        }
    }
}