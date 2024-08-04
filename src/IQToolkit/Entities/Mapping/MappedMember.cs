// Copyright(c) Microsoft Corporation.All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Entities.Mapping
{
    public abstract class MappedMember
    {
        /// <summary>
        /// The <see cref="MappedEntity"/> this is a member of.
        /// </summary>
        public abstract MappedEntity Entity { get; }

        /// <summary>
        /// The member that is mapped.
        /// </summary>
        public abstract MemberInfo Member { get; }
    }

    public abstract class MappedColumnMember : MappedMember
    {
        /// <summary>
        /// The table containing this column.
        /// </summary>
        public abstract MappedTable Table { get; }

        /// <summary>
        /// The name of the column in the table.
        /// </summary>
        public abstract string ColumnName { get; }

        /// <summary>
        /// The column's type in the database query language.
        /// </summary>
        public abstract string? ColumnType { get; }

        /// <summary>
        /// True if the member is part of the entity's primary key.
        /// </summary>
        public abstract bool IsPrimaryKey { get; }

        /// <summary>
        /// True if a property should not be updated.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// True if a property is computed after insert or update.
        /// </summary>
        public abstract bool IsComputed { get; }

        /// <summary>
        /// True if a property value is generated on the server during insertion.
        /// </summary>
        public abstract bool IsGenerated { get; }

        /// <summary>
        /// True if a property can be part of an update operation
        /// </summary>
        public virtual bool IsUpdatable =>
            !this.IsPrimaryKey
            && !this.IsReadOnly;
    }

    public abstract class MappedRelationshipMember : MappedMember
    {
        /// <summary>
        /// The entity on the other side of the relationship.
        /// </summary>
        public abstract MappedEntity RelatedEntity { get; }

        /// <summary>
        /// Returns true if the member is the source of a one-to-many relationship.
        /// </summary>
        public abstract bool IsSource { get; }

        /// <summary>
        /// Returns true if the member is the target of a one-to-many relationship.
        /// </summary>
        public abstract bool IsTarget { get; }

        /// <summary>
        /// Determines if a relationship property refers to a single entity (as opposed to a collection.)
        /// </summary>
        public abstract bool IsSingleton { get; }
    }

    public abstract class MappedAssociationMember : MappedRelationshipMember
    {
        /// <summary>
        /// Returns the key members on this side of the association
        /// </summary>
        public abstract IReadOnlyList<MappedColumnMember> KeyMembers { get; }

        /// <summary>
        /// Returns the key members on the other side (related side) of the association
        /// </summary>
        public abstract IReadOnlyList<MappedColumnMember> RelatedKeyMembers { get; }
    }

    public abstract class MappedNestedEntityMember : MappedRelationshipMember
    {
    }
}