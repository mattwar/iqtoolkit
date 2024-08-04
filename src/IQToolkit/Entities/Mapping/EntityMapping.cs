// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Reflection;

namespace IQToolkit.Entities
{
    using Mapping;

    /// <summary>
    /// Provides information to map database table rows into objects.
    /// </summary>
    public abstract class EntityMapping
    {
        /// <summary>
        /// An optional type that has members for entity tables.
        /// </summary>
        public abstract Type? ContextType { get; }

        /// <summary>
        /// The fields and properties of the context type that refer to entity tables.
        /// </summary>
        public abstract IReadOnlyList<MemberInfo> ContextMembers { get; }

        /// <summary>
        /// Get the <see cref="MappedEntity"/> for the context member that refers to a table of entities.
        /// </summary>
        public abstract MappedEntity GetEntity(MemberInfo contextMember);

        /// <summary>
        /// Gets the <see cref="MappedEntity"/> given the table.
        /// </summary>
        public virtual MappedEntity GetEntity(IEntityTable table) =>
            this.GetEntity(table.EntityType, table.EntityId);

        /// <summary>
        /// Get the <see cref="MappedEntity"/> the entity, 
        /// yet is represented publicly as 'elementType'.
        /// </summary>
        public abstract MappedEntity GetEntity(Type entityType, string? entityId = null);
    }
}