// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Reflection;

namespace IQToolkit.Entities
{
    /// <summary>
    /// Defines query execution and materialization policies. 
    /// </summary>
    public abstract class QueryPolicy
    {
        /// <summary>
        /// Determines if a relationship property is to be included in the results of the query
        /// </summary>
        public abstract bool IsIncluded(MemberInfo member);

        /// <summary>
        /// Determines if a relationship property is included, but the query for the related data is 
        /// deferred until the property is first accessed.
        /// </summary>
        public abstract bool IsDeferLoaded(MemberInfo member);
    }
}