// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq;

#pragma warning disable CS0649

namespace Test
{
    using IQToolkit.Data.Mapping;
    using System.Collections.Generic;

    public abstract partial class NorthwindMappingTests : QueryTestBase
    {
        #region Basic Mapping
        class Basic_AttributesOnEntity
        {
            [Table(Name ="Customers")]
            public class Customer
            {
                [Column(Name ="CustomerID")]
                public string ID;

                [Column(Name ="ContactName")]
                public string Name;

                public string Phone;
            }
        }

        public void TestBasicMapping_AttributesOnEntity()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Basic_AttributesOnEntity.Customer>().ToList();

            Assert.Equal(91, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }

        class Basic_AttributesOnEntity_RuntimeType
        {
            [Entity(RuntimeType = typeof(RuntimeCustomer))]
            [Table(Name = "Customers")]
            public interface ICustomer
            {
                [Column(Name = "CustomerID")]
                string ID { get; set; }

                [Column(Name = "ContactName")]
                string Name { get; set; }

                string Phone { get; set; }
            }

            public class RuntimeCustomer : ICustomer
            {
                public string ID { get; set; }
                public string Name { get; set; }
                public string Phone { get; set; }
            }
        }

        public void TestBasicMapping_AttributesOnEntity_RuntimeType()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Basic_AttributesOnEntity_RuntimeType.ICustomer>().ToList();

            Assert.Equal(91, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }

        class Basic_AttributesOnContext
        {
            public class Customer
            {
                public string ID;
                public string Name;
                public string Phone;
            }

            public abstract class Context
            {
                [Table(Name = "Customers")]
                [Column(Member="Customers.ID", Name = "CustomerID")] // legal to have context property preceding
                [Column(Member="Name", Name = "ContactName")]
                public abstract IQueryable<Customer> Customers { get; }
            }
        }

        public void TestBasicMapping_AttributesOnContext()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping(typeof(Basic_AttributesOnContext.Context)));
            var items = provider.GetTable<Basic_AttributesOnContext.Customer>().ToList();

            Assert.Equal(91, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }

        
        class Basic_AttributesOnContext_TableNameInferred
        {
            public class Customer
            {
                public string ID;
                public string Name;
                public string Phone;
            }

            public abstract class Context
            {
                [Table]
                [Column(Member = "Customers.ID", Name = "CustomerID")] // legal to have context property preceding
                [Column(Member = "Name", Name = "ContactName")]
                public abstract IQueryable<Customer> Customers { get; }
            }
        }

