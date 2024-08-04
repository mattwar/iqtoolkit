// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Entities.Mapping
{
    using System.Collections.Immutable;
    using Utils;

    public abstract class StandardMapping : EntityMapping
    {
        private ImmutableDictionary<string, MappedEntity> _idToEntityMap;
        private ImmutableDictionary<string, MappedTable> _idToTableMap;

        public override Type? ContextType { get; }

        /// <summary>
        /// The set of members that refer to entity tables on the context type.
        /// </summary>
        public override IReadOnlyList<MemberInfo> ContextMembers =>
            _contextMembers.Value;
        private readonly Lazy<IReadOnlyList<MemberInfo>> _contextMembers;

        public StandardMapping(Type? contextType)
        {
            this.ContextType = contextType;
            _idToEntityMap = ImmutableDictionary<string, MappedEntity>.Empty;
            _idToTableMap = ImmutableDictionary<string, MappedTable>.Empty;

            _contextMembers = new Lazy<IReadOnlyList<MemberInfo>>(() =>
                contextType != null
                    ? TypeHelper.GetDeclaredFieldsAndProperties(
                        contextType,
                        m => TypeHelper.IsAssignableToGeneric(TypeHelper.GetMemberType(m), typeof(IEntityTable<>))
                        )
                    : ReadOnlyList<MemberInfo>.Empty
            );
        }

        protected virtual void InitializeContextMembers()
        {
            // pre-load entities for context members
            foreach (var m in this.ContextMembers)
            {
                var _ = GetEntity(m);
            }
        }

        /// <summary>
        /// Gets the entity id for the context member.
        /// </summary>
        protected virtual string GetEntityId(
            MemberInfo contextMember)
        {
            return contextMember.Name;
        }

        /// <summary>
        /// Gets the entity id for the entity type.
        /// </summary>
        protected virtual string GetEntityId(
            Type entityType)
        {
            return entityType.Name;
        }

        /// <summary>
        /// Get the <see cref="MappedEntity"/> represented by the IQueryable context member
        /// </summary>
        public override MappedEntity GetEntity(
            MemberInfo contextMember)
        {
            var entityType = TypeHelper.GetEntityType(contextMember);
            return GetEntity(entityType, GetEntityId(contextMember));
        }

        /// <summary>
        /// Gets the context member associated with the entity id.
        /// </summary>
        public virtual bool TryGetContextMember(string entityId, out MemberInfo member)
        {
            member = this.ContextMembers.FirstOrDefault(m => GetEntityId(m) == entityId);
            return member != null;
        }

        /// <summary>
        /// Gets the context member associated with the entity type.
        /// </summary>
        public virtual bool TryGetContextMember(Type entityType, out MemberInfo member)
        {
            member = this.ContextMembers.FirstOrDefault(m => TypeHelper.GetEntityType(m) == entityType);
            return member != null;
        }

        /// <summary>
        /// Gets the <see cref="MappedEntity"/> for the entity id.
        /// </summary>
        public override MappedEntity GetEntity(
            Type entityType, string? entityId)
        {
            return GetOrCreateEntity(entityType, entityId ?? GetEntityId(entityType), null);
        }

        /// <summary>
        /// Gets or creates the <see cref="MappedEntity"/> for the entity id.
        /// </summary>
        protected virtual MappedEntity GetOrCreateEntity(
            Type entityType, string entityId, ParentEntity? parent)
        {
            if (!_idToEntityMap.TryGetValue(entityId, out var mappedEntity))
            {
                var tmp = CreateEntity(entityType, entityId, parent)!;
                mappedEntity = ImmutableInterlocked.GetOrAdd(ref _idToEntityMap, entityId, tmp);
            }

            return mappedEntity;
        }

        /// <summary>
        /// Create a <see cref="MappedEntity"/> from attributes on the entity or context type.
        /// </summary>
        protected abstract MappedEntity CreateEntity(
            Type entityType, string entityId, ParentEntity? parent);

        /// <summary>
        /// Gets the 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="tableId"></param>
        /// <returns></returns>
        protected virtual MappedTable GetOrCreateTable(
            MappedEntity entity, string tableId, ParentEntity? parent)
        {
            if (!_idToTableMap.TryGetValue(tableId, out var table))
            {
                var tmp = CreateTable(entity, tableId, parent);
                table = ImmutableInterlocked.GetOrAdd(ref _idToTableMap, tableId, tmp);
            }

            return table;
        }

        /// <summary>
        /// Create the <see cref="MappedTable"/> for the table id.
        /// </summary>
        protected abstract MappedTable CreateTable(
            MappedEntity entity, string tableId, ParentEntity? parent);

        protected class StandardEntity : MappedEntity
        {
            public StandardMapping Mapping { get; }
            public override string EntityId { get; }
            public override Type Type { get; }
            public override Type ConstructedType { get; }

            public StandardEntity(
                StandardMapping mapping, 
                string entityId,
                Type type,
                Type constructedType,
                Func<MappedEntity, IReadOnlyList<MappedTable>> fnTables,
                Func<MappedEntity, IReadOnlyList<MappedMember>> fnMembers)
            {
                this.Mapping = mapping;
                this.EntityId = entityId;
                this.Type = type;
                this.ConstructedType = constructedType;
                _mappedMembers = new Lazy<IReadOnlyList<MappedMember>>(() => fnMembers(this), ReadOnlyList<MappedMember>.Empty);
                _mappedTables = new Lazy<IReadOnlyList<MappedTable>>(() => fnTables(this), ReadOnlyList<MappedTable>.Empty);
                _columnMembers = new Lazy<IReadOnlyList<MappedColumnMember>>(
                    () => this.MappedMembers.OfType<MappedColumnMember>().ToReadOnly(),
                    ReadOnlyList<MappedColumnMember>.Empty
                    );
                _relationshipMembers = new Lazy<IReadOnlyList<MappedRelationshipMember>>(
                    () => this.MappedMembers.OfType<MappedRelationshipMember>().ToReadOnly(),
                    ReadOnlyList<MappedRelationshipMember>.Empty
                    );
                _primaryKeyMembers = new Lazy<IReadOnlyList<MappedColumnMember>>(
                    () => this.MappedMembers.OfType<MappedColumnMember>().Where(m => m.IsPrimaryKey).ToReadOnly(),
                    ReadOnlyList<MappedColumnMember>.Empty
                    );
                _primaryTable = new Lazy<MappedTable>(() =>
                    this.Tables.First(t => !(t is MappedExtensionTable))
                    );
                _extensionTables = new Lazy<IReadOnlyList<MappedExtensionTable>>(
                    () => this.Tables.OfType<MappedExtensionTable>().ToReadOnly(),
                    ReadOnlyList<MappedExtensionTable>.Empty
                    );
            }

            private readonly Lazy<IReadOnlyList<MappedMember>> _mappedMembers;
            public override IReadOnlyList<MappedMember> MappedMembers =>
                _mappedMembers.Value;

            private readonly Lazy<IReadOnlyList<MappedTable>> _mappedTables;
            public override IReadOnlyList<MappedTable> Tables =>
                _mappedTables.Value;

            private readonly Lazy<IReadOnlyList<MappedColumnMember>> _columnMembers;
            public override IReadOnlyList<MappedColumnMember> ColumnMembers =>
                _columnMembers.Value;

            private readonly Lazy<IReadOnlyList<MappedRelationshipMember>> _relationshipMembers;
            public override IReadOnlyList<MappedRelationshipMember> RelationshipMembers =>
                _relationshipMembers.Value;

            private readonly Lazy<IReadOnlyList<MappedColumnMember>> _primaryKeyMembers;
            public override IReadOnlyList<MappedColumnMember> PrimaryKeyMembers =>
                _primaryKeyMembers.Value;

            private readonly Lazy<MappedTable> _primaryTable;
            public override MappedTable PrimaryTable =>
                _primaryTable.Value;

            private readonly Lazy<IReadOnlyList<MappedExtensionTable>> _extensionTables;
            public override IReadOnlyList<MappedExtensionTable> ExtensionTables =>
                _extensionTables.Value;

        }

        protected class StandardTable : MappedTable
        {
            public override MappedEntity Entity { get; }
            public override string TableId { get; }
            public override string TableName { get; }

            public StandardTable(
                MappedEntity entity,
                string tableId,
                string tableName)
            {
                this.Entity = entity;
                this.TableId = tableId;
                this.TableName = tableName;
            }
        }

        protected class StandardExtensionTable : MappedExtensionTable
        {
            public override MappedEntity Entity { get; }
            public override string TableId { get; }
            public override string TableName { get; }

            public StandardExtensionTable(
                MappedEntity entity,
                string tableId,
                string tableName,
                Func<MappedTable> fnRelatedTable,
                Func<MappedExtensionTable, IReadOnlyList<string>> fnKeyColumnNames,
                Func<MappedExtensionTable, IReadOnlyList<MappedColumnMember>> fnRelatedMembers)
            {
                this.Entity = entity;
                this.TableId = tableId;
                this.TableName = tableName;
                _relatedTable = new Lazy<MappedTable>(fnRelatedTable);
                _keyColumnNames = new Lazy<IReadOnlyList<string>>(
                    () => fnKeyColumnNames(this),
                    ReadOnlyList<string>.Empty
                    );
                _relatedMembers = new Lazy<IReadOnlyList<MappedColumnMember>>(
                    () => fnRelatedMembers(this),
                    ReadOnlyList<MappedColumnMember>.Empty
                    );
            }

            private readonly Lazy<MappedTable> _relatedTable;
            public override MappedTable RelatedTable =>
                _relatedTable.Value;

            private readonly Lazy<IReadOnlyList<string>> _keyColumnNames;
            public override IReadOnlyList<string> KeyColumnNames =>
                _keyColumnNames.Value;

            private readonly Lazy<IReadOnlyList<MappedColumnMember>> _relatedMembers;
            public override IReadOnlyList<MappedColumnMember> RelatedMembers =>
                _relatedMembers.Value;
        }

        protected class StandardColumnMember : MappedColumnMember
        {
            public override MappedEntity Entity { get; }
            public override MemberInfo Member { get; }
            public override MappedTable Table { get; }
            public override string ColumnName { get; }
            public override string? ColumnType { get; }
            public override bool IsPrimaryKey { get; }
            public override bool IsReadOnly { get; }
            public override bool IsComputed { get; }
            public override bool IsGenerated { get; }

            public StandardColumnMember(
                MappedEntity entity,
                MemberInfo member,
                MappedTable table,
                string columnName,
                string? columnType,
                bool isPrimaryKey,
                bool isReadOnly,
                bool isComputed,
                bool isGenerated
                )
            {
                this.Entity = entity;
                this.Member = member;
                this.ColumnName = columnName;
                this.ColumnType = columnType;
                this.Table = table;
                this.IsPrimaryKey = isPrimaryKey;
                this.IsReadOnly = isReadOnly;
                this.IsComputed = isComputed;
                this.IsGenerated = isGenerated;
            }
        }

        protected class StandardAssociationMember : MappedAssociationMember
        {
            public override MappedEntity Entity { get; }
            public override MemberInfo Member { get; }
            public override bool IsSource { get; }

            public StandardAssociationMember(
                MappedEntity entity,
                MemberInfo member,
                bool isSource,
                Func<MappedAssociationMember, IReadOnlyList<MappedColumnMember>> fnKeyMembers,
                Func<MappedAssociationMember, MappedEntity> fnRelatedEntity,
                Func<MappedAssociationMember, IReadOnlyList<MappedColumnMember>> fnRelatedKeyMembers)
            {
                this.Entity = entity;
                this.Member = member;
                _keyMembers = new Lazy<IReadOnlyList<MappedColumnMember>>(() => fnKeyMembers(this));
                _relatedEntity = new Lazy<MappedEntity>(() => fnRelatedEntity(this));
                _relatedKeyMembers = new Lazy<IReadOnlyList<MappedColumnMember>>(() => fnRelatedKeyMembers(this));
            }

            public override bool IsTarget =>
                !IsSource;

            public override bool IsSingleton =>
                TypeHelper.IsSequenceType(TypeHelper.GetMemberType(this.Member));

            private readonly Lazy<IReadOnlyList<MappedColumnMember>> _keyMembers;
            public override IReadOnlyList<MappedColumnMember> KeyMembers =>
                _keyMembers.Value;

            private readonly Lazy<MappedEntity> _relatedEntity;
            public override MappedEntity RelatedEntity =>
                _relatedEntity.Value;

            private readonly Lazy<IReadOnlyList<MappedColumnMember>> _relatedKeyMembers;
            public override IReadOnlyList<MappedColumnMember> RelatedKeyMembers =>
                _relatedKeyMembers.Value;
        }

        protected class StandardNestedEntityMember : MappedNestedEntityMember
        {
            public override MappedEntity Entity { get; }
            public override MemberInfo Member { get; }

            public StandardNestedEntityMember(
                MappedEntity entity,
                MemberInfo member,
                Func<MappedNestedEntityMember, MappedEntity> fnRelatedEntity)
            {
                this.Entity = entity;
                this.Member = member;
                _relatedEntity = new Lazy<MappedEntity>(() => fnRelatedEntity(this));
            }

            public override bool IsSource => true;
            public override bool IsTarget => false;
            public override bool IsSingleton => true;

            private readonly Lazy<MappedEntity> _relatedEntity;
            public override MappedEntity RelatedEntity =>
                _relatedEntity.Value;
        }

        protected class ParentEntity
        {
            public ParentEntity? Parent { get; }
            public MemberInfo Member { get; }
            public Type EntityType { get; }
            public string EntityId { get; }

            public ParentEntity(ParentEntity? parent, MemberInfo member, Type entityType, string entityId)
            {
                this.Parent = parent;
                this.Member = member;
                this.EntityType = entityType;
                this.EntityId = entityId;
            }

            public ParentEntity Root
            {
                get
                {
                    var p = this;

                    while (p.Parent != null)
                    {
                        p = p.Parent;
                    }

                    return p;
                }
            }
        }
    }
}
