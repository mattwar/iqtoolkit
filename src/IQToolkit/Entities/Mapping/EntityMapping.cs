// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Reflection;

using System.Diagnostics.CodeAnalysis;

namespace IQToolkit.Entities
{
    using Mapping;

    /// <summary>
    /// Provides information to map database table rows into objects.
    /// </summary>
    public abstract class EntityMapping
    {
        /// <summary>
        /// Get the <see cref="MappedEntity"/> the entity for the entity type and id.
        /// </summary>
        public abstract bool TryGetEntity(
            Type entityType, 
            string? entityId, 
            [NotNullWhen(true)] out MappedEntity? entity);

        /// <summary>
        /// Get the <see cref="MappedEntity"/> for the context member that refers to an entity table or query.
        /// </summary>
        public abstract bool TryGetEntity(
            MemberInfo contextMember, 
            [NotNullWhen(true)] out MappedEntity? entity);

        /// <summary>
        /// Gets all the known entities.
        /// </summary>
        public abstract IEnumerable<MappedEntity> GetEntities();
    }
}