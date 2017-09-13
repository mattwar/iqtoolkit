// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Represents a parameter to a <see cref="QueryCommand"/>.
    /// </summary>
    public class QueryParameter
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of the parameter in the CLR type system.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The type of the parameter in the database's type system.
        /// </summary>
        public QueryType QueryType { get; }

        public QueryParameter(string name, Type type, QueryType queryType)
        {
            this.Name = name;
            this.Type = type;
            this.QueryType = queryType;
        }
    }
}