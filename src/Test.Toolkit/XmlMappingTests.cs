using static Test.TestHelpers;

namespace Test.Toolkit
{
    using IQToolkit;
    using IQToolkit.Entities;
    using IQToolkit.Entities.Mapping;

    [TestClass]
    public class XmlMappingTests
    {
        [TestMethod]
        public void TestMapping_RoundTrip()
        {
            // construct XML from attribute mapping and round trip it,
            // to XML text and back to XmlMapping
            var attrMapped = new AttributeMapping(typeof(NorthwindWithAttributes));
            var xml = XmlMapping.ToXml(attrMapped);
            var xmlMapped = XmlMapping.FromXml(xml);
            var xml2 = XmlMapping.ToXml(xmlMapped);
            AssertLargeTextEqual(xml, xml2);
            var full = XmlMapping.ToXml(xmlMapped, minimal: false);
        }

        [TestMethod]
        public void TestEntity_AllMappingInferredFromType()
        {
            // everything inferred from type
            TestEntity(
                """
                <Mapping>
                </Mapping>
                """,
                typeof(Customer),
                entityId: "ECustomer",
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestEntity_MappingFoundByEntityType()
        {
            // entityId unspecified, deduced from finding mapping associated with type
            TestEntity(
                """
                <Mapping>
                    <Entity Id='ECustomer' Type='Test.Customer'>
                    </Entity>
                </Mapping>
                """,
                typeof(Customer),
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestEntity_MappingFoundByEntityId()
        {
            // id explicit in mapping
            TestEntity(
                """
                <Mapping>
                    <Entity Id='ECustomer' Type='Test.Customer'>
                        <Table Name='Customers' />
                    </Entity>
                </Mapping>
                """,
                typeof(Customer),
                entityId: "ECustomer",
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customers",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestEntity_UnknownId()
        {
            TestEntity(
                """
                <Mapping>
                    <Entity Id='ECustomer' Type='Test.Customer'>
                    </Entity>
                </Mapping>
                """,
                typeof(Customer),
                entityId: "UnknownId",
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestTable_NameInferredFromEntityType()
        {
            // with mapping
            TestTable(
                """
                <Mapping>
                    <Entity Id='C'>
                    </Entity>
                </Mapping>
                """,
                typeof(Customer),
                entityId: "C",
                tableName: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );

            // without mapping
            TestTable(
                """
                <Mapping>
                </Mapping>
                """,
                typeof(Customer),
                tableName: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestTable_Declared()
        {
            TestTable(
                """
                <Mapping>
                    <Entity Id='C'>
                        <Table Name='Customers' />
                    </Entity>
                </Mapping>
                """,
                typeof(Customer),
                entityId: "C",
                tableName: "Customers",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestColumn_IsPrimaryKey()
        {
            TestColumn(
                """
                <Mapping>
                    <Entity Id='C'>
                        <Table Name='Customers'>
                            <Column Name='CustomerID' IsPrimaryKey='true' />
                        </Table>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                columnName: "CustomerID",
                isPrimaryKey: true
                );
        }

        [TestMethod]
        public void TestColumn_IsReadOnly()
        {
            TestColumn(
                """
                <Mapping>
                    <Entity Id='C'>
                        <Table Name='Customers'>
                            <Column Name='CustomerID' IsReadOnly='true' />
                        </Table>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                columnName: "CustomerID",
                isReadOnly: true
                );
        }

        [TestMethod]
        public void TestColumn_IsComputed()
        {
            TestColumn(
                """
                <Mapping>
                    <Entity Id='Customer'>
                        <Table Name='Customers'>
                            <Column Name='CustomerID' IsComputed='true' />
                        </Table>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                columnName: "CustomerID",
                isComputed: true
                );
        }

        [TestMethod]
        public void TestColumn_IsGenerated()
        {
            TestColumn(
                """
                <Mapping>
                    <Entity Id='C'>
                        <Table Name='Customers'>
                            <Column Name='CustomerID' IsGenerated='true' />
                        </Table>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                columnName: "CustomerID",
                isGenerated: true
                );
        }

        [TestMethod]
        public void TestColumn_Type()
        {
            TestColumn(
                """
                <Mapping>
                    <Entity Id='C'>
                        <Table Name='Customers'>
                            <Column Name='CustomerID' Type='NVARCHAR' />
                        </Table>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                columnName: "CustomerID",
                columnType: "NVARCHAR"
                );
        }

        [TestMethod]
        public void TestColumnMember_Inferred()
        {
            TestColumnMember(
                """
                <Mapping>
                    <Entity Id='C'>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                memberName: "CustomerID",
                columnName: "CustomerID"
                );

            TestColumnMember(
                """
                <Mapping>
                </Mapping>
                """,
                entityType: typeof(Customer),
                memberName: "CustomerID",
                columnName: "CustomerID"
                );
        }

        [TestMethod]
        public void TestColumnMember_Declared()
        {
            TestColumnMember(
                """
                <Mapping>
                    <Entity Id='C'>
                        <ColumnMember Name='CustomerID' />
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                memberName: "CustomerID",
                columnName: "CustomerID"
                );
        }

        [TestMethod]
        public void TestColumnMember_Declared_AlternateColumnName()
        {
            TestColumnMember(
                """
                <Mapping>
                    <Entity Id='C'>
                        <ColumnMember Name='CustomerID' Column='ID' />
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                memberName: "CustomerID",
                columnName: "ID"
                );
        }

        [TestMethod]
        public void TestCompoundMember_Inferred()
        {
            TestCompoundMember(
                """
                <Mapping>
                    <Entity Id='E'>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Employee),
                entityId: "E",
                memberName: "Address",
                columnNames: "Street, City, Region, PostalCode"
                );

            TestCompoundMember(
                """
                <Mapping>
                </Mapping>
                """,
                entityType: typeof(Employee),
                memberName: "Address",
                columnNames: "Street, City, Region, PostalCode"
                );
        }

        [TestMethod]
        public void TestCompoundMember_Declared()
        {
            TestCompoundMember(
                """
                <Mapping>
                    <Entity Id='E'>
                        <CompoundMember Name="Address">
                            <ColumnMember Name="Street" />
                            <ColumnMember Name="City" />
                            <ColumnMember Name="Region" />
                            <ColumnMember Name="PostalCode" />
                        </CompoundMember>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Employee),
                entityId: "E",
                memberName: "Address",
                columnNames: "Street, City, Region, PostalCode"
                );
        }

        [TestMethod]
        public void TestCompoundMember_Declared_AlternateColumnName()
        {
            TestCompoundMember(
                """
                <Mapping>
                    <Entity Id='E'>
                        <CompoundMember Name="Address">
                            <ColumnMember Name="Street" Column="Address" />
                            <ColumnMember Name="City" />
                            <ColumnMember Name="Region" />
                            <ColumnMember Name="PostalCode" />
                        </CompoundMember>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Employee),
                entityId: "E",
                memberName: "Address",
                columnNames: "Address, City, Region, PostalCode"
                );
        }

        [TestMethod]
        public void TestCompoundMember_Declared_Partial()
        {
            TestCompoundMember(
                """
                <Mapping>
                    <Entity Id='E'>
                        <CompoundMember Name="Address">
                            <ColumnMember Name="Street" Column="Address" />
                        </CompoundMember>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Employee),
                entityId: "E",
                memberName: "Address",
                columnNames: "Address, City, Region, PostalCode"
                );
        }

        [TestMethod]
        public void TestAssociationMember_Declared()
        {
            TestAssociationMember(
                """
                <Mapping>
                    <Entity Id='C'>
                        <AssociationMember Name='Orders' KeyColumns='CustomerID' RelatedEntityId='O' RelatedKeyColumns='CustomerID'/>
                    </Entity>
                    <Entity Id='O'>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                memberName: "Orders",
                relatedEntityType: typeof(Order),
                relatedEntityId: "O",
                keyColumnNames: "CustomerID",
                relatedKeyColumnNames: "CustomerID"
                );
        }

        [TestMethod]
        public void TestAssociationMember_Declared_Inferred_RelatedKeyColumns()
        {
            TestAssociationMember(
                """
                <Mapping>
                    <Entity Id='C'>
                        <AssociationMember Name='Orders' KeyColumns='CustomerID' RelatedEntityId='O' />
                    </Entity>
                    <Entity Id='O'>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                memberName: "Orders",
                relatedEntityType: typeof(Order),
                relatedEntityId: "O",
                keyColumnNames: "CustomerID",
                relatedKeyColumnNames: "CustomerID"
                );
        }

        [TestMethod]
        public void TestAssociationMember_UnknownKeyColumn()
        {
            TestAssociationMember(
                """
                <Mapping>
                    <Entity Id='C'>
                        <AssociationMember Name='Orders' KeyColumns='Huh?' RelatedEntityId='O' RelatedKeyColumns='CustomerID'/>
                    </Entity>
                    <Entity Id='O'>
                    </Entity>
                </Mapping>
                """,
                entityType: typeof(Customer),
                entityId: "C",
                memberName: "Orders",
                relatedEntityType: typeof(Order),
                hasDiagnostics: true
                );
        }

        public void TestMapping(
            string xml,
            Type? contextType,
            Action<EntityMapping> fnCheck)
        {
            var mapping = XmlMapping.FromXml(xml);
            fnCheck(mapping);
        }

        public void AssertDiagnostics(bool expectedDiagnostics, MappedEntity entity)
        {
            if (expectedDiagnostics != entity.Diagnostics.Count > 0)
            {
                Assert.Fail($"Mapping Diagnostics (Entity '{entity.Id}'):\n{string.Join("\n", entity.Diagnostics.Select(d => d.Message))}");
            }
        }

        /// <summary>
        /// Test one entity
        /// </summary>
        public void TestEntity(
            string xml,
            Type entityType,
            string memberNames,
            string tableNames,
            string columnNames,
            bool hasDiagnostics = false,
            Type? contextType = null,
            string? entityId = null,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                xml,
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

                    AssertDiagnostics(hasDiagnostics, entity);

                    var memNames = SplitNames(memberNames);
                    Assert.AreEqual(memNames.Length, entity.Members.Count, "members count");

                    for (int i = 0; i < memNames.Length; i++)
                    {
                        Assert.AreEqual(memNames[i], entity.Members[i].Member.Name, "member name");
                    }

                    var tabNames = SplitNames(tableNames);
                    Assert.AreEqual(tabNames.Length, entity.Tables.Count, "tables count");
                    for (int i = 0; i < tabNames.Length; i++)
                    {
                        Assert.AreEqual(tabNames[i], entity.Tables[i].Name, "table name");
                    }

                    var colNames = SplitNames(columnNames);
                    Assert.AreEqual(colNames.Length, entity.Columns.Count, "columns count");
                    for (int i = 0; i < colNames.Length; i++)
                    {
                        Assert.AreEqual(colNames[i], entity.Columns[i].Name, "column name");
                    }

                    fnCheck?.Invoke(mapping);
                });
        }

        /// <summary>
        /// Test one column mapped for an entity.
        /// </summary>
        public void TestColumn(
            string xml,
            Type entityType,
            string columnName,
            string? tableName = null,
            Type? contextType = null,
            string? entityId = null,
            string? columnType = null,
            bool? isPrimaryKey = false,
            bool? isReadOnly = false,
            bool? isComputed = false,
            bool? isGenerated = false,
            bool hasDiagnostics = false,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                xml, 
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");
                    AssertDiagnostics(hasDiagnostics, entity);

                    Assert.IsTrue(entity.TryGetColumn(columnName, tableName, out var column), "column");
                    if (columnType != null)
                        Assert.AreEqual(columnType, column.Type);
                    Assert.AreEqual(isPrimaryKey, column.IsPrimaryKey, "IsPrimaryKey");
                    Assert.AreEqual(isReadOnly, column.IsReadOnly, "IsReadOnly");
                    Assert.AreEqual(isComputed, column.IsComputed, "IsComputed");
                    Assert.AreEqual(isGenerated, column.IsGenerated, "IsGenerated");

                    fnCheck?.Invoke(mapping);
                });
        }

        /// <summary>
        /// Test one table mapped for an entity.
        /// </summary>
        public void TestTable(
            string xml,
            Type entityType,
            string tableName,
            string columnNames,
            Type? contextType = null,
            string? entityId = null,
            bool? isPrimaryTable = null,
            string? keyColumnNames = null,
            string? relatedTableName = null,
            string? relatedKeyColumnNames = null,
            bool hasDiagnostics = false,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                xml, 
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

                    AssertDiagnostics(hasDiagnostics, entity);

                    Assert.IsTrue(entity.TryGetTable(tableName, out var table), "table");

                    var colNames = SplitNames(columnNames);
                    Assert.AreEqual(colNames.Length, table.Columns.Count, "columns count");

                    for (int i = 0; i < colNames.Length; i++)
                    {
                        Assert.AreEqual(colNames[i], table.Columns[i].Name, "column name");
                    }

                    if (isPrimaryTable != null)
                        Assert.AreEqual(isPrimaryTable.Value, table is PrimaryTable);

                    if (relatedTableName != null
                        || relatedKeyColumnNames != null
                        || keyColumnNames != null)
                    {
                        var extTable = table as ExtensionTable;
                        Assert.IsNotNull(extTable, "extension table");

                        if (keyColumnNames != null)
                        {
                            var kcNames = SplitNames(keyColumnNames);
                            Assert.AreEqual(kcNames.Length, extTable.KeyColumns.Count, "key columns count");
                            for (int i = 0; i < kcNames.Length; i++)
                            {
                                Assert.AreEqual(kcNames[i], extTable.KeyColumns[i].Name, "key column");
                            }
                        }

                        if (relatedTableName != null)
                            Assert.AreEqual(relatedTableName, extTable.RelatedTable.Name, "related table name");

                        if (relatedKeyColumnNames != null)
                        {
                            var kcNames = SplitNames(relatedKeyColumnNames);
                            Assert.AreEqual(kcNames.Length, extTable.RelatedKeyColumns.Count, "related key columns count");
                            for (int i = 0; i < kcNames.Length; i++)
                            {
                                Assert.AreEqual(kcNames[i], extTable.RelatedKeyColumns[i].Name, "related key column");
                            }
                        }
                    }

                    fnCheck?.Invoke(mapping);
                });
        }

        /// <summary>
        /// Test one column member mapped for an entity.
        /// </summary>
        public void TestColumnMember(
            string xml,
            Type entityType,
            string memberName,
            string? columnName = null,
            string? tableName = null,
            Type? contextType = null,
            string? entityId = null,
            bool hasDiagnostics = false,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                xml, contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

                    AssertDiagnostics(hasDiagnostics, entity);

                    Assert.IsTrue(entity.TryGetMember(memberName, out var member), "member");
                    var columnMember = member as ColumnMember;
                    Assert.IsNotNull(columnMember, "column member");

                    columnName = columnName ?? memberName;
                    Assert.AreEqual(columnName, columnMember.Column.Name, "column");

                    fnCheck?.Invoke(mapping);
                });
        }

        /// <summary>
        /// Test one compound member mapped for an entity.
        /// </summary>
        public void TestCompoundMember(
            string xml,
            Type entityType,
            string memberName,
            string columnNames,
            string? tableName = null,
            bool hasDiagnostics = false,
            Type? contextType = null,
            string? entityId = null,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                xml, 
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

                    AssertDiagnostics(hasDiagnostics, entity);

                    Assert.IsTrue(entity.TryGetMember(memberName, out var member), "member");
                    var compoundMember = member as CompoundMember;
                    Assert.IsNotNull(compoundMember, "compound member");

                    var names = SplitNames(columnNames);
                    var columnMembers = compoundMember.Members.OfType<ColumnMember>().ToArray();
                    Assert.AreEqual(names.Length, columnMembers.Length, "member count");

                    for(int i = 0; i < names.Length; i++)
                    {
                        Assert.AreEqual(names[i], columnMembers[i].Column.Name, "compound column");
                    }

                    fnCheck?.Invoke(mapping);
                });
        }

