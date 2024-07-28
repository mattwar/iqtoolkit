// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Reflection;

namespace IQToolkit.Data
{
    /// <summary>
    /// Defines query execution and materialization policies. 
    /// </summary>
    public class QueryPolicy
    {
        /// <summary>
        /// Constructs a <see cref="QueryPolicy"/>
        /// </summary>
        protected QueryPolicy()
        {
        }

        /// <summary>
        /// A default <see cref="QueryPolicy"/> with no policy.
        /// </summary>
        public static QueryPolicy Default =
            new QueryPolicy();

        /// <summary>
        /// Determines if a relationship property is to be included in the results of the query
        /// </summary>
        public virtual bool IsIncluded(MemberInfo member)
        {
            return false;
        }

        /// <summary>
        /// Determines if a relationship property is included, but the query for the related data is 
        /// deferred until the property is first accessed.
        /// </summary>
        public virtual bool IsDeferLoaded(MemberInfo member)
        {
            return false;
        }
    }
}