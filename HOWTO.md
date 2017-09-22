# How to use IQToolkit

IQToolkit is a simple ORM (object-relational mapper).
It allows use you write LINQ queries in your application and 
execute them against a database.

IQToolkit was developed by Matt Warren as a by-product of his [blog series on how to
write a LINQ IQueryable query provider](https://blogs.msdn.microsoft.com/mattwar/2007/07/30/linq-building-an-iqueryable-provider-part-i/).

The current IQToolkit goes beyond what was discussed in the series, and presents a mini
ORM that can be used to query and interact with many kinds of databases.

The current live repository for the source can be found [here](https://github.com/mattwar/iqtoolkit).

<br/>

## How do I get it?

You'll either need to build the sources yourself or download the built packages from Nuget.
You can find official packages on nuget under the following names:

1. **IQToolkit.Common** -- API and shared libraries
2. **IQToolkit.Access**  -- A query provider for Microsoft Access (.mdb and .accdb) files.
3. **IQToolkit.SqlClient** -- A query provider for Microsoft SQL Server client (System.Data.SqlClient)
4. **IQToolkit.SqlServerCe** -- A query provider Microsoft SQL Server Compact Edition (System.Data.SqlServerCe)
5. **IQToolkit.SQLite** -- A query provider SQLite (.db3) files (System.Data.SQLite)
6. **IQToolkit.MySql** -- A query provider for MySql (MySql.Data)

<br/>

# The Basics

For these examples I will be using the Access query provider using the Northwind sample database.

*You can get a copy of the database by downloading it from the IQToolkit sources on GitHub
in the [Access query provider test project](https://github.com/mattwar/iqtoolkit/tree/master/src/Test.Access).*
*It has both the .mdb and .accdb versions. Either will work.*

I create the query provider by constructing an instance of `AccessQueryProvider`.

```c#
using System.Linq;
using IQToolkit;
using IQToolkit.Access;
using IQToolkit.Data;
...
var provider = new AccessQueryProvider("northwind.mdb");
```

Specifying the name of a database file or a standard ADO.Net (System.Data) connection string is all I need to get started.

If I already have a `DbConnection` instance from existing code, I can specify it in the constructor instead.

```c#
OleDbConnection connection = ...;
var provider = new AccessQueryProvider(connection);
```
<br/>

Querying
--------

In order to write a query, I first need a class with fields and/or properties that correspond to the columns of a table in the database. 

For this example, I'll define a `Customer` class to use to query the `Customers` table in the Northwind database.

```c#
public class Customer
{
    public string CustomerID;
    public string ContactName;
    public string Phone;
}
```

I don't have to specify members for all the columns in the database, just the ones I want to reference or return in my query result.

Now with both the provider and the `Customer` class I can issue a query against the `Customers` table.

```c#
var query = from c in provider.GetTable<Customer>() 
            where c.ContactName.StartsWith("Maria")
            select c;

var results = query.ToList();
```

The database query that gets executed looks like this:
```SQL
SELECT t0.[ContactName], t0.[CustomerID], t0.[Phone]
FROM [Customers] AS t0
WHERE (t0.[ContactName] LIKE p0 + '%')
```
<br/>

Mapping
-------

Mapping is information that an ORM uses to determine how types and members in your program correspond to tables and columns in the database.

Notice how I did not define any mapping in the prior example, just a simple class.
By default, the provider assumed the public members of the `Customer` type corresponded to the same named columns in the database.

However, if I want to have the member names differ from the column names, I am going to need to specify it somehow.

I can use attributes on my `Customer` class to provide additional information to the provider.

```c#
[Table(Name="Customers")]
public class Customer
{
    [Column(Name="CustomerID")]
    public string ID;

    [Column(Name="ContactName")]
    public string Name;

    public string Phone;
}
```

I only need to supply an attribute where I want information to differ from what would otherwise be inferred.

Now I can write a query using the names I think make sense to my application, even if the database names are different.

```c#
var query = from c in provider.GetTable<Customer>() 
            where c.Name.StartsWith("Maria")
            select c;

var results = query.ToList();
```
<br/>

I can also choose to keep my entity class clean of mapping attributes and instead specify them on a separate context class that I might be using to
hide the query provider and only expose the specific queryable collections I want my application to access.

```c#
public class NorthwindContext
{
    private readonly DbQueryProvider provider;

    public NorthwindContext(DbQueryProvider provider) 
    {
        this.provider = provider.WithMapping(
            new HybridMapping(typeof(NorthwindContext)));
    }

    [Table(Name="Customers")]
    [Column(Member=nameof(Customer.ID), Name="CustomerID")]
    [Column(Member=nameof(Customer.Name), Name="ContactName")]
    public IQueryable<Customer> Customers => this.provider.GetTable<Customer>();
}
```

If I do so, I can keep the `Customer` class free from being tied to a specific mapping. 
Who knows, I may want to reuse this class to access data from a different table, or different database even.

Notice in the constructor I have to change the mapping that the provider is using to one that knows about the context class.
I constructed a new instance of `HybridMapping` telling it about the context class.

```C#
this.provider = provider.WithMapping(
    new HybridMapping(typeof(NorthwindContext)));
```
*(`HybridMapping` is the kind of mapping used when you don't otherwise specify one.)*

Now it knows where to look for those mapping attributes.  

If I wanted to, I could point it at some other
class with properties and attributes that I don't even expose to the rest of my code. 
But that's just silly, because if I wanted to get the mapping information that far away from my code,
I might as well boot it entirely out of the source altogether.

Alas, I can do that too. Instead of in source, I can specify the mapping in a separate file that I load at runtime.
The `XmlMapping` class can be used to extract mapping information from an xml document 
(that is basically just a serialized form of the attributes).

```c#
var mapping = XmlMapping.FromXml(File.ReadToEnd("mapping.xml"));
var provider = new AccessQueryProvider("northwind.mdb", mapping);
```
If I want to use an xml file mapping or any other kind of mapping, I can be explicit about it and specify it when I construct the provider.

More on this in advanced topics.

<br/>

Relationships
-------------

Real world relationships might be hard, but database ones are not.

A relationship in a database is typically an association between two different database tables.
For example, the `Orders` table in the Northwind database has a many-to-one relationship between it and the `Customers` table.
How it works is that both tables define a `CustomerID` column that can be used in a database query to identify the rows of each table that go together.
The relationship is many-to-one, because many different orders can have the same `CustomerID` value,
each corresponding to the same row in the `Customers` table.

It is interesting to note, that if I were writing a database query in SQL I would have 
to know all about each relationship's details, which columns in each table to use, 
because each time I wanted to bring both tables together I would have to specify the join clause explicitly.

An ORM like IQToolkit frees me from having to know this every time I write a query.
Instead, I can just bake this knowledge into the mapping, and use normal object references
in my code to represent them, and simple dot-notation to bring the two tables together.

I can add a list of orders to the `Customer` class, because each customer corresponds to
zero or more orders.

```c#
public class Customer
{
    // data members
    public string CustomerID;
    public string ContactName;
    public string Phone;

    // relationships
    public List<Order> Orders = new List<Order>();
}
```

Likewise, I can add a `Customer` field to `Order` because each order 
can have a single customer it corresponds to.

```c#
public class Order
{
    // data members
    public int OrderID;
    public string CustomerID;

    // relationships
    public Customer Customer;
}
```

And now I am free to issue queries against the `Customers` table that automatically joins itself
against the `Orders` table simply by referencing the `Orders` field in the `Customer` type.

```c#
var db = new NorthwindContext(provider);

var query = from c in db.Customers
            where c.Orders.Any()
            select c;

var results = query.ToList();
```

Which executes the following database query:

```SQL
SELECT t0.[ContactName], t0.[CustomerID], t0.[Phone]
FROM [Customers] AS t0
WHERE EXISTS(
  SELECT 0
  FROM [Orders] AS t1
  WHERE (t1.[CustomerID] = t0.[CustomerID])
  )
```

To be clear about the relationship in the mapping, I can define it
using the `Association` attribute.  It defines an assocation mapping
between the values in specific members of the `Customer` and `Order` types.

```c#
public class NorthwindContext
{
    private readonly DbEntityProvider provider;

    public NorthwindContext(DbEntityProvider provider)
    {
        this.provider = provider.WithMapping(new HybridMapping(typeof(NorthwindContext)));
    }

    [Table(Name="Customers")]
    [Column(Member=nameof(Customer.CustomerID), IsPrimaryKey=true)]
    [Column(Member=nameof(Customer.ContactName))]
    [Association(
        Member=nameof(Customer.Orders), 
        KeyMembers=nameof(Customer.CustomerID),
        RelatedKeyMembers=nameof(Order.CustomerID))]
    public IQueryable<Customer> Customers => this.provider.GetTable<Customer>();

    [Table(Name="Orders")]
    [Column(Member=nameof(Order.OrderID), IsPrimaryKey=true)]
    [Column(Member=nameof(Order.CustomerID))]
    [Association(
        Member=nameof(Order.Customer), 
        KeyMembers=nameof(Order.CustomerID),
        RelatedKeyMembers=nameof(Customer.CustomerID), 
        IsForeignKey=true)]
    public IQueryable<Order> Orders => this.provider.GetTable<Order>();
}
```
Or I can put those attributes directly on the `Customer` and `Order` classes.

```c#
[Table(Name="Customers")]
public class Customer
{
    // data members
    [Column(IsPrimaryKey=true)]
    public string CustomerID;
    
    [Column]
    public string ContactName;

    [Column]
    public string Phone;

    // relationships
    [Association(
        KeyMembers=nameof(Customer.CustomerID),
        RelatedKeyMembers=nameof(Order.CustomerID))]
    public List<Order> Orders = new List<Order>();
}

[Table(Name="Orders")]
public class Order
{
    // data members
    [Column(IsPrimaryKey=true)]
    public int OrderID;

    [Column]
    public string CustomerID;

    // relationships
    [Association(
        KeyMembers=nameof(Order.CustomerID),
        RelatedKeyMembers=nameof(Customer.CustomerID),
        IsForeignKey=true)]
    public Customer Customer;
}
```
But probably not both.

Or I can specify nothing, and hope the default inferrence heuristic will figure it out.

*It probably won't.*

If I wanted to bring back data from both tables, I can write a query that references both in the result.

```c#
var query = from o in db.Orders
            select new { o.OrderID, o.Customer.ContactName };
```

It will cause a join and return column data from both tables.

```SQL
SELECT t0.[OrderID], t1.[ContactName]
FROM [Orders] AS t0
LEFT OUTER JOIN [Customers] AS t1
  ON (t1.[CustomerID] = t0.[CustomerID])
```

But notice, if I just query for customers, I'll get back customers that have
an Orders property, but the collection will be empty. 

```c#
var count = (from c in db.Customers
              where c.CustomerID == "ALFKI"
              select c.Orders.Count).Single();

var customer = (from c in db.Customers
            where c.CustomerID == "ALFKI"
            select c).Single();

var equal = count == customer.Orders.Count; // false: 6 != 0
```

Don't worry. That is as intended.

If I want my relationship members to be included, in my query results, I am going to have to specify a policy.

<br/>

Policy
------
Query policies control what optional parts of an entity get returned in the query results.
An entity is any type that is mapped or corresponds to the layout of a database table.

I can create a policy and assign it to the query provider when I construct it.
Then I can configure the policy to include the elements of the `Orders` collection
any time my query returns `Customer`.

```c#
var policy = new EntityPolicy();
var provider = new AccessQueryProvider("northwind.mdb", policy: policy);

policy.IncludeWith<Customer>(c => c.Orders);

var customer = (from c in db.Customers
                where c.CustomerID == "ALFKI"
                select c;).Single();

var success = customer.Orders.Count == 6;
```

When I do so, I end up executing more than one database query;
one for the customers and one for the matching orders.

```SQL
SELECT t1.[CustomerID], t1.[OrderID]
FROM [Customers] AS t0
LEFT OUTER JOIN [Orders] AS t1
  ON (t1.[CustomerID] = t0.[CustomerID])
WHERE (t0.[CustomerID] = p0)

SELECT t0.[ContactName], t0.[CustomerID], t0.[Phone]
FROM [Customers] AS t0
WHERE (t0.[CustomerID] = p0)
```

<br/>

Inserts, Updates and Deletes
----------------------------

In addition to querying for data, IQToolkit can be used to insert, update or delete rows from database tables.

In order to do this, I'll have to give my application access to these API's.
They are defined for the collections with the type `IUpdatable<T>` as extension methods,
and also explicitly on the type `IEntityTable<T>` that the method `IEntityProvider.GetTable<T>()` returns.

If I'm using a context class I'll have to make sure my collection properties return at least `IUpdatable<T>` instead of just `IQueryable<T>`, 
so my code can use the extra API's.

```c#
public class NorthwindContext
{
    private readonly DbEntityProvider provider;

    public NorthwindContext(DbEntityProvider provider)
    {
        this.provider = provider.WithMapping(new HybridMapping(typeof(NorthwindContext)));
    }

    [Table(Name="Customers")]
    [Column(Member=nameof(Customer.CustomerID), IsPrimaryKey=true)]
    [Column(Member=nameof(Customer.ContactName))]
    [Association(
        Member=nameof(Customer.Orders), 
        KeyMembers=nameof(Customer.CustomerID),
        RelatedKeyMembers=nameof(Order.CustomerID))]
    public IUpdatable<Customer> Customers => this.provider.GetTable<Customer>();

    [Table(Name="Orders")]
    [Column(Member=nameof(Order.OrderID), IsPrimaryKey=true)]
    [Column(Member=nameof(Order.CustomerID))]
    [Association(
        Member=nameof(Order.Customer), 
        KeyMembers=nameof(Order.CustomerID),
        RelatedKeyMembers=nameof(Customer.CustomerID),
        IsForeignKey=true)]
    public IUpdatable<Order> Orders => this.provider.GetTable<Order>();
}
```

Now, I can use the updatable collection to insert, update or delete rows from the database table
by specifying entity instances in my program that correspond to rows I want the database to operate on.

Unlike quering, however, for insert and update we'll likely need to specify fields/properties
for all columns that are required by the database.

So I will redefine `Customer` as:
```c#
public class Customer
{
    // data members
    public string CustomerID;
    public string ContactName;
    public string CompanyName;
    public string Phone;
    public string City;
    public string Country;

    // relationships
    public List<Order> Orders = new List<Order>();
}
```

**To insert:**

```c#
var cust = new Customer {
                CustomerID ="NEWGU",
                ContactName = "New Guy",
                CompanyName = "Some Company",
                City = "Some Place",
                Country = "Some Where",
                Phone = "(xxx) yyy zzzz" };

db.Customers.Insert(cust);
```

The database command that is executed:

```SQL
INSERT INTO [Customers](
    [City], 
    [CompanyName], 
    [ContactName], 
    [Country], 
    [CustomerID], 
    [Phone])
VALUES (p0, p1, p2, p3, p4, p5)
```

**To update:**

```c#
// get a customer from the database
var cust = db.Customers.Single(c => c.CustomerID == "ALFKI");

// change one or more members
cust.Phone = "(xxx) yyy zzz";

// update it
db.Customers.Update(cust);
```

The database command that is executed:

```SQL
UPDATE [Customers]
SET [City] = p1, 
    [CompanyName] = p2, 
    [ContactName] = p3, 
    [Country] = p4, 
    [Phone] = p5
WHERE ([CustomerID] = p0)
```

**To delete:**

```c#
// get a customer from the database
var cust = db.Customers.Single(c => c.CustomerID == "ALFKI");

// delete it
db.Customers.Delete(cust);
```

The database command that is executed:

```SQL
DELETE FROM [Customers]
WHERE ([CustomerID] = p0)
```

<br/>

## Advanced Topics

Sessions
--------

Multi-Table Mapping
-------------------

Nested Entity Mapping
---------------------

Transactions
------------
Logs
----

