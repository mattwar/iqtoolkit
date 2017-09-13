// Copyright(c) Microsoft Corporation.All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// A scalar type as understood by the database.
    /// </summary>
    public abstract class QueryType
    {
        public abstract bool NotNull { get; }
        public abstract int Length { get; }
        public abstract short Precision { get; }
        public abstract short Scale { get; }
    }
}