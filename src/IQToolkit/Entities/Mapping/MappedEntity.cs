// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;

namespace IQToolkit.Entities.Mapping
{
    /// <summary>
    /// Information about how an object is mapped to one or more database table rows.
    /// </summary>
    public abstract class MappedEntity
    {
        /// <summary>
        /// The mapping id of the entity that distinguishes it from other mappings for the same type.
        /// </summary>
        public abstract string EntityId { get; }

        /// <summary>
        /// The type of the entity used in the <see cref="IEntityTable{TEntity}"/>.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// The type of the entity that is constructed at runtime.
        /// This may be the same or different from the Type property
        /// allowing the entity type to be an interface or abstract class.
        /// </summary>
        public abstract Type ConstructedType { get; }

        /// <summary>
        /// All the mapped members of the entity.
        /// </summary>
        public abstract IReadOnlyList<MappedMember> MappedMembers { get; }

        /// <summary>
        /// The members that are mapped to database table columns.
        /// </summary>
        public abstract IReadOnlyList<MappedColumnMember> ColumnMembers { get; }

        /// <summary>
        /// The members that refer to other entities.
        /// </summary>
        public abstract IReadOnlyList<MappedRelationshipMember> RelationshipMembers { get; }

        /// <summary>
        /// The members that form the primary key of the entity.
        /// </summary>
        public abstract IReadOnlyList<MappedColumnMember> PrimaryKeyMembers { get; }

        /// <summary>
        /// All the tables that the entity maps to.
        /// </summary>
        public abstract IReadOnlyList<MappedTable> Tables { get; }

        /// <summary>
        /// The primary table for an entity.
        /// </summary>
        public abstract MappedTable PrimaryTable { get; }

        /// <summary>
        /// All extension tables for an entity that is mapped to multiple tables.
        /// </summary>
        public abstract IReadOnlyList<MappedExtensionTable> ExtensionTables { get; }
    }
}