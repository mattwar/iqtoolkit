// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Entities
{
    /// <summary>
    /// Defines the language rules for a query provider.
    /// </summary>
    public abstract class QueryLanguage
    {
        /// <summary>
        /// The type system used by the language.
        /// </summary>
        public abstract QueryTypeSystem TypeSystem { get; }

        /// <summary>
        /// Gets the execution plan for the query expression.
        /// </summary>
        public abstract QueryPlan GetQueryPlan(
            Expression query, 
            IEntityProvider provider
            );
    }
}