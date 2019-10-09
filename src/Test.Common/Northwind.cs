// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    using IQToolkit;
    using IQToolkit.Data;
    using IQToolkit.Data.Mapping;

    public class Customer
    {
        public string CustomerID;
        public string ContactName;
        public string CompanyName;
        public string Phone;
        public string City;
        public string Country;
        public IList<Order> Orders;
    }

    public class Order
    {
        public int OrderID;
        public string CustomerID;
        public DateTime OrderDate;
        public Customer Customer;
        public List<OrderDetail> Details;
    }

    public class OrderDetail
    {
        public int? OrderID { get; set; }
        public int ProductID { get; set; }
        public Product Product;
        public Order Order;
    }

    public interface IEntity
    {
        int ID { get; }
    }

    public class Product : IEntity
    {
        public int ID;
        public string ProductName;
        public bool Discontinued;

        int IEntity.ID
        {
            get { return this.ID; }
        }
    }

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
        public string Street { get; private set; }
        public string City { get; private set; }
        public string Region { get; private set; }
        public string PostalCode { get; private set; }

        public Address(string street, string city, string region, string postalCode)
        {
            this.Street = street;
            this.City = city;
            this.Region = region;
            this.PostalCode = postalCode;
        }
    }

    public class Northwind
    {
        private readonly IEntityProvider provider;

        public Northwind(IEntityProvider provider)
        {
            this.provider = provider;
        }

        public IEntityProvider Provider
        {
            get { return this.provider; }
        }

        public virtual IEntityTable<Customer> Customers
        {
            get { return this.provider.GetTable<Customer>(); }
        }

        public virtual IEntityTable<Order> Orders
        {
            get { return this.provider.GetTable<Order>(); }
        }

        public virtual IEntityTable<OrderDetail> OrderDetails
        {
            get { return this.provider.GetTable<OrderDetail>(); }
        }

        public virtual IEntityTable<Product> Products
        {
            get { return this.provider.GetTable<Product>(); }
        }

        public virtual IEntityTable<Employee> Employees
        {
            get { return this.provider.GetTable<Employee>(); }
        }
    }

    public class NorthwindWithAttributes : Northwind
    {
        public NorthwindWithAttributes(IEntityProvider provider)
            : base(provider)
        {
        }

        [Table]
        [Column(Member = nameof(Customer.CustomerID), IsPrimaryKey = true)]
        [Column(Member = nameof(Customer.ContactName))]
        [Column(Member = nameof(Customer.CompanyName))]
        [Column(Member = nameof(Customer.Phone))]
        [Column(Member = nameof(Customer.City), DbType="NVARCHAR(20)")]
        [Column(Member = nameof(Customer.Country))]
        [Association(Member = nameof(Customer.Orders), KeyMembers = nameof(Customer.CustomerID), RelatedKeyMembers = nameof(Order.CustomerID))]
        public override IEntityTable<Customer> Customers
        {
            get { return base.Customers; }
        }
        
        [Table]
        [Column(Member = nameof(Order.OrderID), IsPrimaryKey = true, IsGenerated = true)]
        [Column(Member = nameof(Order.CustomerID))]
        [Column(Member = nameof(Order.OrderDate))]
        [Association(Member = nameof(Order.Customer), KeyMembers = nameof(Order.CustomerID), RelatedKeyMembers = nameof(Customer.CustomerID))]
        [Association(Member = nameof(Order.Details), KeyMembers = nameof(Order.OrderID), RelatedKeyMembers = nameof(OrderDetail.OrderID))]
        public override IEntityTable<Order> Orders
        {
            get { return base.Orders; }
        }

        [Table(Name = "Order Details")]
        [Column(Member = nameof(OrderDetail.OrderID), IsPrimaryKey = true)]
        [Column(Member = nameof(OrderDetail.ProductID), IsPrimaryKey = true)]
        [Association(Member = nameof(OrderDetail.Product), KeyMembers = nameof(OrderDetail.ProductID), RelatedKeyMembers = nameof(Product.ID))]
        [Association(Member = nameof(OrderDetail.Order), KeyMembers =nameof(OrderDetail.OrderID), RelatedKeyMembers = nameof(Order.OrderID))]
        public override IEntityTable<OrderDetail> OrderDetails
        {
            get { return base.OrderDetails; }
        }

        [Table]
        [Column(Member = nameof(Product.ID), Name="ProductId", IsPrimaryKey = true)]
        [Column(Member = nameof(Product.ProductName))]
        [Column(Member = nameof(Product.Discontinued))]
        public override IEntityTable<Product> Products
        {
            get { return base.Products; }
        }

        [Table]
        [Column(Member = nameof(Employee.EmployeeID), IsPrimaryKey = true)]
        [Column(Member = nameof(Employee.LastName))]
        [Column(Member = nameof(Employee.FirstName))]
        [Column(Member = nameof(Employee.Title))]
        [Column(Member = "Address.Street", Name = "Address")]
        [Column(Member = "Address.City")]
        [Column(Member = "Address.Region")]
        [Column(Member = "Address.PostalCode")]
        public override IEntityTable<Employee> Employees
        {
            get { return base.Employees; }
        }
    }

    public interface INorthwindSession
    {
        void SubmitChanges();
        ISessionTable<Customer> Customers { get; }
        ISessionTable<Order> Orders { get; }
        ISessionTable<OrderDetail> OrderDetails { get; }
    }

    public class NorthwindSession : INorthwindSession
    {
        IEntitySession session;

        public NorthwindSession(EntityProvider provider)
            : this(new EntitySession(provider))
        {
        }

        public NorthwindSession(IEntitySession session)
        {
            this.session = session;
        }

        public IEntitySession Session
        {
            get { return this.session; }
        }

        public void SubmitChanges()
        {
            this.session.SubmitChanges();
        }

        public ISessionTable<Customer> Customers
        {
            get { return this.session.GetTable<Customer>(); }
        }

        public ISessionTable<Order> Orders
        {
            get { return this.session.GetTable<Order>(); }
        }

        public ISessionTable<OrderDetail> OrderDetails
        {
            get { return this.session.GetTable<OrderDetail>(); }
        }
    }

    public class CustomerX
    {
        public CustomerX(string customerId, string contactName, string companyName, string phone, string city, string country, List<OrderX> orders)
        {
            this.CustomerID = customerId;
            this.ContactName = contactName;
            this.CompanyName = companyName;
            this.Phone = phone;
            this.City = city;
            this.Country = country;
            this.Orders = orders;
        }

        public string CustomerID { get; private set; }
        public string ContactName { get; private set; }
        public string CompanyName { get; private set; }
        public string Phone { get; private set; }
        public string City { get; private set; }
        public string Country { get; private set; }
        public List<OrderX> Orders { get; private set; }
    }

    public class OrderX
    {
        public OrderX(int orderID, string customerID, DateTime orderDate, CustomerX customer)
        {
            this.OrderID = orderID;
            this.CustomerID = customerID;
            this.OrderDate = orderDate;
            this.Customer = customer;
        }

        public int OrderID { get; private set; }
        public string CustomerID { get; private set; }
        public DateTime OrderDate { get; private set; }
        public CustomerX Customer { get; private set; }
    }

    public class NorthwindX
    {
        EntityProvider provider;

        public NorthwindX(EntityProvider provider)
        {
            this.provider = provider;
        }

        [Table]
        [Column(Member = nameof(CustomerX.CustomerID), IsPrimaryKey = true)]
        [Column(Member = nameof(CustomerX.ContactName))]
        [Column(Member = nameof(CustomerX.CompanyName))]
        [Column(Member = nameof(CustomerX.Phone))]
        [Column(Member = nameof(CustomerX.City), DbType = "NVARCHAR(20)")]
        [Column(Member = nameof(CustomerX.Country))]
        [Association(Member = nameof(CustomerX.Orders), KeyMembers = nameof(CustomerX.CustomerID), RelatedKeyMembers = nameof(OrderX.CustomerID))]
        public IQueryable<CustomerX> Customers
        {
            get { return this.provider.GetTable<CustomerX>(); }
        }

        [Table]
        [Column(Member = nameof(OrderX.OrderID), IsPrimaryKey = true, IsGenerated = true)]
        [Column(Member = nameof(OrderX.CustomerID))]
        [Column(Member = nameof(OrderX.OrderDate))]
        [Association(Member = nameof(OrderX.Customer), KeyMembers = nameof(OrderX.CustomerID), RelatedKeyMembers = nameof(CustomerX.CustomerID))]
        public IEntityTable<OrderX> Orders
        {
            get { return this.provider.GetTable<OrderX>(); }
        }
    }
}
