// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// An entity (table/row type) described in the mapping system.
    /// </summary>
    public abstract class MappingEntity
    {
        /// <summary>
        /// The mapping ID of the entity (typically the name of the entity type.)
        /// </summary>
        public abstract string EntityId { get; }

        /// <summary>
        /// The static type of the entity returned from the <see cref="IEntityTable{T}"/> 
        /// This could differ from the <see cref="EntityType"/> if the static type is a base type or an interface.
        /// </summary>
        public abstract Type ElementType { get; }

        /// <summary>
        /// The type of the entity that is constructed and returned from the <see cref="IEntityTable{T}"/>.
        /// </summary>
        public abstract Type EntityType { get; }
    }
}