using static Test.TestHelpers;

namespace Test.Toolkit
{
    using IQToolkit;
    using IQToolkit.Entities;
    using IQToolkit.Entities.Mapping;

    [TestClass]
    public class AttributeMappingTests
    {
        [TestMethod]
        public void TestNorthwindMapping()
        {
            // this is sort of sanity test
            TestMapping(new AttributeMapping(typeof(NorthwindWithAttributes)));
        }

        [TestMethod]
        public void TestEntity_Inferred()
        {
            // inferred from type
            TestEntity(
                typeof(Customer),
                entityId: "Customers",
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );

            // inferred from type
            TestEntity(
                typeof(Customer),
                entityId: "Customers",
                contextType: typeof(CustomersNoMappingContext),
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestEntity_Declared()
        {
            // id explicit in mapping
            TestEntity(
                typeof(Customer),
                contextType: typeof(CustomersWithEntityIdContext),
                entityId: "ECustomer",
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customers",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );

            // id implied by matching type of context member
            TestEntity(
                typeof(Customer),
                contextType: typeof(CustomersWithEntityIdContext),
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customers",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestEntity_UnknownId()
        {
            TestEntity(
                typeof(Customer),
                entityId: "UnknownId",
                contextType: typeof(CustomersNoMappingContext),
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );

            TestEntity(
                typeof(Customer),
                entityId: "UnknownId",
                contextType: typeof(CustomersWithEntityIdContext),
                memberNames: "CustomerID, ContactName, CompanyName, Phone, City, Country",
                tableNames: "Customer",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestTable_Declared_Context()
        {
            TestTable(
                typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithTableNameContext),
                tableName: "TCustomers",
                columnNames: "CustomerID, ContactName, CompanyName, Phone, City, Country"
                );
        }

        [TestMethod]
        public void TestColumn_Declared_Name()
        {
            TestColumn(
                typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithColumnNameContext),
                columnName: "ID"
                );
        }

        [TestMethod]
        public void TestColumn_Declared_IsPrimaryKey()
        {
            TestColumn(
                typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithIsPrimaryKeyContext),
                columnName: "CustomerID",
                isPrimaryKey: true
                );
        }

        [TestMethod]
        public void TestColumn_Declared_IsReadOnly()
        {
            TestColumn(
                typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithIsReadOnlyContext),
                columnName: "CustomerID",
                isReadOnly: true
                );
        }

        [TestMethod]
        public void TestColumn_Declared_IsComputed()
        {
            TestColumn(
                typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithIsComputedContext),
                columnName: "CustomerID",
                isComputed: true
                );
        }

        [TestMethod]
        public void TestColumn_Declared_IsGenerated()
        {
            TestColumn(
                typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithIsGeneratedContext),
                columnName: "CustomerID",
                isGenerated: true
                );
        }

        [TestMethod]
        public void TestColumn_Declared_ColumnType()
        {
            TestColumn(
                typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithTypeContext),
                columnName: "CustomerID",
                columnType: "NVARCHAR"
                );
        }

        [TestMethod]
        public void TestColumnMember_Inferred()
        {
            TestColumnMember(
                entityType: typeof(Customer),
                contextType: typeof(Northwind),
                memberName: "CustomerID",
                columnName: "CustomerID"
                );
        }

        [TestMethod]
        public void TestColumnMember_Declared()
        {
            TestColumnMember(
                entityType: typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithColumnContext),
                memberName: "CustomerID",
                columnName: "CustomerID"
                );
        }

