// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Entities.Mapping
{
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using Utils;

    public abstract class StandardMapping : EntityMapping
    {
        private ImmutableDictionary<string, MappedEntity> _idToEntityMap;

        protected StandardMapping()
        {
            _idToEntityMap = ImmutableDictionary<string, MappedEntity>.Empty;
        }

        public override IEnumerable<MappedEntity> GetEntities() =>
            _idToEntityMap.Values.SelectManyRecursive(e => 
                e.Members.OfType<AssociationMember>().Select(m => m.RelatedEntity));

        /// <summary>
        /// Gets or creates the <see cref="MappedEntity"/> for the entity id.
        /// </summary>
        protected virtual MappedEntity GetOrCreateEntity(
            Type entityType, 
            string entityId)
        {
            if (!_idToEntityMap.TryGetValue(entityId, out var mappedEntity))
            {
                var tmp = CreateEntity(entityType, entityId)!;
                mappedEntity = ImmutableInterlocked.GetOrAdd(ref _idToEntityMap, entityId, tmp);

                RealizeEntity(mappedEntity);
            }

            return mappedEntity;
        }

        /// <summary>
        /// Access all entity lazy members to force failures early.
        /// </summary>
        private void RealizeEntity(MappedEntity entity)
        {
            foreach (var table in entity.Tables)
            {
                RealizeTable(table);
            }

            foreach (var member in entity.Members)
            {
                RealizeMember(member);
            }
        }

        private void RealizeTable(MappedTable table)
        {
            var columns = table.Columns;
            if (columns.Count > 0)
            {
                table.TryGetColumn(columns[0].Name, out _);
            }
            if (table is ExtensionTable extTable)
            {
                var keyColumns = extTable.KeyColumns;
                var relatedTable = extTable.RelatedTable;
                var relatedKeyColumns = extTable.RelatedKeyColumns;
            }
        }

        private void RealizeMember(MappedMember member)
        {
            if (member is ColumnMember mcm)
            {
                var column = mcm.Column;
            }
            else if (member is CompoundMember compound)
            {
                foreach (var cm in compound.Members)
                {
                    RealizeMember(cm);
                }
            }
            else if (member is AssociationMember assoc)
            {
                var keyColumns = assoc.KeyColumns;
                var relatedEntity = assoc.RelatedEntity;
                var relatedColumns = assoc.RelatedKeyColumns;
            }
        }

        /// <summary>
        /// Create a <see cref="MappedEntity"/> from attributes on the entity or context type.
        /// </summary>
        protected abstract MappedEntity CreateEntity(
            Type entityType, string entityId);

        private static readonly char[] _nameListSeparators = new char[] { ' ', ',', '|' };

        protected virtual IEnumerable<string> GetNames(string nameList) =>
            nameList.Split(_nameListSeparators);

        protected virtual IReadOnlyList<MappedColumn> GetEntityColumns(
            MappedEntity entity, 
            string columnNames,
            string? tableName = null)
        {
            var columns = new List<MappedColumn>();

            foreach (var columnName in GetNames(columnNames))
            {
                if (entity.TryGetColumn(columnName, tableName, out var column))
                {
                    columns.Add(column);
                }
            }

            return columns.ToReadOnly();
        }

        protected virtual IReadOnlyList<MappedColumn> GetTableColumns(
            MappedTable table, 
            string columnNames)
        {
            var columns = new List<MappedColumn>();

            foreach (var columnName in GetNames(columnNames))
            {
                if (table.TryGetColumn(columnName, out var column))
                {
                    columns.Add(column);
                }
            }

            return columns.ToReadOnly();
        }

        /// <summary>
        /// True if the member can possibly be a column member.
        /// </summary>
        protected virtual bool IsPossibleColumnMember(MemberInfo member)
        {
            if (member is Type)
                return false;

            var type = TypeHelper.GetNonNullableType(TypeHelper.GetMemberType(member));

            if (TypeHelper.IsSequenceType(type, out var elementType))
            {
                return elementType == typeof(char) || elementType == typeof(byte);
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                    return false;
                case TypeCode.Object:
                    return
                        type == typeof(DateTimeOffset) ||
                        type == typeof(TimeSpan) ||
                        type == typeof(Guid) ||
                        typeof(IEnumerable<char>).IsAssignableFrom(type) ||
                        typeof(IEnumerable<byte>).IsAssignableFrom(type);
                default:
                    return true;
            }
        }

        /// <summary>
        /// True if the member can possibly be a compound (multi-column) member.
        /// </summary>
        protected virtual bool IsPossibleCompoundMember(MemberInfo member)
        {
            // TODO: discount any known entity types
            return !TypeHelper.IsSequenceType(TypeHelper.GetMemberType(member))
                && !IsPossibleColumnMember(member)
                && !(member is Type)
                && !IsKnownEntityType(TypeHelper.GetEntityType(member));
        }

        protected virtual bool IsKnownEntityType(Type type)
        {
            return this.GetEntities().Any(e => e.Type == type);
        }

        protected virtual IReadOnlyList<Diagnostic> GetEntityDiagnostics(MappedEntity entity)
        {
            // borrow from pool since likely not to have any diagnostics
            var diagnostics = _diagnosticPool.AllocateFromPool();
            try
            {
                foreach (var table in entity.Tables)
                {
                    GatherTableDiagnostics(table, diagnostics);
                }

                foreach (var column in entity.Columns)
                {
                    GatherColumnDiagnostics(column, diagnostics);
                }

                foreach (var member in entity.Members)
                {
                    GatherMemberDiagnostics(member, diagnostics);
                }

                return (diagnostics.Count > 0)
                    ? diagnostics.ToReadOnly()
                    : ReadOnlyList<Diagnostic>.Empty;
            }
            finally
            {
                _diagnosticPool.ReturnToPool(diagnostics);
            }
        }

        private static readonly ObjectPool<List<Diagnostic>> _diagnosticPool = 
            new ObjectPool<List<Diagnostic>>(
                () => new List<Diagnostic>(), 
                list => list.Clear()
                );

        private void GatherTableDiagnostics(MappedTable table, List<Diagnostic> diagnostics)
        {
            if (table is UnknownTable ut)
                diagnostics.AddRange(ut.Diagnostics);

            if (table is ExtensionTable extTable)
            {
                GatherTableDiagnostics(extTable.RelatedTable, diagnostics);
            }
        }

        private void GatherColumnDiagnostics(MappedColumn column, List<Diagnostic> diagnostics)
        {
            if (column is UnknownColumn uc)
                diagnostics.AddRange(uc.Diagnostics);
        }

        private void GatherReferencedEntityDiagnostics(MappedEntity entity, List<Diagnostic> diagnostics)
        {
            if (entity is UnknownEntity ue)
                diagnostics.AddRange(ue.Diagnostics);
        }

        protected virtual void GatherMemberDiagnostics(MappedMember member, List<Diagnostic> diagnostics)
        {
            if (member is AssociationMember assoc)
            {
                foreach (var column in assoc.KeyColumns)
                {
                    GatherColumnDiagnostics(column, diagnostics);
                }

                GatherReferencedEntityDiagnostics(assoc.RelatedEntity, diagnostics);

                foreach (var relatedColumn in assoc.RelatedKeyColumns)
                {
                    GatherColumnDiagnostics(relatedColumn, diagnostics);
                }
            }
            else if (member is CompoundMember compound)
            {
                foreach (var cm in compound.Members)
                {
                    GatherMemberDiagnostics(cm, diagnostics);
                }
            }
        }

        protected virtual MappedColumn GetMemberColumn(ColumnMember member, string? columnName, string? tableName)
        {
            return this.GetReferencedColumn(member.Entity, columnName ?? member.Member.Name, tableName, "Column Member", GetMemberPath(member));
        }

        protected virtual IReadOnlyList<MappedColumn> GetAssociationKeyColumns(
            AssociationMember association, string? keyColumns, string? tableName)
        {
            return this.GetNames(keyColumns ?? "")
                .Select(name => GetReferencedColumn(association.Entity, name, tableName, "Association Member", GetMemberPath(association)))
                .ToReadOnly();
        }

        protected virtual IReadOnlyList<MappedColumn> GetAssociationRelatedKeyColumns(
            AssociationMember association, string? relatedKeyColumns, string? keyColumns, string? tableName)
        {
            return this.GetNames(relatedKeyColumns ?? keyColumns ?? "")
                .Select(name => GetReferencedColumn(association.Entity, name, tableName, "Association Member", GetMemberPath(association)))
                .ToReadOnly();
        }

        protected virtual MappedEntity GetAssociationRelatedEntity(
            AssociationMember association, string? relatedEntityId)
        {
            var relatedEntityType = TypeHelper.GetSequenceElementType(association.Type);

            if (this.TryGetEntity(relatedEntityType, relatedEntityId, out var relatedEntity))
                return relatedEntity;

            return new UnknownEntity(
                relatedEntityType,
                relatedEntityId ?? "Unknown",
                new Diagnostic($"Member '{GetMemberPath(association)}': Unknown entity '{relatedEntityId}'.")
                );
        }

        protected virtual MappedTable GetRelatedTable(MappedTable table, string? relatedTableName)
        {
            if (relatedTableName != null)
            {
                if (table.Entity.TryGetTable(relatedTableName, out var relatedTable))
                    return relatedTable;

                return new UnknownTable(
                    relatedTableName,
                    new Diagnostic($"Extension table '{table.Name}': Unknown related table '{relatedTableName}'.")
                    );
            }
            else
            {
                return table.Entity.PrimaryTable;
            }
        }

        protected virtual IReadOnlyList<MappedColumn> GetTableKeyColumns(
            MappedTable table, string? keyColumns)
        {
            return this.GetNames(keyColumns ?? "")
                .Select(name => GetReferencedColumn(table.Entity, name, table.Name, "Extension Table", table.Name))
                .ToReadOnly();
        }

        protected virtual IReadOnlyList<MappedColumn> GetRelatedTableKeyColumns(
            MappedTable table, string? relatedKeyColumns, string? keyColumns, string? relatedTableName)
        {
            return this.GetNames(relatedKeyColumns ?? keyColumns ?? "")
                .Select(name => GetReferencedColumn(table.Entity, name, relatedTableName, "Extension Table", table.Name))

                .ToReadOnly();
        }

        protected virtual MappedColumn GetReferencedColumn(
            MappedEntity entity, string columnName, string? tableName, string sourceKind, string sourceName)
        {
            if (entity.TryGetColumn(columnName, tableName, out var column))
                return column;

            if (tableName != null && !entity.TryGetTable(tableName, out _))
                return new UnknownColumn(columnName, new Diagnostic($"{sourceKind} '{sourceName}': Unknown table '{tableName}'."));

            return new UnknownColumn(columnName, new Diagnostic($"{sourceKind} '{sourceName}': Unknown column '{columnName}'."));
        }

        protected virtual string GetMemberPath(MappedMember member)
        {
            if (member.Parent == null)
                return member.Member.Name;
            return GetMemberPath(member.Parent) + "." + member.Member.Name;
        }

        protected virtual MappedColumn CreateInferredColumn(
            MappedTable table,
            string name)
        {
            return new StandardColumn(
                table,
                name,
                columnType: null,
                isPrimaryKey: false,
                isReadOnly: false,
                isComputed: false,
                isGenerated: false,
                me => table.Entity.Members.OfType<ColumnMember>().FirstOrDefault(cm => cm.Column == me)
                );
        }



        protected class StandardEntity : MappedEntity
        {
            public StandardMapping Mapping { get; }
            public override string Id { get; }
            public override Type Type { get; }
            public override Type ConstructedType { get; }
            public override string? Context { get; }

            public override IReadOnlyList<MappedMember> Members => _mappedMembers.Value;
            private readonly Lazy<IReadOnlyList<MappedMember>> _mappedMembers;

            public override IReadOnlyList<MappedTable> Tables => _mappedTables.Value;
            private readonly Lazy<IReadOnlyList<MappedTable>> _mappedTables;

            public override IReadOnlyList<ColumnMember> PrimaryKeyMembers => _primaryKeyMembers.Value;
            private readonly Lazy<IReadOnlyList<ColumnMember>> _primaryKeyMembers;

            public override MappedTable PrimaryTable => _primaryTable.Value;
            private readonly Lazy<MappedTable> _primaryTable;

            public override IReadOnlyList<ExtensionTable> ExtensionTables => _extensionTables.Value;
            private readonly Lazy<IReadOnlyList<ExtensionTable>> _extensionTables;

            public override IReadOnlyList<MappedColumn> Columns => _columns.Value;
            private readonly Lazy<IReadOnlyList<MappedColumn>> _columns;

            public override IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Value;
            private readonly Lazy<IReadOnlyList<Diagnostic>> _diagnostics;

            public override bool TryGetMember(string name, [NotNullWhen(true)] out MappedMember? member) =>
                _memberMap.Value.TryGetValue(name, out member);
            private readonly Lazy<Dictionary<string, MappedMember>> _memberMap;

            public override bool TryGetTable(string name, [NotNullWhen(true)] out MappedTable? table) =>
                _tableMap.Value.TryGetValue(name, out table);
            private readonly Lazy<Dictionary<string, MappedTable>> _tableMap;

            public StandardEntity(
                StandardMapping mapping, 
                string entityId,
                Type type,
                Type constructedType,
                string? context,
                Func<MappedEntity, IReadOnlyList<MappedTable>> fnTables,
                Func<MappedEntity, IReadOnlyList<MappedMember>> fnMembers,
                Func<MappedEntity, IReadOnlyList<Diagnostic>> fnDiagnostics)
            {
                this.Mapping = mapping;
                this.Id = entityId;
                this.Type = type;
                this.ConstructedType = constructedType;
                this.Context = context;

                _mappedMembers = new Lazy<IReadOnlyList<MappedMember>>(
                    () => fnMembers(this), 
                    ReadOnlyList<MappedMember>.Empty
                    );

                _primaryKeyMembers = new Lazy<IReadOnlyList<ColumnMember>>(
                    () => this.Members.OfType<ColumnMember>().Where(m => m.Column.IsPrimaryKey).ToReadOnly(),
                    ReadOnlyList<ColumnMember>.Empty
                    );

                _mappedTables = new Lazy<IReadOnlyList<MappedTable>>(
                    () => fnTables(this), 
                    ReadOnlyList<MappedTable>.Empty
                    );

                _primaryTable = new Lazy<MappedTable>(() =>
                    this.Tables.First(t => !(t is ExtensionTable))
                    );

                _extensionTables = new Lazy<IReadOnlyList<ExtensionTable>>(
                    () => this.Tables.OfType<ExtensionTable>().ToReadOnly(),
                    ReadOnlyList<ExtensionTable>.Empty
                    );

                _columns = new Lazy<IReadOnlyList<MappedColumn>>(
                    () => this.Tables.SelectMany(t => t.Columns).ToReadOnly()
                    );

                _tableMap = new Lazy<Dictionary<string, MappedTable>>(
                    () => _mappedTables.Value.ToDictionary(t => t.Name)
                    );

                _memberMap = new Lazy<Dictionary<string, MappedMember>>(
                    () => _mappedMembers.Value.ToDictionary(m => m.Member.Name)
                    );

                _diagnostics = new Lazy<IReadOnlyList<Diagnostic>>(
                    () => fnDiagnostics(this), 
                    ReadOnlyList<Diagnostic>.Empty
                    );
            }
        }

        protected class StandardPrimaryTable : PrimaryTable
        {
            public override MappedEntity Entity { get; }
            public override string Name { get; }

            public override IReadOnlyList<MappedColumn> Columns => _columns.Value;
            private readonly Lazy<IReadOnlyList<MappedColumn>> _columns;

            public override bool TryGetColumn(string name, [NotNullWhen(true)] out MappedColumn? column) =>
                _nameToColumnMap.Value.TryGetValue(name, out column);
            private readonly Lazy<Dictionary<string, MappedColumn>> _nameToColumnMap;

            public StandardPrimaryTable(
                MappedEntity entity,
                string tableName,
                Func<MappedTable, IReadOnlyList<MappedColumn>> fnColumns)
            {
                this.Entity = entity;
                this.Name = tableName;

                _columns = new Lazy<IReadOnlyList<MappedColumn>>(
                    () => fnColumns(this)
                    );

                _nameToColumnMap = new Lazy<Dictionary<string, MappedColumn>>(() =>
                    _columns.Value.ToDictionary(c => c.Name)
                    );
            }
        }

        protected class StandardExtensionTable : ExtensionTable
        {
            public override MappedEntity Entity { get; }
            public override string Name { get; }

            private readonly Lazy<IReadOnlyList<MappedColumn>> _columns;
            public override IReadOnlyList<MappedColumn> Columns =>
                _columns.Value;

            public override bool TryGetColumn(string name, [NotNullWhen(true)] out MappedColumn? column) =>
                _nameToColumnMap.Value.TryGetValue(name, out column);
            private readonly Lazy<Dictionary<string, MappedColumn>> _nameToColumnMap;

            private readonly Lazy<IReadOnlyList<MappedColumn>> _keyColumns;
            public override IReadOnlyList<MappedColumn> KeyColumns =>
                _keyColumns.Value;

            private readonly Lazy<MappedTable> _relatedTable;
            public override MappedTable RelatedTable =>
                _relatedTable.Value;

            private readonly Lazy<IReadOnlyList<MappedColumn>> _relatedKeyColumns;
            public override IReadOnlyList<MappedColumn> RelatedKeyColumns =>
                _relatedKeyColumns.Value;

            public StandardExtensionTable(
                MappedEntity entity,
                string tableName,
                Func<MappedTable, IReadOnlyList<MappedColumn>> fnColumns,
                Func<ExtensionTable, IReadOnlyList<MappedColumn>> fnKeyColumns,
                Func<ExtensionTable, MappedTable> fnRelatedTable,
                Func<ExtensionTable, IReadOnlyList<MappedColumn>> fnRelatedKeyColumns)
            {
                this.Entity = entity;
                this.Name = tableName;

                _columns = new Lazy<IReadOnlyList<MappedColumn>>(
                    () => fnColumns(this)
                    );

                _nameToColumnMap = new Lazy<Dictionary<string, MappedColumn>>(() =>
                    _columns.Value.ToDictionary(c => c.Name)
                    );

                _keyColumns = new Lazy<IReadOnlyList<MappedColumn>>(
                    () => fnKeyColumns(this),
                    ReadOnlyList<MappedColumn>.Empty
                    );

                _relatedTable = new Lazy<MappedTable>(
                    () => fnRelatedTable(this)
                    );

                _relatedKeyColumns = new Lazy<IReadOnlyList<MappedColumn>>(
                    () => fnRelatedKeyColumns(this),
                    ReadOnlyList<MappedColumn>.Empty
                    );
            }
        }

        protected class StandardColumn : MappedColumn
        {
            public override MappedTable Table { get; }
            public override string Name { get; }
            public override string? Type { get; }
            public override bool IsPrimaryKey { get; }
            public override bool IsReadOnly { get; }
            public override bool IsComputed { get; }
            public override bool IsGenerated { get; }

            public override ColumnMember? Member => _member?.Value;
            private readonly Lazy<ColumnMember?>? _member;

            public StandardColumn(
                MappedTable table,
                string name,
                string? columnType,
                bool isPrimaryKey,
                bool isReadOnly,
                bool isComputed,
                bool isGenerated,
                Func<MappedColumn, ColumnMember?>? fnMember)
            {
                this.Table = table;
                this.Name = name;
                this.Type = columnType;
                this.IsPrimaryKey = isPrimaryKey;
                this.IsReadOnly = isReadOnly;
                this.IsComputed = isComputed;
                this.IsGenerated = isGenerated;
                _member = fnMember != null
                    ? new Lazy<ColumnMember?>(() => fnMember(this))
                    : null;
            }
        }

        protected class StandardColumnMember : ColumnMember
        {
            /// <summary>
            /// The entity this column member is part of.
            /// </summary>
            public override MappedEntity Entity { get; }

            /// <summary>
            /// The parent member if this member is nested within another member.
            /// </summary>
            public override MappedMember? Parent { get; }

            /// <summary>
            /// The member of the entity type.
            /// </summary>
            public override MemberInfo Member { get; }

            /// <summary>
            /// The table column that the member is mapped to.
            /// </summary>
            public override MappedColumn Column => _column.Value;
            private readonly Lazy<MappedColumn> _column;

            public StandardColumnMember(
                MappedEntity entity,
                MappedMember? parent,
                MemberInfo member,
                Func<StandardColumnMember, MappedColumn> fnColumn
                )
            {
                this.Entity = entity;
                this.Parent = parent;
                this.Member = member;
                _column = new Lazy<MappedColumn>(() => fnColumn(this));
            }
        }

        protected class StandardCompoundMember : CompoundMember
        {
            public override MappedEntity Entity { get; }
            public override MappedMember? Parent { get; }
            public override MemberInfo Member { get; }
            public override Type ConstructedType { get; }

            public override IReadOnlyList<MappedMember> Members => _members.Value;
            private readonly Lazy<IReadOnlyList<MappedMember>> _members;

            public StandardCompoundMember(
                MappedEntity entity,
                MappedMember? parent,
                MemberInfo member,
                Type constructedType,
                Func<MappedMember, IReadOnlyList<MappedMember>> fnMembers)
            {
                this.Entity = entity;
                this.Parent = parent;
                this.Member = member;
                this.ConstructedType = constructedType;
                _members = new Lazy<IReadOnlyList<MappedMember>>(() => fnMembers(this));
            }
        }

        protected class StandardAssociationMember : AssociationMember
        {
            public override MappedEntity Entity { get; }
            public override MappedMember? Parent { get; }
            public override MemberInfo Member { get; }
            public override bool IsSource { get; }

            public override bool IsTarget =>
                !IsSource;

            public override bool IsOneToOne =>
                !TypeHelper.IsSequenceType(TypeHelper.GetMemberType(this.Member));

            private readonly Lazy<IReadOnlyList<MappedColumn>> _keyMembers;
            public override IReadOnlyList<MappedColumn> KeyColumns =>
                _keyMembers.Value;

            private readonly Lazy<MappedEntity> _relatedEntity;
            public override MappedEntity RelatedEntity =>
                _relatedEntity.Value;

            private readonly Lazy<IReadOnlyList<MappedColumn>> _relatedKeyMembers;
            public override IReadOnlyList<MappedColumn> RelatedKeyColumns =>
                _relatedKeyMembers.Value;

            public StandardAssociationMember(
                MappedEntity entity,
                MappedMember? parent,
                MemberInfo member,
                bool isSource,
                Func<AssociationMember, IReadOnlyList<MappedColumn>> fnKeyColumns,
                Func<AssociationMember, MappedEntity> fnRelatedEntity,
                Func<AssociationMember, IReadOnlyList<MappedColumn>> fnRelatedKeyColumns)
            {
                this.Entity = entity;
                this.Parent = parent;
                this.Member = member;
                this.IsSource = isSource;
                _keyMembers = new Lazy<IReadOnlyList<MappedColumn>>(() => fnKeyColumns(this));
                _relatedEntity = new Lazy<MappedEntity>(() => fnRelatedEntity(this));
                _relatedKeyMembers = new Lazy<IReadOnlyList<MappedColumn>>(() => fnRelatedKeyColumns(this));
            }
        }

        protected virtual bool TryGetType(string name, out Type type)
        {
            type = Type.GetType(name);
            
            if (type == null)
            {
                // look for type in assemblies that reference the toolkit
                foreach (var assembly in Factories.EntityProviderFactoryRegistry.Singleton.SearchAssemblies)
                {
                    type = assembly.GetType(name);
                    if (type != null)
                        break;
                }
            }

            return type != null;
        }

        protected class UnknownEntity : MappedEntity
        {
            public override Type Type { get; }
            public override string Id { get; }
            public override IReadOnlyList<Diagnostic> Diagnostics { get; }

            private UnknownEntity(Type type, string id, IReadOnlyList<Diagnostic> diagnostics)
            {
                this.Type = type;
                this.Id = id;
                this.Diagnostics = diagnostics.ToReadOnly();
            }

            public UnknownEntity(Type type, string id, Diagnostic diagnostics)
                : this(type, id, new[] { diagnostics })
            {
            }

            public override string? Context => null;
            public override Type ConstructedType => typeof(object);
            public override IReadOnlyList<MappedMember> Members => ReadOnlyList<MappedMember>.Empty;
            public override IReadOnlyList<ColumnMember> PrimaryKeyMembers => ReadOnlyList<ColumnMember>.Empty;
            public override IReadOnlyList<MappedTable> Tables => ReadOnlyList<MappedTable>.Empty;
            public override MappedTable PrimaryTable => throw new NotImplementedException();
            public override IReadOnlyList<ExtensionTable> ExtensionTables => ReadOnlyList<ExtensionTable>.Empty;
            public override IReadOnlyList<MappedColumn> Columns => ReadOnlyList<MappedColumn>.Empty;

            public override bool TryGetMember(string name, [NotNullWhen(true)] out MappedMember? member)
            {
                member = null;
                return false;
            }

            public override bool TryGetTable(string name, [NotNullWhen(true)] out MappedTable? table)
            {
                table = null;
                return false;
            }

            public static readonly UnknownEntity Default = 
                new UnknownEntity(typeof(object), "Unknown", ReadOnlyList<Diagnostic>.Empty);
        }

        public class UnknownTable : MappedTable
        {
            public override string Name { get; }
            public IReadOnlyList<Diagnostic> Diagnostics { get; }

            private UnknownTable(string tableName, IReadOnlyList<Diagnostic> diagnostics)
            {
                this.Name = tableName;
                this.Diagnostics = diagnostics.ToReadOnly();
            }

            public UnknownTable(string tableName, Diagnostic diagnostic)
                : this(tableName, new[] { diagnostic })
            {
            }

            public override MappedEntity Entity => UnknownEntity.Default;
            public override IReadOnlyList<MappedColumn> Columns => ReadOnlyList<MappedColumn>.Empty;

            public override bool TryGetColumn(string name, [NotNullWhen(true)] out MappedColumn? column)
            {
                column = null;
                return false;
            }

            public static readonly UnknownTable Default =
                new UnknownTable("Unknown", ReadOnlyList<Diagnostic>.Empty);
        }

        public class UnknownColumn : MappedColumn
        {
            public override string Name { get; }
            public IReadOnlyList<Diagnostic> Diagnostics { get; }

            public UnknownColumn(string columnName, Diagnostic diagnostic)
            {
                this.Name = columnName;
                this.Diagnostics = new[] { diagnostic }.ToReadOnly();
            }

            public override MappedTable Table => UnknownTable.Default;
            public override ColumnMember? Member => null;
            public override string? Type => null;
            public override bool IsPrimaryKey => false;
            public override bool IsReadOnly => false;
            public override bool IsComputed => false;
            public override bool IsGenerated => false;
        }
    }
}
