// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Entities.Mapping
{
    /// <summary>
    /// A <see cref="MappingAttribute"/> that describes an association between two entities via related column
    /// values in the tables underlying each. This is often the same as a foreign key relationship in the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class AssociationAttribute : MemberAttribute
    {
        /// <summary>
        /// The members of the entity that are used to associate this entity with the other related entities.
        /// This property must be specified.
        /// </summary>
        public string? KeyMembers { get; set; }

        /// <summary>
        /// The mapping ID of the related entity.
        /// If not specified, it is inferred to be the entity id of the related entity type.
        /// </summary>
        public string? RelatedEntityId { get; set; }

        /// <summary>
        /// The members in the related entity type that form the association key.
        /// If not specified, the related key members are inferred to have the same names as the key members. 
        /// </summary>
        public string? RelatedKeyMembers { get; set; }

        /// <summary>
        /// True if the association's <see cref="KeyMembers"/> correpsonding columns are foreign keys (constrained to the related table's primary key).
        /// This information is important to correctly order inserts, updates and deletes without violating foreign key constraints in the database.
        /// </summary>
        public bool IsForeignKey { get; set; }
    }
}
