// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace IQToolkit.Entities.Mapping
{
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using Utils;

#if false
    <Mapping>
        <Entity Id="EC" Type="Customer" ConstructedType="Customer" Context="Customers" >
            <ColumnMember Name="CustomerID" Column="CustomerID" Table="Customers" />
            <CompoundMember Name="Address" Type="IAddress" ConstructedType="Address">
                <Member Name="Street" Column="Address" Table="..." />
                <Member Name="City" Column="City" Table="..." />
            </CompoundMember>
            <AssociationMember Name="Orders"
                Table="Customers"
                KeyColumns="CustomerID" 
                RelatedEntityId="EO"
                RelatedTable="Orders"
                RelatedKeyColumns="CustomerID",
                IsForeignKey="false"
                />
            <Table Name="Customers">
                <Column Name="CustomerID" Type="NVARCHAR" IsPrimaryKey="true" />
                <Column Name="ContactName" Type="NVARCHAR" />              
                <Column Name="City" />
            </Table>
            <ExtensionTable Name="..." KeyColumns="" RelatedTable="" RelatedKeyColumns="">
                <Column Name=".." />
            </ExtensionTable>
        </Entity>
        <Entity Id="EO" Type="Order" ConstructedType="Order Context="Orders">
            <ColumnMember Name="OrderID" Column="OrderID" Table="Orders" />
            <AssociationMember Name="Customer"
                KeyColumns="CustomerID" 
                IsForeignKey="true" // KeyColumns are a foreign key to customers table (important for insert/update order)
                RelatedEntityId="EC"
                RelatedTable="Customers"
                RelatedKeyColumns="CustomerID"
                />
            <Table Name="Orders">
                <Column Name="OrderID" Type="INT" IsPrimaryKey="true" />
                <Column Name="CustomerID" Type="NVARCHAR" />
            </Table>
        </Entity>
    </Mapping>

#endif

    /// <summary>
    /// An <see cref="EntityMapping"/> for mapping stored in an XML document.
    /// </summary>
    public class XmlMapping : StandardMapping
    {
        private readonly IReadOnlyList<XElement> _entityElements;
        private readonly Dictionary<string, XElement> _entityIdToElementsMap;
        private readonly Dictionary<string, XElement> _entityTypeNameToElementsMap;
        private readonly Dictionary<string, XElement> _contextMemberNameToElementsMap;

        /// <summary>
        /// Constructs a new instance of <see cref="XmlMapping"/>
        /// </summary>
        public XmlMapping(string xml)
        {
            _entityElements = XElement.Parse(xml)
                .Elements(EntityName)
                .Where(e => GetId(e) != null)
                .ToReadOnly();

            _entityIdToElementsMap = _entityElements
                .ToDictionary_FirstWins(GetId)!;

            _contextMemberNameToElementsMap = _entityElements
                .Where(e => GetContext(e) != null)
                .ToDictionary_FirstWins(GetContext)!;

            _entityTypeNameToElementsMap = _entityElements
                .Where(e => GetEntityType(e) != null)
                .ToDictionary_FirstWins(GetEntityType)!;

            // pre-create all declared entities w/ types
            foreach (var kvp in _entityTypeNameToElementsMap)
            {
                if (this.TryGetType(kvp.Key, out var entityType))
                {
                    var id = GetId(kvp.Value);
                    this.TryGetEntity(entityType, id, out var entity);
                }
            }
        }

        public override bool TryGetEntity(
            Type entityType, 
            string? entityId, 
            [NotNullWhen(true)] out MappedEntity? entity)
        {
            if (entityId == null)
            {
                // get entity id of entity associated with the entity type
                if ((_entityTypeNameToElementsMap.TryGetValue(entityType.FullName, out var element)
                    || _entityTypeNameToElementsMap.TryGetValue(entityType.Name, out element)))
                {
                    entityId = GetId(element);
                }
            }

            if (entityId == null)
                entityId = entityType.Name;

            entity = this.GetOrCreateEntity(entityType, entityId);
            return entity != null;
        }

        public override bool TryGetEntity(
            MemberInfo contextMember,
            [NotNullWhen(true)] out MappedEntity? entity)
        {
            // type can be passed in as memberinfo by mistake
            if (contextMember is Type type)
                return TryGetEntity(type, null, out entity);

            var contextMemberEntityType = TypeHelper.GetEntityType(contextMember);

            // get id of entity from element associated with the member
            if (!_contextMemberNameToElementsMap.TryGetValue(contextMember.Name, out var element))
            {
                return TryGetEntity(contextMemberEntityType, GetId(element)!, out entity);
            }

            return TryGetEntity(contextMemberEntityType, null, out entity);
        }

        protected override MappedEntity CreateEntity(
            Type entityType, string entityId)
        {
            _entityIdToElementsMap.TryGetValue(entityId, out var entityElement);
            return CreateEntity(entityType, entityId, entityElement);
        }

        private MappedEntity CreateEntity(
            Type entityType, string entityId, XElement? entityElement)
        {
            var constructedType = GetConstructedType(entityElement) is { } typeName && this.TryGetType(typeName, out var typeFromElement)
                ? typeFromElement
                : entityType;

            var context = GetContext(entityElement);

            // build map of all members to their mapping elements
            Dictionary<MemberInfo, XElement>? memberToElementMap = null;
            if (entityElement != null)
            {
                memberToElementMap = new Dictionary<MemberInfo, XElement>();
                GetMemberElements(entityType, entityElement, memberToElementMap);
            }

            return new StandardEntity(
                this,
                entityId,
                entityType,
                constructedType,
                context,
                me => CreateEntityTables(me, entityElement, memberToElementMap),
                me => CreateMembers(me, null, memberToElementMap),
                me => GetEntityDiagnostics(me)
                );
        }

        private void GetMemberElements(
            Type type,
            XElement element,
            Dictionary<MemberInfo, XElement> map)
        {
            foreach (var prop in element.Elements().Where(IsMember))
            {
                if (GetName(prop) is { } name 
                    && type.TryGetDeclaredFieldOrProperty(name, out var member))
                {
                    map[member] = prop;

                    var memberType = TypeHelper.GetMemberType(member);
                    GetMemberElements(memberType, prop, map);
                }
            }
        }

        protected virtual IReadOnlyList<MappedTable> CreateEntityTables(
            MappedEntity entity, 
            XElement? entityElement,
            Dictionary<MemberInfo, XElement>? memberToElementMap)
        {
            var tableElement = entityElement?.Element(TableName);
            var extTableElements = entityElement?.Elements(ExtendedTableName);

            var tables = new List<MappedTable>();

            var primaryTable = CreateTable(entity, entityElement, tableElement, memberToElementMap);
            tables.Add(primaryTable);

            if (extTableElements != null)
            {
                foreach (var extTableElement in extTableElements)
                {
                    var extTable = CreateTable(entity, entityElement, extTableElement, memberToElementMap);
                    tables.Add(extTable);
                }
            }

            return tables.ToReadOnly();
        }

        protected virtual IReadOnlyList<MappedMember> CreateMembers(
            MappedEntity entity,
            MappedMember? parent,
            Dictionary<MemberInfo, XElement>? memberToElementMap)
        {
            var declaringType = parent != null
                ? TypeHelper.GetSequenceElementType(parent.Type)
                : entity.Type;

            var mappedMembers = new List<MappedMember>();

            foreach (var member in declaringType.GetDeclaredFieldsAndProperties())
            {
                // find mapping info for member
                XElement? memberElement = null;
                memberToElementMap?.TryGetValue(member, out memberElement);

                if (TryCreateMember(entity, parent, member, memberElement, memberToElementMap, out var mappedMember))
                {
                    mappedMembers.Add(mappedMember);
                }
            }

            return mappedMembers.ToReadOnly();
        }

        protected virtual bool TryCreateMember(
            MappedEntity entity,
            MappedMember? parent,
            MemberInfo member,
            XElement? memberElement,
            Dictionary<MemberInfo, XElement>? memberToElementMap,
            [NotNullWhen(true)] out MappedMember? mappedMember)
        {
            // associations have key columns
            if (memberElement != null)
            {
                if (IsAssociationMember(memberElement)
                    && GetKeyColumns(memberElement) is { } keyColumns)
                {
                    var isForeignKey = GetIsForeignKey(memberElement);

                    mappedMember = new StandardAssociationMember(
                        entity,
                        parent,
                        member,
                        isForeignKey,
                        fnKeyColumns: 
                            me => this.GetAssociationKeyColumns(me, GetKeyColumns(memberElement), GetTableName(memberElement)),
                        fnRelatedEntity: 
                            me => this.GetAssociationRelatedEntity(me, GetRelatedEntityId(memberElement)),
                        fnRelatedKeyColumns: 
                            me => this.GetAssociationRelatedKeyColumns(me, GetRelatedKeyColumns(memberElement), GetKeyColumns(memberElement), GetTableName(memberElement))
                        );

                    return true;
                }
                // compound members have nested property elements
                else if (IsCompoundMember(memberElement))
                {
                    var constructedType = GetConstructedType(memberElement) is string typeName
                        && this.TryGetType(typeName, out var typeFromElement)
                        ? typeFromElement
                        : TypeHelper.GetEntityType(member);

                    mappedMember = new StandardCompoundMember(
                        entity,
                        parent,
                        member,
                        constructedType,
                        me => this.CreateMembers(entity, me, memberToElementMap)
                        );

                    return true;
                }
                else if (IsColumnMember(memberElement))
                {
                    // anything else must be a single column member
                    var memberType = TypeHelper.GetMemberType(member);

                    mappedMember = new StandardColumnMember(
                        entity,
                        parent,
                        member,
                        fnColumn:
                            me => this.GetMemberColumn(me, GetColumnName(memberElement), GetTableName(memberElement))
                        );

                    return true;
                }
            }
            else if (IsPossibleColumnMember(member))
            {
                // infer this member to be mapped to a column of the same name
                var memberType = TypeHelper.GetMemberType(member);
                mappedMember = new StandardColumnMember(
                    entity,
                    parent,
                    member,
                    fnColumn:
                        me => this.GetMemberColumn(me, null, me.Entity.PrimaryTable.Name)
                    );

                return true;
            }
            else if (IsPossibleCompoundMember(member))
            {
                // infer this member to be a compound member
                mappedMember = new StandardCompoundMember(
                    entity,
                    parent,
                    member,
                    TypeHelper.GetMemberType(member),
                    me => this.CreateMembers(entity, me, memberToElementMap)
                    );

                return true;
            }

            mappedMember = null;
            return false;
        }


        private MappedTable CreateTable(
            MappedEntity entity, 
            XElement? entityElement,
            XElement? tableElement,
            Dictionary<MemberInfo, XElement>? memberToElementMap)
        {
            var tableName = GetName(tableElement) 
                ?? entity.Type.Name;

            if (tableElement?.Name == ExtendedTableName)
            {
                return new StandardExtensionTable(
                    entity,
                    tableName,
                    fnColumns: 
                        me => CreateTableColumns(me, entityElement, tableElement, memberToElementMap),
                    fnKeyColumns: 
                        me => this.GetTableKeyColumns(me, GetKeyColumns(tableElement)),
                    fnRelatedTable: 
                        me => this.GetRelatedTable(me, GetRelatedTableName(tableElement)),
                    fnRelatedKeyColumns: 
                        me => this.GetRelatedTableKeyColumns(me, GetRelatedKeyColumns(tableElement), GetKeyColumns(tableElement), GetRelatedTableName(tableElement))
                    );
            }
            else
            {
                return new StandardPrimaryTable(
                    entity,
                    tableName,
                    fnColumns:
                        me => CreateTableColumns(me, entityElement, tableElement, memberToElementMap)
                    );
            }
        }

        private IReadOnlyList<MappedColumn> CreateTableColumns(
            MappedTable table, 
            XElement? entityElement,
            XElement? tableElement,
            Dictionary<MemberInfo, XElement>? memberToElementMap)
        {
            // all the columns declared as part of the table
            // + any additional columns referenced in a property
            // + all the unmapped members (if primary table)

            var columns = new List<MappedColumn>();
            var declared = new HashSet<string>();

            // all columns declared in table element
            if (tableElement != null)
            {
                foreach (var colElement in GetColumns(tableElement))
                {
                    if (TryCreateColumn(table, colElement, out var column))
                    {
                        if (declared.Add(column.Name))
                        {
                            columns.Add(column);
                        }
                    }
                }
            }

            if (entityElement != null)
            {
                // any additional columns for this table referenced in a declared property
                AddInferredElementColumns(table, entityElement, columns, memberToElementMap, declared);
            }

            // any additional columns inferred from entity type itself
            if (table == table.Entity.PrimaryTable)
            {
                AddInferredMemberColumns(table, table.Entity.Type, columns, memberToElementMap, declared);
            }

            return columns.ToReadOnly();
        }

        private void AddInferredElementColumns(
            MappedTable table,
            XElement element,
            List<MappedColumn> columns,
            Dictionary<MemberInfo, XElement>? memberToElementMap,
            HashSet<string> declared)
        {
            foreach (var member in GetMembers(element))
            {
                // table name may be implied
                var tableName = GetTableName(member)
                    ?? (table == table.Entity.PrimaryTable ? table.Name : null);

                if (IsColumnMember(member)
                    && tableName == table.Name)
                {
                    var columnName = GetColumnName(member)
                        ?? GetName(member);

                    if (columnName != null
                        && declared.Add(columnName))
                    {
                        columns.Add(CreateInferredColumn(table, columnName));
                    }
                }
                else if (IsCompoundMember(member))
                {
                    AddInferredElementColumns(table, member, columns, memberToElementMap, declared);
                }
                else if (IsAssociationMember(member))
                {
#if false
                    if (tableName == table.Name
                        && GetKeyColumns(member) is { } keyColumns)
                    {
                        foreach (var keyColumnName in this.GetNames(keyColumns))
                        {
                            if (declared.Add(keyColumnName))
                            {
                                columns.Add(CreateInferredColumn(table, keyColumnName));
                            }
                        }
                    }
                    else if (GetRelatedTableName(member) == table.Name
                        && GetRelatedKeyColumns(member) is { } relatedKeyColumns)
                    {
                        foreach (var relatedKeyColumnName in this.GetNames(relatedKeyColumns))
                        {
                            if (declared.Add(relatedKeyColumnName))
                            {
                                columns.Add(CreateInferredColumn(table, relatedKeyColumnName));
                            }
                        }
                    }
#endif
                }
            }
        }

        private void AddInferredMemberColumns(
            MappedTable table,
            Type type, 
            List<MappedColumn> columns, 
            Dictionary<MemberInfo, XElement>? memberToElementMap,
            HashSet<string> declared)
        {
            var members = type.GetDeclaredFieldsAndProperties();

            foreach (var member in members)
            {
                XElement? memberElement = null;
                memberToElementMap?.TryGetValue(member, out memberElement);

                if (memberElement != null)
                {
                    if (IsCompoundMember(memberElement))
                    {
                        // continue on to nested members, since they may not have declarations
                        var mcType = TypeHelper.GetMemberType(member);
                        AddInferredMemberColumns(table, mcType, columns, memberToElementMap, declared);
                    }
                    else
                    {
                        // do nothing, column is already inferred from element
                    }
                }
                else if (IsPossibleColumnMember(member))
                {
                    if (declared.Add(member.Name))
                    {
                        columns.Add(CreateInferredColumn(table, member.Name));
                    }
                }
                else if (IsPossibleCompoundMember(member))
                {
                    var mcType = TypeHelper.GetMemberType(member);
                    AddInferredMemberColumns(table, mcType, columns, memberToElementMap, declared);
                }
            }
        }

        private bool TryCreateColumn(
            MappedTable table, 
            XElement columnElement, 
            [NotNullWhen(true)] out MappedColumn column)
        {
            var columnName = GetName(columnElement);
            if (columnName != null)
            {
                var columnType = GetColumnType(columnElement);
                var isPrimaryKey = GetIsPrimaryKey(columnElement);
                var isReadOnly = GetIsReadOnly(columnElement);
                var isComputed = GetIsComputed(columnElement);
                var isGenerated = GetIsGenerated(columnElement);

                column = new StandardColumn(
                    table,
                    columnName,
                    columnType,
                    isPrimaryKey: isPrimaryKey,
                    isReadOnly: isReadOnly,
                    isComputed: isComputed,
                    isGenerated: isGenerated,
                    me => table.Entity.Members.OfType<ColumnMember>().FirstOrDefault(cm => cm.Column == me)
                    );

                return true;
            }

            column = default!;
            return false;
        }

        /// <summary>
        /// Converts the XML mapping text to an <see cref="EntityMapping"/>.
        /// </summary>
        public static EntityMapping FromXml(string xml)
        {
            return new XmlMapping(xml);
        }

        /// <summary>
        /// Converts <see cref="EntityMapping"/> to serialized XML text.
        /// </summary>
        public static string ToXml(
            EntityMapping mapping, 
            bool minimal=true)
        {
            return ToXml(mapping.GetEntities(), minimal);
        }

        /// <summary>
        /// Converts the set of <see cref="MappedEntity"/> and all entities reachable by those entities
        /// to serialized XML text.
        /// </summary>
        public static string ToXml(
            IEnumerable<MappedEntity> entities, 
            bool minimal=true)
        {
            var mappingElement = new XElement("Mapping");

            var allEntities = entities.SelectManyRecursive(
                e => e.Members.OfType<AssociationMember>().Select(a => a.RelatedEntity)
                );

            foreach (var entity in allEntities.OrderBy(e => e.Id))
            {
                var entityElement = ToEntityElement(entity, minimal);
                mappingElement.Add(entityElement);
            }

            return mappingElement.ToString();
        }

        private static XElement ToEntityElement(MappedEntity entity, bool minimal)
        {
            var element = new XElement(EntityName);

            element.Add(new XAttribute(IdName, entity.Id));
            element.Add(new XAttribute(TypeName, entity.Type.FullName));

            if (!minimal || entity.ConstructedType != entity.Type)
                element.Add(new XAttribute(ConstructedTypeName, entity.ConstructedType.FullName));

            if (entity.Context != null)
                element.Add(new XAttribute(ContextName, entity.Context));

            foreach (var member in entity.Members.OrderBy(m => m.Member.Name))
            {
                if (member is ColumnMember cm 
                    && cm.Member.Name == cm.Column.Name
                    && minimal)
                    continue;

                element.Add(ToMemberElement(member, minimal));
            }

            element.Add(ToTableElement(entity.PrimaryTable, minimal));

            foreach (var table in entity.ExtensionTables.OrderBy(t => t.Name))
            {
                element.Add(ToTableElement(table, minimal));
            }

            return element;
        }

        private static XElement ToTableElement(MappedTable table, bool minimal)
        {
            if (table is ExtensionTable extTable)
            {
                var element = new XElement(ExtendedTableName);

                element.Add(new XAttribute(NameName, table.Name));
                element.Add(new XAttribute(KeyColumnsName, GetColumnNames(extTable.KeyColumns)));
                element.Add(new XAttribute(RelatedTableName, extTable.RelatedTable.Name));
                element.Add(new XAttribute(RelatedKeyColumnsName, GetColumnNames(extTable.RelatedKeyColumns)));

                foreach (var column in table.Columns.OrderBy(c => c.Name))
                {
                    element.Add(ToColumnElement(column, minimal));
                }

                return element;
            }
            else
            {
                var element = new XElement(TableName);

                element.Add(new XAttribute(NameName, table.Name));

                foreach (var column in table.Columns.OrderBy(c => c.Name))
                {
                    element.Add(ToColumnElement(column, minimal));
                }

                return element;
            }
        }

        private static bool CanBeInferred(MappedColumn column)
        {
            return column.Member != null
                && column.Member.Member.Name == column.Name
                && column.Type == null
                && column.IsPrimaryKey == false
                && column.IsReadOnly == false
                && column.IsComputed == false
                && column.IsGenerated == false;
        }

        private static XElement ToColumnElement(MappedColumn column, bool minimal)
        {
            var element = new XElement(ColumnName);
            element.Add(new XAttribute(NameName, column.Name));
            if (!minimal || !string.IsNullOrEmpty(column.Type))
                element.Add(new XAttribute(TypeName, column.Type ?? ""));
            if (!minimal || column.IsPrimaryKey)
                element.Add(new XAttribute(IsPrimaryKeyName, column.IsPrimaryKey));
            if (!minimal || column.IsReadOnly)
                element.Add(new XAttribute(IsReadOnlyName, column.IsReadOnly));
            if (!minimal || column.IsComputed)
                element.Add(new XAttribute(IsComputedName, column.IsComputed));
            if (!minimal || column.IsGenerated)
                element.Add(new XAttribute(IsGeneratedName, column.IsGenerated));
            return element;
        }

        private static XElement ToMemberElement(MappedMember member, bool minimal)
        {
            switch (member)
            {
                case ColumnMember cm:
                    {
                        var element = new XElement(ColumnMemberName);
                        element.Add(new XAttribute(NameName, member.Member.Name));

                        if (!minimal || cm.Column.Name != cm.Member.Name)
                            element.Add(new XAttribute(ColumnName, cm.Column.Name));
                        if (!minimal || cm.Column.Table != cm.Column.Table.Entity.PrimaryTable)
                            element.Add(new XAttribute(TableName, cm.Column.Table.Name));
                        return element;
                    }

                case CompoundMember cm:
                    {
                        var element = new XElement(CompoundMemberName);
                        element.Add(new XAttribute(NameName, member.Member.Name));

                        if (!minimal || cm.ConstructedType != cm.Type)
                            element.Add(new XAttribute(ConstructedTypeName, cm.ConstructedType.FullName));

                        foreach (var cmm in cm.Members.OrderBy(m => m.Member.Name))
                        {
                            if (cmm is ColumnMember colMember
                                && colMember.Member.Name == colMember.Column.Name
                                && minimal)
                                continue;

                            element.Add(ToMemberElement(cmm, minimal));
                        }

                        return element;
                    }

                case AssociationMember am:
                    {
                        var element = new XElement(AssociationMemberName);
                        element.Add(new XAttribute(NameName, member.Member.Name));

                        var keyNames = GetColumnNames(am.KeyColumns);
                        element.Add(new XAttribute(KeyColumnsName, keyNames));
                        element.Add(new XAttribute(RelatedEntityIdName, am.RelatedEntity.Id));

                        var relatedNames = GetColumnNames(am.RelatedKeyColumns);
                        if (!minimal || relatedNames != keyNames)
                            element.Add(new XAttribute(RelatedKeyColumnsName, relatedNames));
                        
                        if (!minimal || am.IsSource)
                            element.Add(new XAttribute(IsForeignKeyName, am.IsSource));

                        return element;
                    }

                default:
                    throw new InvalidCastException($"{nameof(XmlMapping)}: Unhandled member type '{member.GetType().Name}' in {nameof(ToMemberElement)}");
            }
        }

        private static string GetColumnNames(IEnumerable<MappedColumn> columns)
        {
            return string.Join(", ", columns.Select(c => c.Name));
        }

        private bool IsMember(XElement element) =>
            IsColumnMember(element)
            || IsCompoundMember(element)
            || IsAssociationMember(element);

        private bool IsColumnMember(XElement element) =>
            element.Name == ColumnMemberName;

        private bool IsAssociationMember(XElement element) =>
            element.Name == AssociationMemberName;

        private bool IsCompoundMember(XElement element) =>
            element.Name == CompoundMemberName;

        private string? GetId(XElement? element) =>
            element?.Attribute(IdName)?.Value;

        private string? GetContext(XElement? element) =>
            element?.Attribute(ContextName)?.Value;

        private string? GetEntityType(XElement? element) =>
            element?.Attribute(TypeName)?.Value;

        private string? GetConstructedType(XElement? element) =>
            element?.Attribute(ConstructedTypeName)?.Value;

        private string? GetName(XElement? element) =>
            element?.Attribute(NameName)?.Value;

        private string? GetColumnName(XElement? element) =>
            element?.Attribute(ColumnName)?.Value;

        private string? GetTableName(XElement? element) =>
            element?.Attribute(TableName)?.Value;

        private string? GetColumnType(XElement? element) =>
            element?.Attribute(TypeName)?.Value;

        private bool GetIsPrimaryKey(XElement? element) =>
            ((bool?)element?.Attribute(IsPrimaryKeyName)) ?? false;

        private bool GetIsComputed(XElement? element) =>
            ((bool?)element?.Attribute(IsComputedName)) ?? false;

        private bool GetIsGenerated(XElement? element) =>
            ((bool?)element?.Attribute(IsGeneratedName)) ?? false;

        private bool GetIsReadOnly(XElement? element) =>
            ((bool?)element?.Attribute(IsReadOnlyName)) ?? false;

        private string? GetKeyColumns(XElement? element) =>
            element?.Attribute(KeyColumnsName)?.Value;

        private string? GetRelatedTableName(XElement? element) =>
            element?.Attribute(RelatedTableName)?.Value;

        private string? GetRelatedKeyColumns(XElement? element) =>
            element?.Attribute(RelatedKeyColumnsName)?.Value;

        private string? GetRelatedEntityId(XElement? element) =>
            element?.Attribute(RelatedEntityIdName)?.Value;

        private bool GetIsForeignKey(XElement? element) =>
            ((bool?)element?.Attribute(IsForeignKeyName)) ?? false;

        private IEnumerable<XElement> GetMembers(XElement? element) =>
            element?.Elements().Where(IsMember) ?? ReadOnlyList<XElement>.Empty;

        private IEnumerable<XElement> GetTables(XElement? element) =>
            element?.Elements(TableName) ?? ReadOnlyList<XElement>.Empty;

        private IEnumerable<XElement> GetColumns(XElement element) =>
            element?.Elements(ColumnName) ?? ReadOnlyList<XElement>.Empty;

        private static readonly XName EntityName = XName.Get("Entity");
        private static readonly XName TypeName = XName.Get("Type");
        private static readonly XName ConstructedTypeName = XName.Get("ConstructedType");
        private static readonly XName IdName = XName.Get("Id");
        private static readonly XName ContextName = XName.Get("Context");

        private static readonly XName TableName = XName.Get("Table");
        private static readonly XName NameName = XName.Get("Name");
        private static readonly XName ExtendedTableName = XName.Get("ExtendedTable");
        private static readonly XName KeyColumnsName = XName.Get("KeyColumns");
        private static readonly XName RelatedTableName = XName.Get("RelatedTable");
        private static readonly XName RelatedKeyColumnsName = XName.Get("RelatedKeyColumns");

        private static readonly XName MemberName = XName.Get("Member");
        private static readonly XName ColumnMemberName = XName.Get("ColumnMember");
        private static readonly XName CompoundMemberName = XName.Get("CompoundMember");
        private static readonly XName AssociationMemberName = XName.Get("AssociationMember");

        private static readonly XName ColumnName = XName.Get("Column");
        private static readonly XName IsComputedName = XName.Get("IsComputed");
        private static readonly XName IsPrimaryKeyName = XName.Get("IsPrimaryKey");
        private static readonly XName IsGeneratedName = XName.Get("IsGenerated");
        private static readonly XName IsReadOnlyName = XName.Get("IsReadOnly");

        private static readonly XName RelatedEntityIdName = XName.Get("RelatedEntityId");
        private static readonly XName IsForeignKeyName = XName.Get("IsForeignKey");
    }
}