        /// <summary>
        /// Test one association member mapped for an entity.
        /// </summary>
        public void TestAssociationMember(
            string xml,
            Type entityType,
            string memberName,
            Type relatedEntityType,
            string? keyColumnNames = null,
            string? relatedKeyColumnNames = null,
            string? relatedEntityId = null,
            bool hasDiagnostics = false,
            Type? contextType = null,
            string? entityId = null,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                xml, 
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

                    AssertDiagnostics(hasDiagnostics, entity);

                    Assert.IsTrue(entity.TryGetMember(memberName, out var member), "member");
                    var associationMember = member as AssociationMember;
                    Assert.IsNotNull(associationMember, "association member");

                    Assert.AreEqual(relatedEntityType, associationMember.RelatedEntity.Type, "related entity type");

                    if (relatedEntityId != null)
                    {
                        Assert.AreEqual(relatedEntityId, associationMember.RelatedEntity.Id, "related entity id");
                    }

                    if (keyColumnNames != null)
                    {
                        var kcNames = SplitNames(keyColumnNames);
                        Assert.AreEqual(kcNames.Length, associationMember.KeyColumns.Count, "key columns count");

                        for (int i = 0; i < kcNames.Length; i++)
                        {
                            Assert.AreEqual(kcNames[i], associationMember.KeyColumns[i].Name, "key column");
                        }
                    }

                    if (relatedKeyColumnNames != null)
                    {
                        var rkcNames = SplitNames(relatedKeyColumnNames);
                        Assert.AreEqual(rkcNames.Length, associationMember.RelatedKeyColumns.Count, "related key columns length");

                        for (int i = 0; i < rkcNames.Length; i++)
                        {
                            Assert.AreEqual(rkcNames[i], associationMember.RelatedKeyColumns[i].Name, "key column");
                        }
                    }

                    fnCheck?.Invoke(mapping);
                });
        }
    }
}