        public void TestBasicMapping_AttributesOnContext_TableNameInferred()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping(typeof(Basic_AttributesOnContext_TableNameInferred.Context)));
            var items = provider.GetTable<Basic_AttributesOnContext_TableNameInferred.Customer>().ToList();

            Assert.Equal(91, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }

        class Basic_AttributesOnContext_RuntimeType
        {
            public interface ICustomer
            {
                string ID { get; set; }
                string Name { get; set; }
                string Phone { get; set; }
            }

            public class RuntimeCustomer : ICustomer
            {
                public string ID { get; set; }
                public string Name { get; set; }
                public string Phone { get; set; }
            }

            public abstract class Context
            {
                [Entity(RuntimeType = typeof(RuntimeCustomer))]
                [Table(Name = "Customers")]
                [Column(Member = "Customers.ID", Name = "CustomerID")] // legal to have context property preceding
                [Column(Member = "Name", Name = "ContactName")]
                public abstract IQueryable<ICustomer> Customers { get; }
            }
        }

        public void TestBasicMapping_AttributesOnContext_RuntimeType()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping(typeof(Basic_AttributesOnContext_RuntimeType.Context)));
            var items = provider.GetTable<Basic_AttributesOnContext_RuntimeType.ICustomer>().ToList();

            Assert.Equal(91, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }

        class Basic_XmlMapped
        {
            public class Customer
            {
                public string ID;
                public string Name;
                public string Phone;
            }

            public static string Xml = @"
<map>
  <Entity Id = ""Customer"">
    <Table Name=""Customers""/>
    <Column Member=""ID"" Name=""CustomerID""/>
    <Column Member=""Name"" Name=""ContactName""/>
  </Entity>
 </map>
";
        }

        public void TestBasicMapping_XmlMapped()
        {
            var provider = this.GetProvider().WithMapping(XmlMapping.FromXml(Basic_XmlMapped.Xml));
            var items = provider.GetTable<Basic_XmlMapped.Customer>().ToList();

            Assert.Equal(91, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }

        class Basic_XmlMapped_RuntimeType
        {
            public interface ICustomer
            {
                string ID { get; set; }
                string Name { get; set; }
                string Phone { get; set; }
            }

            public class RuntimeCustomer : ICustomer
            {
                public string ID { get; set; }
                public string Name { get; set; }
                public string Phone { get; set; }
            }

            public static string Xml = @"
<map>
  <Entity Id = ""ICustomer"" RuntimeType=""Test.NorthwindMappingTests+Basic_XmlMapped_RuntimeType+RuntimeCustomer"">
    <Table Name=""Customers""/>
    <Column Member=""ID"" Name=""CustomerID""/>
    <Column Member=""Name"" Name=""ContactName""/>
  </Entity>
 </map>
";
        }

        public void TestBasicMapping_XmlMapped_RuntimeType()
        {
            var provider = this.GetProvider().WithMapping(
                XmlMapping.FromXml(Basic_XmlMapped_RuntimeType.Xml, typeof(Basic_XmlMapped_RuntimeType.RuntimeCustomer).Assembly));
            var items = provider.GetTable<Basic_XmlMapped_RuntimeType.ICustomer>().ToList();

            Assert.Equal(91, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }
        #endregion

        #region Associations

        #region Mapping attributes on entity
        class Association_AttributesOnEntity
        {
            [Table(Name = "Customers")]
            public class Customer
            {
                public string CustomerID;
                public string ContactName;
                public string Phone;

                [Association(KeyMembers=nameof(CustomerID))]
                public List<Order> Orders = new List<Order>();
            }

            [Table(Name ="Orders")]
            public class Order
            {
                public int OrderID;
                public string CustomerID;

                [Association(KeyMembers=nameof(CustomerID))]
                public Customer Customer;
            }
        }

        public void TestAssociation_AttributesOnEntity_Orders()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Association_AttributesOnEntity.Customer>()
                .Where(c => c.Orders.Count() > 1)
                .ToList();

            Assert.Equal(88, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.CustomerID);
                Assert.NotEqual(null, item.ContactName);
                Assert.NotEqual(null, item.Phone);
                Assert.NotEqual(null, item.Orders);
                Assert.Equal(0, item.Orders.Count); // not retrieved, no policy
            }
        }

        public void TestAssociation_AttributesOnEntity_Customer()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Association_AttributesOnEntity.Order>()
                .Where(o => o.Customer.ContactName.StartsWith("Maria"))
                .ToList();

            Assert.Equal(25, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.OrderID);
                Assert.NotEqual(null, item.CustomerID);
                Assert.Equal(null, item.Customer); // not retrieved, no policy
            }
        }

        class Association_AttributesOnEntity_DifferentKeys
        {
            [Table(Name = "Customers")]
            public class Customer
            {
                [Column(Name ="CustomerID")]
                public string ID;
                public string ContactName;
                public string Phone;

                [Association(KeyMembers = nameof(ID), RelatedKeyMembers = nameof(Order.CustomerID))]
                public List<Order> Orders = new List<Order>();
            }

            [Table(Name = "Orders")]
            public class Order
            {
                [Column(Name ="OrderID")]
                public int ID;
                public string CustomerID;

                [Association(KeyMembers = nameof(CustomerID), RelatedKeyMembers = nameof(Association_AttributesOnEntity_DifferentKeys.Customer.ID))]
                public Customer Customer;
            }
        }

        public void TestAssociation_AttributesOnEntity_DifferentKeys_Orders()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Association_AttributesOnEntity_DifferentKeys.Customer>()
                .Where(c => c.Orders.Count() > 1)
                .ToList();

            Assert.Equal(88, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.ContactName);
                Assert.NotEqual(null, item.Phone);
                Assert.NotEqual(null, item.Orders);
                Assert.Equal(0, item.Orders.Count);
            }
        }

        public void TestAssociation_AttributesOnEntity_DifferenteKeys_Customer()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Association_AttributesOnEntity_DifferentKeys.Order>()
                .Where(o => o.Customer.ContactName.StartsWith("Maria"))
                .ToList();

            Assert.Equal(25, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.CustomerID);
                Assert.Equal(null, item.Customer); // not retrieved, no policy
            }
        }
        #endregion

        #region Mapping attributes on context
        class Association_AttributesOnContext
        {
            public class Customer
            {
                public string CustomerID;
                public string ContactName;
                public string Phone;

                public List<Order> Orders = new List<Order>();
            }

            public class Order
            {
                public int OrderID;
                public string CustomerID;
                public Customer Customer;
            }

            public abstract class Context
            {
                [Table(Name = "Customers")]
                [Association(Member="Orders", KeyMembers = nameof(Customer.CustomerID))]
                public abstract IQueryable<Customer> Customers { get; }

                [Table(Name = "Orders")]
                [Association(Member="Customer", KeyMembers = nameof(Order.CustomerID))]
                public abstract IQueryable<Order> Orders { get; }
            }
        }

        public void TestAssociation_AttributesOnContext_Orders()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping(typeof(Association_AttributesOnContext.Context)));
            var items = provider.GetTable<Association_AttributesOnContext.Customer>()
                .Where(c => c.Orders.Count() > 1)
                .ToList();

            Assert.Equal(88, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.CustomerID);
                Assert.NotEqual(null, item.ContactName);
                Assert.NotEqual(null, item.Phone);
                Assert.NotEqual(null, item.Orders);
                Assert.Equal(0, item.Orders.Count); // not retrieved, no policy
            }
        }

        public void TestAssociation_AttributesOnContext_Customer()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping(typeof(Association_AttributesOnContext.Context)));
            var items = provider.GetTable<Association_AttributesOnContext.Order>()
                .Where(o => o.Customer.ContactName.StartsWith("Maria"))
                .ToList();

            Assert.Equal(25, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.OrderID);
                Assert.NotEqual(null, item.CustomerID);
                Assert.Equal(null, item.Customer); // not retrieved, no policy
            }
        }
        #endregion

        #region Mapping in XML
        class Association_XmlMapping
        {
            public class Customer
            {
                public string CustomerID;
                public string ContactName;
                public string Phone;
                public List<Order> Orders = new List<Order>();
            }

            public class Order
            {
                public int OrderID;
                public string CustomerID;
                public Customer Customer;
            }

            public static string Xml = @"
<map>
  <Entity Id=""Customer"">
    <Table Name=""Customers""/>
    <Association Member=""Orders"" KeyMembers=""CustomerID""/>
  </Entity>
  <Entity Id=""Order"">
    <Table Name=""Orders""/>
    <Association Member=""Customer"" KeyMembers=""CustomerID""/>
  </Entity>
 </map>
";
        }

        public void TestAssociation_XmlMapping_Orders()
        {
            var provider = this.GetProvider().WithMapping(
                XmlMapping.FromXml(Association_XmlMapping.Xml, typeof(Association_XmlMapping.Customer).Assembly));

            var items = provider.GetTable<Association_XmlMapping.Customer>()
                .Where(c => c.Orders.Count() > 1)
                .ToList();

            Assert.Equal(88, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.CustomerID);
                Assert.NotEqual(null, item.ContactName);
                Assert.NotEqual(null, item.Phone);
                Assert.NotEqual(null, item.Orders);
                Assert.Equal(0, item.Orders.Count); // not retrieved, no policy
            }
        }

        public void TestAssociation_XmlMapping_Customer()
        {
            var provider = this.GetProvider().WithMapping(
                XmlMapping.FromXml(Association_XmlMapping.Xml, typeof(Association_XmlMapping.Customer).Assembly));

            var items = provider.GetTable<Association_XmlMapping.Order>()
                .Where(o => o.Customer.ContactName.StartsWith("Maria"))
                .ToList();

            Assert.Equal(25, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.OrderID);
                Assert.NotEqual(null, item.CustomerID);
                Assert.Equal(null, item.Customer); // not retrieved, no policy
            }
        }

        class Association_XmlMapping_DifferentKeys
        {
            public class Customer
            {
                public string ID;
                public string ContactName;
                public string Phone;
                public List<Order> Orders = new List<Order>();
            }

            public class Order
            {
                public int ID;
                public string CustomerID;
                public Customer Customer;
            }

            public static string Xml = @"
<map>
  <Entity Id=""Customer"">
    <Table Name=""Customers""/>
    <Column Member=""ID"" Name=""CustomerID""/>
    <Association Member=""Orders"" KeyMembers=""ID"" RelatedKeyMembers=""CustomerID""/>
  </Entity>
  <Entity Id=""Order"">
    <Table Name=""Orders""/>
    <Column Member=""ID"" Name=""OrderID""/>
    <Association Member=""Customer"" KeyMembers=""CustomerID"" RelatedKeyMembers=""ID""/>
  </Entity>
 </map>
";
        }

        public void TestAssociation_XmlMapping_DifferentKeys_Orders()
        {
            var provider = this.GetProvider().WithMapping(
                XmlMapping.FromXml(Association_XmlMapping_DifferentKeys.Xml, typeof(Association_XmlMapping_DifferentKeys.Customer).Assembly));

            var items = provider.GetTable<Association_XmlMapping_DifferentKeys.Customer>()
                .Where(c => c.Orders.Count() > 1)
                .ToList();

            Assert.Equal(88, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.ContactName);
                Assert.NotEqual(null, item.Phone);
                Assert.NotEqual(null, item.Orders);
                Assert.Equal(0, item.Orders.Count);
            }
        }

        public void TestAssociation_XmlMapping_DifferenteKeys_Customer()
        {
            var provider = this.GetProvider().WithMapping(
                XmlMapping.FromXml(Association_XmlMapping_DifferentKeys.Xml, typeof(Association_XmlMapping_DifferentKeys.Customer).Assembly));

            var items = provider.GetTable<Association_XmlMapping_DifferentKeys.Order>()
                .Where(o => o.Customer.ContactName.StartsWith("Maria"))
                .ToList();

            Assert.Equal(25, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.CustomerID);
                Assert.Equal(null, item.Customer); // not retrieved, no policy
            }
        }

        #endregion

        #endregion

        #region Nested Entities
        class Nested_AttributesDistributed
        {
            [Table(Name = "Employees")]
            public class Employee
            {
                public int EmployeeID;
                public string LastName;
                public string FirstName;
                public string Title;

                [NestedEntity]
                public Address Address;
            }

            public class Address
            {
                [Column(Name = "Address")]
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }
        }

        public void TestNestedEntity_AttributesDistributed()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Nested_AttributesDistributed.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesDistributed_NoNestedEntity
        {
            [Table(Name = "Employees")]
            public class Employee
            {
                public int EmployeeID;
                public string LastName;
                public string FirstName;
                public string Title;
                public Address Address;
            }

            public class Address
            {
                [Column(Name = "Address")]
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }
        }

        public void TestNestedEntity_AttributesDistributed_NoNestedEntity()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Nested_AttributesDistributed_NoNestedEntity.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesDistributed_MultipleNestedEntities
        {
            [Table(Name = "Employees")]
            public class Employee
            {
                public int EmployeeID;

                [NestedEntity]
                public Name Name;

                [NestedEntity]
                public Address Address;
            }

            public class Name
            {
                [Column(Name = "LastName")]
                public string Last;

                [Column(Name = "FirstName")]
                public string First;

                public string Title;
            }

            public class Address
            {
                [Column(Name = "Address")]
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }
        }

        public void TestNestedEntity_AttributesDistributed_MultipleNestedEntities()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Nested_AttributesDistributed_MultipleNestedEntities.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Name.First);
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesDistributed_DeeplyNested
        {
            [Table(Name = "Employees")]
            public class Employee
            {
                public int EmployeeID;

                [NestedEntity]
                public Person Person;

                [NestedEntity]
                public Address Address;
            }

            public class Person
            {
                [NestedEntity]
                public Name Name;
                public string Title;
            }

            public class Name
            {
                [Column(Name = "LastName")]
                public string Last;

                [Column(Name = "FirstName")]
                public string First;
            }

            public class Address
            {
                [Column(Name = "Address")]
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }
        }

        public void TestNestedEntity_AttributesDistributed_DeeplyNested()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Nested_AttributesDistributed_DeeplyNested.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Person);
                Assert.NotEqual(null, item.Person.Title);
                Assert.NotEqual(null, item.Person.Name);
                Assert.NotEqual(null, item.Person.Name.First);
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesAtRoot
        {
            [Table(Name = "Employees")]
            public class Employee
            {
                public int EmployeeID;
                public string LastName;
                public string FirstName;
                public string Title;

                [NestedEntity]
                [Column(Member = "Address.Street", Name = "Address")]
                public Address Address;
            }

            public class Address
            {
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }
        }

        public void TestNestedEntity_AttributesAtRoot()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Nested_AttributesAtRoot.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesAtRoot_PartialPath
        {
            [Table(Name = "Employees")]
            public class Employee
            {
                public int EmployeeID;
                public string LastName;
                public string FirstName;
                public string Title;

                [NestedEntity]
                [Column(Member = "Street", Name = "Address")]
                public Address Address;
            }

            public class Address
            {
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }
        }

        public void TestNestedEntity_AttributesAtRoot_PartialPath()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Nested_AttributesAtRoot_PartialPath.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesAtRoot_MultipleNestedEntities
        {
            [Table(Name = "Employees")]
            public class Employee
            {
                public int EmployeeID;

                [NestedEntity]
                [Column(Member = "Last", Name = "LastName")]
                [Column(Member = "Name.First", Name = "FirstName")]
                public Name Name;

                [NestedEntity]
                [Column(Member = "Address.Street", Name = "Address")]
                public Address Address;
            }

            public class Name
            {
                public string Last;
                public string First;
                public string Title;
            }

            public class Address
            {
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }
        }

        public void TestNestedEntity_AttributesAtRoot_MultipleNestedEntities()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Nested_AttributesAtRoot_MultipleNestedEntities.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Name.First);
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesAtRoot_DeeplyNested
        {
            [Table(Name = "Employees")]
            public class Employee
            {
                public int EmployeeID;

                [NestedEntity]
                [Column(Member = "Person.Name.Last", Name = "LastName")]
                [Column(Member = "Name.First", Name = "FirstName")]
                public Person Person;

                [NestedEntity]
                [Column(Member = "Street", Name = "Address")]
                public Address Address;
            }

            public class Person
            {
                [NestedEntity]
                public Name Name;
                public string Title;
            }

            public class Name
            {
                public string Last;
                public string First;
            }

            public class Address
            {
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }
        }

        public void TestNestedEntity_AttributesAtRoot_DeeplyNested()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<Nested_AttributesAtRoot_DeeplyNested.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Person);
                Assert.NotEqual(null, item.Person.Title);
                Assert.NotEqual(null, item.Person.Name);
                Assert.NotEqual(null, item.Person.Name.First);
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesOnContext
        {
            public class Employee
            {
                public int EmployeeID;
                public string Last;
                public string First;
                public string Title;
                public Address Address;
            }

            public class Address
            {
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }

            public abstract class Context
            {
                [Table(Name="Employees")]
                [Column(Member="Employees.Last", Name="LastName")]
                [Column(Member="First", Name="FirstName")]
                [Column(Member="Address.Street", Name="Address")]
                public abstract IQueryable<Employee> Employees { get; }
            }
        }

        public void TestNestedEntity_AttributesOnContext()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping(typeof(Nested_AttributesOnContext.Context)));
            var items = provider.GetTable<Nested_AttributesOnContext.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_AttributesOnContext_DeeplyNested
        {
            public class Employee
            {
                public int EmployeeID;
                public Person Person;
                public Address Address;
            }

            public class Person
            {
                public Name Name;
                public string Title;
            }

            public class Name
            {
                public string Last;
                public string First;
            }

            public class Address
            {
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }

            public abstract class Context
            {
                [Table(Name = "Employees")]
                [Column(Member = "Employees.Person.Name.Last", Name = "LastName")]
                [Column(Member = "Person.Name.First", Name = "FirstName")]
                [Column(Member = "Address.Street", Name = "Address")]
                public abstract IQueryable<Employee> Employees { get; }
            }
        }

        public void TestNestedEntity_AttributesOnContext_DeeplyNested()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping(typeof(Nested_AttributesOnContext_DeeplyNested.Context)));
            var items = provider.GetTable<Nested_AttributesOnContext_DeeplyNested.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Person);
                Assert.NotEqual(null, item.Person.Title);
                Assert.NotEqual(null, item.Person.Name);
                Assert.NotEqual(null, item.Person.Name.First);
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_XmlMapped
        {
            public class Employee
            {
                public int EmployeeID;
                public string LastName;
                public string FirstName;
                public string Title;
                public Address Address;
            }

            public class Address
            {
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }

            public static string Xml = @"
<map>
  <Entity Id=""Employee"">
    <Table Name=""Employees""/>
    <NestedEntity Member=""Address"">
        <Column Member=""Street"" Name=""Address""/>
    </NestedEntity>
  </Entity>
 </map>
";
        }

        public void TestNestedEntity_XmlMapped()
        {
            var provider = this.GetProvider().WithMapping(XmlMapping.FromXml(Nested_XmlMapped.Xml));
            var items = provider.GetTable<Nested_XmlMapped.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        class Nested_XmlMapped_DeeplyNested
        {
            public class Employee
            {
                public int EmployeeID;
                public Person Person;
                public Address Address;
            }

            public class Person
            {
                public Name Name;
                public string Title;
            }

            public class Name
            {
                public string Last;
                public string First;
            }

            public class Address
            {
                public string Street;
                public string City;
                public string Region;
                public string PostalCode;
            }

            public static string Xml = @"
<map>
  <Entity Id=""Employee"">
    <Table Name=""Employees""/>
    <NestedEntity Member=""Person"">
      <NestedEntity Member=""Name"">
        <Column Member=""Last"" Name=""LastName""/>
        <Column Member=""First"" Name=""FirstName""/>
      </NestedEntity>
    </NestedEntity>
    <NestedEntity Member=""Address"">
      <Column Member=""Street"" Name=""Address""/>
    </NestedEntity>
  </Entity>
 </map>
";
        }

        public void TestNestedEntity_XmlMapped_DeeplyNested()
        {
            var provider = this.GetProvider().WithMapping(XmlMapping.FromXml(Nested_XmlMapped_DeeplyNested.Xml));
            var items = provider.GetTable<Nested_XmlMapped_DeeplyNested.Employee>().ToList();

            Assert.Equal(9, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.Person);
                Assert.NotEqual(null, item.Person.Title);
                Assert.NotEqual(null, item.Person.Name);
                Assert.NotEqual(null, item.Person.Name.First);
                Assert.NotEqual(null, item.Address);
                Assert.NotEqual(null, item.Address.Street);
            }
        }

        #endregion

        #region Multi-Table Entities
        class MultiTable_AttributesOnEntity
        {
            [Table(Name = "Orders")]
            [ExtensionTable(Name ="Customers", KeyColumns="CustomerID")]
            public class CustomerOrder
            {
                [Column(Name="OrderID")]
                public int ID;

                public string CustomerID;

                [Column(TableId="Customers", Name="ContactName")]
                public string Name;

                [Column(TableId="Customers")]
                public string Phone;
            }
        }

        public void TestMultiTable_AttributesOnEntity()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping());
            var items = provider.GetTable<MultiTable_AttributesOnEntity.CustomerOrder>().Take(20).ToList();

            Assert.Equal(20, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.CustomerID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }

        class MultiTable_AttributesOnContext
        {
            public class CustomerOrder
            {
                public int ID;
                public string CustomerID;
                public string Name;
                public string Phone;
            }

            public abstract class Context
            {
                [Table(Name = "Orders")]
                [ExtensionTable(Name = "Customers", KeyColumns = "CustomerID")]
                [Column(Member = "ID", Name = "OrderID")]
                [Column(Member = "Name", TableId = "Customers", Name = "ContactName")]
                [Column(Member = "Phone", TableId = "Customers")]
                public abstract IQueryable<CustomerOrder> CustomerOrders { get; }
            }
        }

        public void TestMultiTable_AttributesOnContext()
        {
            var provider = this.GetProvider().WithMapping(new AttributeMapping(typeof(MultiTable_AttributesOnContext.Context)));
            var items = provider.GetTable<MultiTable_AttributesOnContext.CustomerOrder>().Take(20).ToList();

            Assert.Equal(20, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.CustomerID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }

        class MultiTable_XmlMapped
        {
            public class CustomerOrder
            {
                public int ID;
                public string CustomerID;
                public string Name;
                public string Phone;
            }

            public static readonly string Xml = @"
<map>
  <Entity Id=""CustomerOrder"">
    <Table Name=""Orders""/>
    <ExtensionTable Name=""Customers"" KeyColumns=""CustomerID""/>
    <Column Member=""ID"" Name=""OrderID""/>
    <Column Member=""Name"" TableId=""Customers"" Name=""ContactName""/>
    <Column Member=""Phone"" TableId=""Customers""/>
  </Entity>
</map>
";
        }

        public void TestMultiTable_XmlMapped()
        {
            var provider = this.GetProvider().WithMapping(XmlMapping.FromXml(MultiTable_XmlMapped.Xml));
            var items = provider.GetTable<MultiTable_XmlMapped.CustomerOrder>().Take(20).ToList();

            Assert.Equal(20, items.Count);

            foreach (var item in items)
            {
                Assert.NotEqual(null, item.ID);
                Assert.NotEqual(null, item.CustomerID);
                Assert.NotEqual(null, item.Name);
                Assert.NotEqual(null, item.Phone);
            }
        }
        #endregion
    }
}
