// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// A <see cref="QueryExecutor"/> factory.
    /// </summary>
    public interface IQueryExecutorFactory
    {
        /// <summary>
        /// Creates a new <see cref="QueryExecutor"/>.
        /// </summary>
        QueryExecutor CreateExecutor();
    }
}