        [TestMethod]
        public void TestColumnMember_Declared_AlternateColumnName()
        {
            TestColumnMember(
                entityType: typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersWithColumnNameContext),
                memberName: "CustomerID",
                columnName: "ID"
                );
        }

        [TestMethod]
        public void TestCompoundMember_Inferred()
        {
            TestCompoundMember(
                entityType: typeof(Employee),
                entityId: "EEmployee",
                contextType: typeof(EmployeesContext),
                memberName: "Address",
                columnNames: "Street, City, Region, PostalCode"
                );

            TestCompoundMember(
                entityType: typeof(Employee),
                contextType: typeof(EmployeesContext),
                memberName: "Address",
                columnNames: "Street, City, Region, PostalCode"
                );
        }

        [TestMethod]
        public void TestCompoundMember_Declared()
        {
            TestCompoundMember(
                entityType: typeof(Employee),
                entityId: "EEmployee",
                contextType: typeof(EmployeesCompoundMemberContext),
                memberName: "Address",
                columnNames: "Street, City, Region, PostalCode"
                );
        }

        [TestMethod]
        public void TestCompoundMember_Declared_AlternateColumnName()
        {
            TestCompoundMember(
                entityType: typeof(Employee),
                entityId: "EEmployee",
                contextType: typeof(EmployeesCompoundMemberColumnNameContext),
                memberName: "Address",
                columnNames: "Address, City, Region, PostalCode"
                );
        }

        [TestMethod]
        public void TestAssociationMember_Declared()
        {
            TestAssociationMember(
                entityType: typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersOrdersContext),
                memberName: "Orders",
                relatedEntityType: typeof(Order),
                relatedEntityId: "EOrder",
                keyColumnNames: "CustomerID",
                relatedKeyColumnNames: "CustomerID"
                );

            TestAssociationMember(
                entityType: typeof(Order),
                entityId: "EOrder",
                contextType: typeof(CustomersOrdersContext),
                memberName: "Customer",
                relatedEntityType: typeof(Customer),
                relatedEntityId: "ECustomer",
                keyColumnNames: "CustomerID",
                relatedKeyColumnNames: "CustomerID"
                );

        }

        [TestMethod]
        public void TestAssociationMember_Declared_Inferred_RelatedKeyColumns()
        {
            TestAssociationMember(
                entityType: typeof(Customer),
                entityId: "ECustomer",
                contextType: typeof(CustomersOrdersInferredRelatedKeysContext),
                memberName: "Orders",
                relatedEntityType: typeof(Order),
                relatedEntityId: "EOrder",
                keyColumnNames: "CustomerID",
                relatedKeyColumnNames: "CustomerID"
                );

            TestAssociationMember(
                entityType: typeof(Order),
                entityId: "EOrder",
                contextType: typeof(CustomersOrdersInferredRelatedKeysContext),
                memberName: "Customer",
                relatedEntityType: typeof(Customer),
                relatedEntityId: "ECustomer",
                keyColumnNames: "CustomerID",
                relatedKeyColumnNames: "CustomerID"
                );
        }

        public abstract class CustomersNoMappingContext
        {
            [Entity(Id = "ECustomer")]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithEntityIdContext
        {
            [Entity(Id = "ECustomer")]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithTableNameContext
        {
            [Entity(Id = "ECustomer")]
            [Table(Name = "TCustomers")]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithColumnContext
        {
            [Entity(Id = "ECustomer")]
            [Column(Member = "CustomerID")]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithColumnNameContext
        {
            [Entity(Id = "ECustomer")]
            [Column(Member="CustomerID", Name = "ID")]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithIsPrimaryKeyContext
        {
            [Entity(Id = "ECustomer")]
            [Column(Member = "CustomerID", IsPrimaryKey = true)]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithIsReadOnlyContext
        {
            [Entity(Id = "ECustomer")]
            [Column(Member = "CustomerID", IsReadOnly = true)]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithIsComputedContext
        {
            [Entity(Id = "ECustomer")]
            [Column(Member = "CustomerID", IsComputed = true)]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithIsGeneratedContext
        {
            [Entity(Id = "ECustomer")]
            [Column(Member = "CustomerID", IsGenerated = true)]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class CustomersWithTypeContext
        {
            [Entity(Id = "ECustomer")]
            [Column(Member = "CustomerID", DbType = "NVARCHAR")]
            public abstract IEntityTable<Customer> Customers { get; }
        }

        public abstract class EmployeesContext 
        {
            [Entity(Id = "EEmployee")]
            public abstract IEntityTable<Employee> Employees { get; }
        }

        public abstract class EmployeesCompoundMemberContext
        {
            [Entity(Id = "EEmployee")]
            [Compound(Member="Address")]
            public abstract IEntityTable<Employee> Employees { get; }
        }

        public abstract class EmployeesCompoundMemberColumnNameContext
        {
            [Entity(Id = "EEmployee")]
            [Compound(Member="Address")]
            [Column(Member = "Address.Street", Name = "Address")]
            public abstract IEntityTable<Employee> Employees { get; }
        }

        public abstract class CustomersOrdersContext
        {
            [Entity(Id = "ECustomer")]
            [Association(Member = "Orders", KeyColumns = "CustomerID", RelatedEntityId = "EOrder", RelatedKeyColumns="CustomerID")]
            public abstract IEntityTable<Customer> Customers { get; }

            [Entity(Id = "EOrder")]
            [Association(Member = "Customer", KeyColumns = "CustomerID", RelatedEntityId = "ECustomer", RelatedKeyColumns="CustomerID", IsForeignKey = true)]
            public abstract IEntityTable<Order> Orders { get; }
        }

        public abstract class CustomersOrdersInferredRelatedKeysContext
        {
            [Entity(Id = "ECustomer")]
            [Association(Member = "Orders", KeyColumns = "CustomerID", RelatedEntityId = "EOrder")]
            public abstract IEntityTable<Customer> Customers { get; }

            [Entity(Id = "EOrder")]
            [Association(Member = "Customer", KeyColumns = "CustomerID", RelatedEntityId = "ECustomer", IsForeignKey = true)]
            public abstract IEntityTable<Order> Orders { get; }
        }

        private void TestMapping(EntityMapping mapping)
        {
            // walk through mapping info to prove that mapping is fully constructed
            // without throwing exceptions

            foreach (var entity in mapping.GetEntities())
            {
                WalkEntity(entity);
            }

            void WalkEntity(MappedEntity entity)
            {
                foreach (var table in entity.Tables)
                {
                    WalkTable(table);
                }

                foreach (var member in entity.Members)
                {
                    WalkMember(member);
                }
            }

            void WalkTable(MappedTable table)
            {
                var columns = table.Columns;
                if (table is ExtensionTable extensionTable)
                {
                    var related = extensionTable.RelatedTable;
                    var keyColumnNames = extensionTable.KeyColumns;
                    var relatedMembers = extensionTable.RelatedKeyColumns;
                }
            }

            void WalkMember(MappedMember member)
            {
                switch (member)
                {
                    case ColumnMember columnMember:
                        var colTable = columnMember.Column.Table;
                        break;
                    case CompoundMember nested:
                        foreach (var mm in nested.Members)
                        {
                            WalkMember(mm);
                        }
                        break;
                    case AssociationMember assoc:
                        var assocRelated = assoc.RelatedEntity;
                        var keys = assoc.KeyColumns;
                        var relatedKeys = assoc.RelatedKeyColumns;
                        break;
                }
            }
        }

        public void TestMapping(
            Type? contextType,
            Action<EntityMapping> fnCheck)
        {
            var mapping = new AttributeMapping(contextType);
            fnCheck(mapping);
        }

        /// <summary>
        /// Test one entity
        /// </summary>
        public void TestEntity(
            Type entityType,
            string memberNames,
            string tableNames,
            string columnNames,
            Type? contextType = null,
            string? entityId = null,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

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
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

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
            Type entityType,
            string tableName,
            string columnNames,
            Type? contextType = null,
            string? entityId = null,
            bool? isPrimaryTable = null,
            string? keyColumnNames = null,
            string? relatedTableName = null,
            string? relatedKeyColumnNames = null,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

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
            Type entityType,
            string memberName,
            string? columnName = null,
            string? tableName = null,
            Type? contextType = null,
            string? entityId = null,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

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
            Type entityType,
            string memberName,
            string columnNames,
            string? tableName = null,
            Type? contextType = null,
            string? entityId = null,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

                    Assert.IsTrue(entity.TryGetMember(memberName, out var member), "member");
                    var compoundMember = member as CompoundMember;
                    Assert.IsNotNull(compoundMember, "compound member");

                    var names = SplitNames(columnNames);
                    var columnMembers = compoundMember.Members.OfType<ColumnMember>().ToArray();
                    Assert.AreEqual(names.Length, columnMembers.Length, "member count");

                    for (int i = 0; i < names.Length; i++)
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
            Type entityType,
            string memberName,
            Type relatedEntityType,
            string keyColumnNames,
            string relatedKeyColumnNames,
            string? relatedEntityId = null,
            Type? contextType = null,
            string? entityId = null,
            Action<EntityMapping>? fnCheck = null
            )
        {
            TestMapping(
                contextType,
                mapping =>
                {
                    Assert.IsTrue(mapping.TryGetEntity(entityType, entityId, out var entity), "entity");

                    Assert.IsTrue(entity.TryGetMember(memberName, out var member), "member");
                    var associationMember = member as AssociationMember;
                    Assert.IsNotNull(associationMember, "association member");

                    Assert.AreEqual(relatedEntityType, associationMember.RelatedEntity.Type, "related entity type");

                    if (relatedEntityId != null)
                        Assert.AreEqual(relatedEntityId, associationMember.RelatedEntity.Id, "related entity id");

                    Assert.AreEqual(relatedEntityId, associationMember.RelatedEntity.Id, "related entity");

                    var kcNames = SplitNames(keyColumnNames);
                    Assert.AreEqual(kcNames.Length, associationMember.KeyColumns.Count, "key columns count");

                    for (int i = 0; i < kcNames.Length; i++)
                    {
                        Assert.AreEqual(kcNames[i], associationMember.KeyColumns[i].Name, "key column");
                    }

                    var rkcNames = SplitNames(relatedKeyColumnNames);
                    Assert.AreEqual(rkcNames.Length, associationMember.RelatedKeyColumns.Count, "related key columns length");

                    for (int i = 0; i < rkcNames.Length; i++)
                    {
                        Assert.AreEqual(rkcNames[i], associationMember.RelatedKeyColumns[i].Name, "key column");
                    }

                    fnCheck?.Invoke(mapping);
                });
        }
    }
}
