// Copyright(c) Microsoft Corporation.All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;

namespace IQToolkit.Entities.Mapping
{
    public abstract class MappedTable
    {
        /// <summary>
        /// The id of the table.
        /// </summary>
        public abstract string TableId { get; }

        /// <summary>
        /// The name of the table.
        /// </summary>
        public abstract string TableName { get; }

        /// <summary>
        /// The primary <see cref="MappedEntity"/> associated with the table.
        /// </summary>
        public abstract MappedEntity Entity { get; }
    }

    public abstract class MappedExtensionTable : MappedTable
    {
        /// <summary>
        /// The related table for an extension table.
        /// </summary>
        public abstract MappedTable RelatedTable { get; }

        /// <summary>
        /// Gets the column names in the extension table that correspond to the primary table's primary key.
        /// </summary>
        public abstract IReadOnlyList<string> KeyColumnNames { get; }

        /// <summary>
        /// Gets the members in the entity that correspond to the columns from the extension table.
        /// </summary>
        public abstract IReadOnlyList<MappedColumnMember> RelatedMembers { get; }
    }
}