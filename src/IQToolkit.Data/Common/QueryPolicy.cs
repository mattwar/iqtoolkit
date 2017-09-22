// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Defines query execution and materialization policies. 
    /// </summary>
    public class QueryPolicy
    {
        /// <summary>
        /// Constructs a <see cref="QueryPolicy"/>
        /// </summary>
        public QueryPolicy()
        {
        }

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

        /// <summary>
        /// Creates a <see cref="QueryPolice"/> instance.
        /// </summary>
        public virtual QueryPolice CreatePolice(QueryTranslator translator)
        {
            return new QueryPolice(this, translator);
        }

        /// <summary>
        /// The default <see cref="QueryPolicy"/>.
        /// </summary>
        public static readonly QueryPolicy Default = new QueryPolicy();
    }
}