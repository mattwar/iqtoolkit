# How to use IQToolkit

IQToolkit is a simple ORM (object-relational mapper).
It allows you to access and manipulate data stored in database tables using LINQ queries against queryable
sequences of classes you define.

IQToolkit was developed by Matt Warren as a by-product of his [blog series on how to
write a LINQ IQueryable query provider](https://blogs.msdn.microsoft.com/mattwar/2007/07/30/linq-building-an-iqueryable-provider-part-i/).

The current IQToolkit goes beyond what was discussed in the series, and presents a mini
ORM that can be used to query and interact with many kinds of databases.

The current live repository for the source can be found [here](https://github.com/mattwar/iqtoolkit).

## How do I get it?

You'll either need to build the sources yourself or download the built packages from Nuget.
You can find official packages on nuget under the following names:

1. **IQToolkit.Common** -- API and shared libraries
2. **IQToolkit.Access**  -- A query provider for Microsoft Access (.mdb and .accdb) files.
3. **IQToolkit.SqlClient** -- A query provider for Microsoft SQL Server client (System.Data.SqlClient)
4. **IQToolkit.SqlServerCe** -- A query provider Microsoft SQL Server Compact Edition (System.Data.SqlServerCe)
5. **IQToolkit.SQLite** -- A query provider SQLite (.db3) files (System.Data.SQLite)
6. **IQToolkit.MySql** -- A query provider for MySql (MySql.Data)

# The Basics

For these examples I will be using the Access query provider using the Northwind sample database.

*You can get a copy of the database by downloading it from the IQToolkit sources on GitHub
in the [Access query provider test project](https://github.com/mattwar/iqtoolkit/tree/master/src/Test.Access).*
*It has both the .mdb and .accdb versions. Either will work.*

I create the query provider by constructing an instance of `AccessQueryProvider`.

```CSharp
using System.Linq;
using IQToolkit;
using IQToolkit.Access;
using IQToolkit.Data;
...
var provider = new AccessQueryProvider("northwind.mdb");
```

Specifying the name of a database file or a standard ADO.Net (System.Data) connection string is all I need to get started.

If I already have a `DbConnection` instance from existing code, I can specify it in the constructor instead.

```CSharp
OleDbConnection connection = ...;
var provider = new AccessQueryProvider(connection);
```

## Basic Querying

In order to write a query, I first need a class with fields and/or properties that correspond to the columns of a table in the database. 

For this example, I'll define a `Customers` class to use to query the `Customers` table in the Northwind database.

```CSharp
public class Customers
{
    public string CustomerID;
    public string ContactName;
    public string Phone;
}
```

I don't have to specify members for all the columns in the database, just the ones I want to reference or return in my query result.

Now with both the provider and the `Customers` class I can issue a query against the `Customers` table.

```CSharp
var query = from c in provider.GetTable<Customers>() 
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

## Basic Mapping

Mapping is information that an ORM uses to determine how types and members in your program correspond to tables and columns in the database.

Notice how I did not need to specify anything beyond the definition of a simple class, and with that I could retrieve the data 
from the corresponding database table into instances of that class.

However, I had to do something awkward and name the class the same name as the database table, *Customers* instead of *Customer*, 
when I would have preferred to use the singular as this class generally corresponds to a single row of data, not the whole table.
I had to do this because the provider needed some way to determine the name of the database table, and by default the name is inferred from the name of my entity class.
Likewise, the names of the columns were inferred from the names of the public fields and properties in my class.

This is perfectly fine if I am happy with those names as the names I use in my source code.
However, if I would rather have finer control I can specify this and more using a mapping.

IQToolkit defines a kind of mapping that uses source code attributes to let me specify information about how my entity class
corresponds to tables and columns in the database.  It lets me place these attributes directly on my entity class, 
to add any additional information the provider might need.

For example, I can rename my `Customers` class to `Customer` and give it a `Table` attribute to tell it the true table name.
Likewise, I can choose to have my field names differ from the column names and place `Column` attributes on these members to inform the provider of the correct column names. 

```CSharp
[Table(Name="Customers")]
public class Customer
{
    [Column(Name="CustomerID")]
    public string ID;

    [Column(Name="ContactName")]
    public string Name;

    // same name as column
    public string Phone;
}
```

I only need to supply an attribute where I want information to differ from what would otherwise be inferred.

Now I can write a query using the names I think make sense to my application, even if the database names are different.

```CSharp
var query = from c in provider.GetTable<Customer>() 
            where c.Name.StartsWith("Maria")
            select c;

var results = query.ToList();
```

*Note:  It is possible to avoid putting attributes directly on entity classes or to use an entirely different
kind of mapping like a file-based mapping instead. You can find these in the advanced mapping topics.*

###  Basic Relationships

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

I can define an `Order` class and add a list of orders to the `Customer` class.

```CSharp
[Table(Name="Customers")]
public class Customer
{
    // data members
    public string CustomerID;
    public string ContactName;
    public string Phone;

    // relationships
    public List<Order> Orders = new List<Order>();
}

[Table(Name="Orders")]
public class Order
{
    // data members
    public int OrderID;
    public string CustomerID;
}
```

But now the provider will be confused. It does not know what to make of the `Orders` field on the `Customer` class,
as it is a list and such a thing does not translate into the concept of a typical scalar database column.

What I want to be able to do is to tell the provider that the orders in this list come from the `Orders` table,
and how to figure out which orders belong to each customer.

To do this I can place an `Association` attribute on the `Orders` property. 
This tells the provider that this member represents an assocation relationship between the two tables.
I only need to specify the `KeyMembers` property because the members that I care to associate 
in both `Customer` and `Order` have the same name. If they had differed, I could have also specified the `RelatedKeyMembers` property.

```CSharp
    // relationships
    [Association(KeyMembers="CustomerID")]
    public List<Order> Orders = new List<Order>();
```

Now the provider has enough information to know that the orders in the `Orders` table that have the same
`CustomerID` value as the customers in the `Customers` table belong together.

From now on, everytime I execute a query that references the `Customer.Orders` property, 
the provider knows to automatically join the `Customers` table with the `Orders` table using these column associations.

For example: 

```CSharp
var query = from c in provider.GetTable<Customer>()
            where c.Orders.Any()
            select c;

var results = query.ToList();
```

Executes the following database query:

```SQL
SELECT t0.[ContactName], t0.[CustomerID], t0.[Phone]
FROM [Customers] AS t0
WHERE EXISTS(
  SELECT 0
  FROM [Orders] AS t1
  WHERE (t1.[CustomerID] = t0.[CustomerID])
  )
```

But now notice, that even though my query gets an automatic join by just referencing the `Orders` property,
when I actually retrieve customers that have associated orders, the `Orders` property on my returned `Customer`
instances are empty. 

```CSharp
var count = (from c in provider.GetTable<Customer>()
              where c.CustomerID == "ALFKI"
              select c.Orders.Count).Single();

var customer = (from c in provider.GetTable<Customer>()
            where c.CustomerID == "ALFKI"
            select c).Single();

var equal = count == customer.Orders.Count; // false: 6 != 0
```

This is expected. 

The choice not to automatically retrieve associated entities, even though the association is declared in the mapping, is intentional.
It means I can go ahead and describe as many association relationships that I may want to reference in my application, without unnecessarily 
retrieving much more data than my application needs.

If I want my related entities to be included in my query results, I can specify that with a query policy.

## Basic Policy

Query policies control what optional parts of an entity get returned in the query results.
An entity is any type that is mapped or corresponds to the layout of a database table.

I can create a policy and assign it to the query provider when I construct it.
Then I can configure the policy to include the elements of the `Orders` collection
any time my query returns `Customer`.

```CSharp
var policy = new EntityPolicy();
var provider = new AccessQueryProvider("northwind.mdb", policy: policy);

policy.IncludeWith<Customer>(c => c.Orders);

var customer = (from c in provider.GetTable<Cusotmer>()
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

## Basic Inserts, Updates and Deletes

In addition to using entity classes to query for database data, 
I can use those same classes to insert, update or delete rows from database tables.

Unlike with quering, however, I may have more requirements on my class definition and mapping.
For example, I will need to at least specify members for all columns that are required by the database for inserting new rows.

So I redefine `Customer` as:
```CSharp
[Table(Name="Customers")]
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
    [Association(KeyMembers="CustomerID")]
    public List<Order> Orders = new List<Order>();
}
```

Now all I really need to do is get my hands on an appropriate instance of `Customer` and 
call the appropriate API method.

### Inserting

To insert a new entity into the `Customers` table, I just need a `Customer` instance representing the new customer, and
then I can simply call the `Insert` method.

*Note: `Insert` is actually an extension method for the interface `IUpdatable<T>`*

```CSharp
var cust = new Customer {
                CustomerID ="NEWGU",
                ContactName = "New Guy",
                CompanyName = "Some Company",
                City = "Some Place",
                Country = "Some Where",
                Phone = "(xxx) yyy zzzz" };

provider.GetTable<Customer>().Insert(cust);
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

### Updating

If I want to update an entity, I probably will want to use one I have already retrieved via a query.

```CSharp
// get a customer from the database
var cust = provider.GetTable<Customer>().Single(c => c.CustomerID == "ALFKI");

// change one or more members
cust.Phone = "(xxx) yyy zzz";

// update it
provider.GetTable<Customer>().Update(cust);
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

But it is not truly required to query the database before updating an entity. 
If I knew the key value up front, I could have created a new instance of `Customer` with 
the `CustomerID` value from a known row in the table.
To the provider, there is no difference between an instance returned from a prior query and one I simply conjured up on the spot.
All it cares about is the values it has in its members, not the object identity of the instance.

Entity tables do not behave like ordinary collection classes, they do not store the entities locally.
They are proxes to tables in a database, where the entities actually live as rows of column values.
Merely modifying a member on an entity class is not sufficient to change the data in the database. 
I have to call the `Update()` method on entity table, passing it the modified entity instance.

### Deleting

Likewise, with deleting, I probaly want to delete a `Customer` I get from the database via a query.

```CSharp
// get a customer from the database
var cust = provider.GetTable<Customer>().Single(c => c.CustomerID == "ALFKI");

// delete it
provider.GetTable<Customer>().Delete(cust);
```

The database command that is executed:

```SQL
DELETE FROM [Customers]
WHERE ([CustomerID] = p0)
```

But that is also not required in order to delete the entity from the database.
I can make a new instance and give it the `CustomerID` of a `Customer` I want to delete, and
use that instance when I call the `Delete` method.
As long as my `Customer` instance has the correct `CustomerID` the delete will execute just the same.

## Other Topics

### Transactions

IQToolkit query providers are built on top of ADO (System.Data) database providers.
So it is possible to control whether queries and updates are executed under a database transaction.

The provider has the settable property `Transaction` that you can assign a `DbTransaction` object that is associated
with the provider's `DbConnection`.

### Logs

IQToolkit query providers support a simple way to get feedback on the queries and commands being executed against the database.
This is a useful tool to help debug your application.

The provider has the settable property `Log` that can be assigned any `TextWriter` instance.
If a `TextWriter` is assigned, all queries and commands executed by the provider are also written into the log.

### Context classes

IQToolkit API's give my application the ability to easily access and manipulate every table in my database.

Yet, we all know that with great power comes great responsibility, 
and that might just be too much responsibilty for my application, 
or the poor souls that come along and have to support it when I'm gone.

I may want to limit what data my application can access by constraining the
scope to just a few entity collections, or I may just want to hide some of
the query provider API.

**Context classes** are classes that expose the collections I want exposed
by simply wrapping the query provider with a class I define.

For example, I can create a context class for the Northind database and only 
expose queryable properties corresponding to the entities I want accessed.

```CSharp
public class Northwind
{
    private readonly provider;

    public Northwind()
    {
        this.provider = new AccessQueryProvider("Northwind.mdb");
    }

    public IQueryable<Customer> Customers => this.provider.GetTable<Customer>();
    public IQueryable<Order> Orders => this.provider.GetTable<Order>();
}
```  

Now instead of writing:
```CSharp
var query = provider.GetTable<Customer>()
```

I can now write code referencing the contex class and its properites instead of the provider directly.

```CSharp
var db = new Northwind();

var query = from c in db.Customers 
            where c.ContactName.StartsWith("Maria") 
            select c;
```

## Advanced Topics

## Advanced Insert, Update and Delete

## Advanced Mapping

Sometimes the types I want represented in my source code do no match perfectly to the layout of the tables in my database.
If I have the luxury of changing the database tables, maybe that's the best plan, or if I have the opportunity to 
add views over the tables that better match my types then I should do that.
But if all else fails, and I'm not able or willing to adjust my types, then I might just need some advanced mapping. 

IQToolkit defines some additional kinds of mapping that help bridge some of the differences between types and tables.
However, it only provides a few general ones. Beyond what is described here, IQToolkit mappings are extensible.
So if I really need to do something special I can always roll up my sleeves and make more.

### Multi-Table Mapping

Sometimes the fields I want in my type correspond to columns split across multiple tables.
I am not always sure why the database is the way it is, but I am sure there are reasons,
probably something to do with normalization, ownership or legacy. 

Fortunately, there is a way to stitch multiple tables together as part of the mapping and use them together as one entity.

When you are declaring the mapping for an entity type you can add additional tables to the mapping by using the `ExtensionTable` attribute.

An extension table is any other table that uses the same key values as the primary table, where the relationship
between the two tables is one-to-one.  Maybe I have a `Customers` table with most of my customer data, and an 
additional `CustomersEx` table for seldom accessed or expensive to access data.

Bridging the two tables together via the `ExtensionTable` attribute is somewhat similar to declaring an association 
via the `Association` attribute, but I only have one entity type.

The Northwind sample database does not have examples of tables that would naturally be used like this.
But for the sake of an example, lets use customers and orders anyway, and pretend they have a one-to-one relationship.

So I define a new type `CustomerOrder` and give it some fields corresponding to columns from both.

```CSharp
    [Table(Name="Orders")]
    [ExtensionTable(Name="Customers", KeyColumns="CustomerID")]
    public class CustomerOrder
    {
        public string CustomerID;
        public int OrderID;

        [Column(TableId="Customers")]
        public string ContactName;
   }
```

Instead of describing the names of key members on the entity, like the `Association` attribute, 
the `ExtensionTable` attribute describes the relationship using the key column names from the two tables.
If the key columns from the two tables share the same names, I only need to specify them once.

Since I only have to specify attributes where I need to deviate from inferrence, 
I only need to specify a `Column` attribute for the `ContactName` column. 
It is the only one that is not from the primary (default) table.

*By default the table ID is the name of the corresponding table, unless it is specified otherwise
using the `TableId` property in the `Table` or `ExtensionTable` attribute, not shown here.*
 
Now I have an entity type that gets is data from rows in two tables.
It does this by joining the two tables together whenever I reference the `CustomerOrder` entity in a query.

For example, the basic get-everything query for the `CustomerOrder` type:

```CSharp
var list = provier.GetTable<CustomerOrder>().ToList();
```

Produces this query:

```SQL
SELECT t1.[ContactName], t0.[CustomerID], t0.[OrderID]
FROM [Orders] AS t0
LEFT OUTER JOIN [Customers] AS t1
  ON (t1.[CustomerID] = t0.[CustomerID])
```

*Notice, the query is a left-outer join, because it is possible for extension tables to not have entries for all primary table rows.**

This is great and just what I was looking for.
But if the true reason why my database data was split across tables was to keep seldom used or expensive data off in a side table,
then do I really want to join and return all the data all the time?

Fortunately, IQToolkit is smart enough to reduce a query and drop unnecessary joins.

So if my query only returned data from the `Orders` table:

```CSharp
var list = provider.GetTable<CustomerOrder>().Where(co => new {co.OrderID, co.CustomerID});
```

Then the join to the `Customers` table gets dropped, and is instead:

```SQL
SELECT t0.[OrderID], t0.[CustomerID]
FROM [Orders] AS t0
```

*It is also possible to insert, update and delete multi-table entities, 
but maybe just not with this hacky example.*


### Nested-Entity Mapping

Unlike with multi-table mapping, where the data I want to make available in my entity class is spread across multiple tables,
sometimes the tables have more data than I want to stuff into a single class.
Instead, I might want split up that data between multiple classes in my source code.

One way I might choose to do this is by having two separate entities defined for the same database table, each accessing different subsets of the columns. 
This might seem like a good idea, until it comes time to insert, update or delete an entity, and then it becomes difficult 
to discern which class in effect owns the row in the table. 
If I delete one entity from its table, would I expect the other one to be deleted as well? 

Probably not.

For this reason, I would not encourage having two entities mapped to the same table, 
unless you are unlikely ever to use them together in the same unit of work.
 
Instead, IQToolkit allows you to have multiple classes correspond to the same table, 
as long as one class is considered the entity that owns the row in the table
and all others are considered nested entities, just pieces of the overall entity spread out into separate classes.

For example, the `Customers` table has many columns that correspond to the address of the company or contact.
Instead of exposing all those properties as individual members on the `Customer` class, 
I can group them together into an `Address` class and  simply make the entire address available via an `Address` property.

```CSharp
[Table(Name="Customers")]
public class Customer
{
    public string CustomerID;
    public string ContactName;
    public string CompanyName;
    public string Phone;

    [NestedEntity]
    public Address Address;
}

public class Address
{
    [Column(Name="Address")]
    public string Street;
    public string City;
    public string Country;
    public string Region;
    public string PostalCode;
}
```

Now, when I retrieve `Customer` instances, the data comes back in the shape of this small hierarchy of classes,
instead of a single instance.

```CSharp
var customers = provider.GetTable<Customer>().ToList();
var address = customers[0].Address;
```

So regardless of how the entity classes are nested (or not), the query that is executed against the database is still the same.
```SQL
SELECT t0.[City], t0.[Country], t0.[PostalCode], t0.[Region], t0.[Address], t0.[CompanyName], 
       t0.[ContactName], t0.[CustomerID], t0.[Phone]
FROM [Customers] AS t0
```

### Mapping with Context Classes

One benefit of using a context class is now I have an additional choice in locations to place my mapping attributes. 
Instead of placing them directly on the entity class, I can now place them on the properties on the context class
that expose the entity collections.

```CSharp
public class Customer
{
    public string ID;
    public string Name;
    public string Phone;
}

public class Order
{
    public int ID;
    public string CustomerID;
}

public class Northwind
{
    private readonly provider;

    public Northwind()
    {
        this.provider = new AccessQueryProvider(
                            "Northwind.mdb", 
                            new AttributeMapping(typeof(Northwind)));
    }

    [Table(Name="Customers")]
    [Column(Member="ID", Name="CustomerID")]
    [Column(Member="Name", Name="ContactName")]
    public IQueryable<Customer> Customers => this.provider.GetTable<Customer>();

    [Table(Name="Orders")]
    [Column(Member="ID", Name="OrderID")]
    public IQueryable<Order> Orders => this.provider.GetTable<Order>();
}
```

The mapping attributes are placed on the `Customers` and `Orders` property.
I still only have to add attributes when necessary.
However, notice how the `Column` attributes now use the `Member` property to identity which member of the entity type each attribute is declared for.

I also had to tell the provider where to find the mapping attributes. 
To do this, I gave the provider an instance of the `AttributeMapping` class initialized with the type of my context class. 
Now the provider (via the mapping) knows to look for attributes on the context class as well.

*It is possible to use a mix of locations, some attributes defined on the entities and some defined on the context class,
but it is not possible to have the same member mapped more than once.*


### Mapping with XML Files

### Strict Mapping

### Runtime Entity Types

### Immutable Entity Classes

---------

## Advanced Policies

----------

## Sessions

IQToolkit providers make it simple to query databases in terms of code objects and LINQ
and they also make it easy to insert, update or delete database data using those same objects.
However, each operation executed via a provider is done without regard of any other.

For instance, if I query for the same entity twice, I will make two different database queries and get back two different instances,
and if my application makes changes to one or more entities' properties, I will have to keep track of that on my own
or figure it out later somehow so I know which entities to call the update API for.

This may be exactly what I want, and if so, then I am already happy. But if not, maybe I should consider using a session.

IQToolkit sessions are a nice simple tool that automates some of this drudgery for me.
They keep track of all entity objects returned from my queries and know when they have been changed.
I can make changes to multiple objects and when I am ready, I only need to make one API call to 
submit all the changes back to the database in one fell swoop.

In addition, because it has access to the full set of all changes,
a session is able to figure out if there are any dependency relationships between the changed entities
and automatically orders all commands sent to the database to keep me from violating those picky
primary key and foreign key constraints.

---

I can create a session on top of a provider I already have.

```CSharp
var session = new EntitySession(provider);
```

And now instead of making queries against a provider's table,
I can make queries against a session's table.

```CSharp
var query = from c in session.GetTable<Customer>()
            select c.ContactName;
```

Now when I query for a customer and make changes to its values, the session knows about it.

I can see what the session thinks by calling the `GetSubmitAction` method.

```CSharp
var cust = (from c in session.GetTable<Customer>
            where c.CustomerID == "ALFKI"
            select c).Single();

// SubmitAction.None
var before = session.GetTable<Customer>().GetSubmitAction(cust);

// make a change
cust.Phone = "XXX YYY ZZZZ";

// SubmitAction.Update
var after = session.GetTable<Customer>().GetSubmitAction(cust);
```

And when I decide I am ready to submit changes back to the database, that's just one simple call,
even if I have made changes to many different entities.

```CSharp
// everyone named Maria needs a new phone
var query = from c in session.GetTable<Customer>()
            where c.ContactName.StartsWith("Maria")
            select c;

foreach (var cust in query)
{
   cust.Phone = "XXX YYY ZZZZ";
}

// I guess I'm ready to go
session.SubmitChanges();
```

This time I made changes to two customers. 
The SQL commands to the database looked like this:

```SQL
UPDATE [Customers]
SET [City] = p1, [CompanyName] = p2, [ContactName] = p3, [Country] = p4, [Phone] = p5
WHERE ([CustomerID] = p0)

UPDATE [Customers]
SET [City] = p1, [CompanyName] = p2, [ContactName] = p3, [Country] = p4, [Phone] = p5
WHERE ([CustomerID] = p0)
```

### Inserting in a Session

Inserting a new entity works out almost the same as inserting an new entity instance using the `IEntityTable<T>.Insert` API.
The only different is the method is named `InsertOnSubmit` instead. The name is different, because the insertion does not happen when you call the method.
The insertion happens later when I call `IEntitySession.SubmitChanges`.  This is a good thing, because the session now is able to
order the insertion amongst the other changes I have made.

```CSharp
var cust = new Customer {
                CustomerID ="NEWGU",
                ContactName = "New Guy",
                CompanyName = "Some Company",
                City = "Some Place",
                Country = "Some Where",
                Phone = "(xxx) yyy zzzz" };

session.GetTable<Customer>().InsertOnSubmit(cust);

// make other changes here.

session.SubmitChanges();
```

### Deleting in a Session

Like inserting, deleting is similar to `IEntityTable<T>.Delete`, with the name changed to `DeleteOnSubmit`.

```CSharp
// get a customer from the database
var cust = session.GetTable<Customer>().Single(c => c.CustomerID == "ALFKI");

// queue up the deletion
session.GetTable<Customer>().DeleteOnSubmit(cust);

// make other changes

session.SubmitChanges();
```


