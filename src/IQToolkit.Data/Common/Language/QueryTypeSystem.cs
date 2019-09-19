// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// A type system used by a query language
    /// </summary>
    public abstract class QueryTypeSystem 
    {
        /// <summary>
        /// Parse a type declaration in the database's language.
        /// </summary>
        public abstract QueryType Parse(string typeDeclaration);

        /// <summary>
        /// Convert a CLR type to a database type.
        /// </summary>
        public abstract QueryType GetColumnType(Type type);

        /// <summary>
        /// Format the data type as it would appear in the language in a declaration.
        /// </summary>
        public abstract string Format(QueryType type, bool suppressSize);
    }
}