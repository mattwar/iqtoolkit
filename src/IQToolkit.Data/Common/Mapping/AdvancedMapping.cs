// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// A <see cref="QueryMapping"/> that allows 
    /// 1) mapping a single entity onto rows in multiple tables,
    /// 2) mapping a single table (or set of tables) into multiple entities (nested).
    /// </summary>
    public abstract class AdvancedMapping : BasicMapping
    {
        /// <summary>
        /// True if the member references a nested entity.
        /// </summary>
        public abstract bool IsNestedEntity(MappingEntity entity, MemberInfo member);

        /// <summary>
        /// Gets the tables associated with a single entity.
        /// </summary>
        public abstract IList<MappingTable> GetTables(MappingEntity entity);

        /// <summary>
        /// Gets the primary table of the mapping (for multi-table mappings).
        /// </summary>
        public MappingTable GetPrimaryTable(MappingEntity entity)
        {
            return this.GetTables(entity).Single(t => !this.IsExtensionTable(t));
        }

        /// <summary>
        /// Gets all extension tables (not the primary table) of a multi-table mapping.
        /// </summary>
        public IEnumerable<MappingTable> GetExtensionTables(MappingEntity entity)
        {
            return this.GetTables(entity).Where(t => this.IsExtensionTable(t));
        }

        /// <summary>
        /// Gets the <see cref="MappingEntity"/> associated with the <see cref="MappingTable"/>.
        /// </summary>
        public abstract MappingEntity GetEntity(MappingTable table);

        /// <summary>
        /// Gets the mapping id for a specific table.
        /// </summary>
        public abstract string GetTableId(MappingTable table);

        /// <summary>
        /// Gets the table id used for the mapped member.
        /// </summary>
        public abstract string GetTableId(MappingEntity entity, MemberInfo member);

        /// <summary>
        /// Gets the name of a table.
        /// </summary>
        public abstract string GetTableName(MappingTable table);

        /// <summary>
        /// True if the table is an extension table.
        /// 
        /// In single entity to multiple table mapping, one table is the primary source and all others are considered extensions.
        /// </summary>
        public abstract bool IsExtensionTable(MappingTable table);

        /// <summary>
        /// Gets the related table's id for an extension table.
        /// This is usually the primary table's id.
        /// </summary>
        public abstract string GetExtensionRelatedTableId(MappingTable table);

        /// <summary>
        /// Gets the column names in the extension table that correspond to the primary table's primary key.
        /// </summary>
        public abstract IEnumerable<string> GetExtensionKeyColumnNames(MappingTable table);

        /// <summary>
        /// Gets the members in the entity that correspond to the columns from the extension table.
        /// </summary>
        public abstract IEnumerable<MemberInfo> GetExtensionRelatedMembers(MappingTable table);

        protected AdvancedMapping()
        {
        }

        public override bool IsRelationship(MappingEntity entity, MemberInfo member)
        {
            return base.IsRelationship(entity, member)
                || this.IsNestedEntity(entity, member);
        }

        public override object CloneEntity(MappingEntity entity, object instance)
        {
            object clone = base.CloneEntity(entity, instance);

            // need to clone nested entities too
            foreach (var mi in this.GetMappedMembers(entity))
            {
                if (this.IsNestedEntity(entity, mi))
                {
                    MappingEntity nested = this.GetRelatedEntity(entity, mi);
                    var nestedValue = mi.GetValue(instance);
                    if (nestedValue != null)
                    {
                        var nestedClone = this.CloneEntity(nested, mi.GetValue(instance));
                        mi.SetValue(clone, nestedClone);
                    }
                }
            }

            return clone;
        }

        public override bool IsModified(MappingEntity entity, object instance, object original)
        {
            if (base.IsModified(entity, instance, original))
                return true;

            // need to check nested entities too
            foreach (var mi in this.GetMappedMembers(entity))
            {
                if (this.IsNestedEntity(entity, mi))
                {
                    MappingEntity nested = this.GetRelatedEntity(entity, mi);
                    if (this.IsModified(nested, mi.GetValue(instance), mi.GetValue(original)))
                        return true;
                }
            }

            return false;
        }

        public override QueryMapper CreateMapper(QueryTranslator translator)
        {
            return new AdvancedMapper(this, translator);
        }
    }
}