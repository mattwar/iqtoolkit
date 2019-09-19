// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Represents info about an entity (a CLR type that is mapped into a row of a table in a database.)
    /// </summary>
    public abstract class MappingEntity
    {
        /// <summary>
        /// The mapping ID of the entity (typically the name of the entity type.)
        /// </summary>
        public abstract string EntityId { get; }

        /// <summary>
        /// The static type of the entity that is referenced by queries, etc. 
        /// </summary>
        public abstract Type StaticType { get; }

        /// <summary>
        /// The type of the entity that is constructed at runtime.
        /// This may be different than the static type if the static type is an base class or interface.
        /// </summary>
        public abstract Type RuntimeType { get; }
    }
}