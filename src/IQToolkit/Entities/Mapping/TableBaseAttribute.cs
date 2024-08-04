// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Entities.Mapping
{
    /// <summary>
    /// A base type for mapping attributes that describe table-like mapping.
    /// </summary>
    public abstract class TableBaseAttribute : MappingAttribute
    {
        /// <summary>
        /// The name of the table in the database. 
        /// If not specified, the table's name will be the name of the member or type the attribute is placed on.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The ID to use for this table in advanced multi-table mapping.
        /// If not specified, the <see cref="Id"/> will be the table's name.
        /// </summary>
        public string? Id { get; set; }
    }
}
