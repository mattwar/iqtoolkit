// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Defined on types that can describe an <see cref="MappingEntity"/>.
    /// </summary>
    public interface IHaveMappingEntity
    {
        /// <summary>
        /// The <see cref="MappingEntity"/>.
        /// </summary>
        MappingEntity Entity { get; }
    }
}