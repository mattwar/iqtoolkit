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
        /// The ID of the table (typically the table's name).
        /// </summary>
        public abstract string TableId { get; }

        /// <summary>
        /// The type of the element produced when querying the entity, which may differ from the <see cref="EntityType"/>.
        /// </summary>
        public abstract Type ElementType { get; }

        /// <summary>
        /// The type of the entity that is mapped to the table's columns.
        /// </summary>
        public abstract Type EntityType { get; }
    }
}