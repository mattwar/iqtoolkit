// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
        public abstract string Id { get; }

        /// <summary>
        /// The type of the entity used in the <see cref="IEntityTable{TEntity}"/>.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// The context member name associated with this entity mapping.
        /// </summary>
        public abstract string? Context { get; }

        /// <summary>
        /// The type of the entity that is constructed at runtime.
        /// This may be the same or different from the Type property
        /// allowing the entity type to be an interface or abstract class.
        /// </summary>
        public abstract Type ConstructedType { get; }

        /// <summary>
        /// All the mapped members of the entity.
        /// </summary>
        public abstract IReadOnlyList<MappedMember> Members { get; }

        /// <summary>
        /// The members that form the primary key of the entity.
        /// </summary>
        public abstract IReadOnlyList<ColumnMember> PrimaryKeyMembers { get; }

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
        public abstract IReadOnlyList<ExtensionTable> ExtensionTables { get; }

        /// <summary>
        /// All the columns that the entity maps to.
        /// </summary>
        public abstract IReadOnlyList<MappedColumn> Columns { get; }

        /// <summary>
        /// Any diagnostics determined when resolving this entity.
        /// </summary>
        public abstract IReadOnlyList<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Gets the mapped member by its member name.
        /// </summary>
        public abstract bool TryGetMember(string name, [NotNullWhen(true)] out MappedMember? member);

        /// <summary>
        /// Gets the mapped table by its name.
        /// </summary>
        public abstract bool TryGetTable(string name, [NotNullWhen(true)] out MappedTable? table);

        /// <summary>
        /// Gets the column by its name.
        /// If more than one table's column has the same name, the first one found is returned,
        /// in order of the primary table and then the extension tables in the order listed.
        /// </summary>
        public virtual bool TryGetColumn(
            string columnName, 
            [NotNullWhen(true)] out MappedColumn? column)
        {
            return TryGetColumn(columnName, null, out column);
        }

        /// <summary>
        /// Gets the column by the column and table name.
        /// </summary>
        public virtual bool TryGetColumn(
            string columnName,
            string? tableName,
            [NotNullWhen(true)] out MappedColumn? column)
        {
            if (tableName != null
                && this.TryGetTable(tableName, out var namedTable)
                && namedTable.TryGetColumn(columnName, out column))
            {
                return true;
            }

            foreach (var table in this.Tables)
            {
                if (table.TryGetColumn(columnName, out column))
                {
                    return true;
                }
            }

            column = null;
            return false;
        }
    }
}