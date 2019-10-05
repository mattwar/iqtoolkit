# Building a LINQ IQueryable provider – Part XV (IQToolkit v0.15): Transactions, Sessions and Factories

Matt Warren - MSFT; June 16, 2009

---------------------------------

This is the fifteenth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you might try searching for the audio tapes on www.Bing.com.  That would be a lot easier than reading. You won't find any, but you'll feel better for having tried.


Complete list of posts in the Building an IQueryable Provider series


Getting this version of the toolkit together has taken a lot of sleepless nights.  Thank goodness for Netflix or I'd have had to spend those sleepless nights actually working on the Toolkit.


Okay, enough with the flavor text, let's get to the crunch.


What's inside:



More Providers - MySQL and SQLite join the previous MS only line up.


Transactions - Use ADO transactions to control the isolation of your queries & updates.


Entity Providers - The provider concept is expanded to include tables of entities


Entity Sessions - The session concept adds identity caching, change tracking and deferred updates via SubmitChanges


Provider Factory - Create providers on the fly w/o knowing anything more than the database name and mapping.


Madness



The full source code and redistributable DLL's can be found at:


http://www.codeplex.com/IQToolkit


More Providers


MySQL -- With the stewardship of MySQL in doubt after the purchase of Sun by Oracle, I was leery of taking on the challenge of making a MySQL provider for the toolkit. Yet, the benefits of doing so turned out to be very significant and not just for the MySQL users, as it forced me to challenge some of my very assumptions about SQL which in turn made some of my meager testing better, which in turn made the toolkit better as a whole. 

I was surprised when I went to download the free and open-source database product that I had to fill out a bunch of information on myself and wait for a formal approval (that took many days) before I was *allowed* to access the product. This seemed like an awful lot of hassle and odd for an open source project; way too corporate. Fortunately, I persisted and got the server up an running with no problem. Next I searched high and low for MySQL version of Northwind so I could have something to run the tests against. If only Bing were available then!  I had to settle for plain ol' Live Search. Fortunately I found what I was looking for, even if it didn't offer me any cash back.

MySQL seems to me what you get if you take a bunch of C programmers and tell them to make a SQL database product and yet offer no guidance. This is not criticism, just a bit of snarky wit over many of the underscore endowed API's that make up the product. Luckily, you'll not need to worry yourself about the manual dexterity needed to type such awkward function names, as you'll be using LINQ and the MySQL provider will do the work for you. On the positive side, the function library of MySQL is quite large and feature rich. I had no problem finding appropriate translations. MySQL even had many interesting features that I'd like to go back and refactor the toolkit to take advantage of.  Next release maybe.

There were a few problems I uncovered while trying to get MySQL to pass the test suites.  I took the opportunity to fill out the execution suite that has languished for a few releases now, so I could actually assert what the correct output ought to be for specific queries. In almost all the cases MySQL did the right thing (as far as I was expecting.)  In most of the cases that it did not appear to do the right thing, it often turned out to be bad assumptions on my part about collation ordering and so removing those assumptions from the tests fixed everything up. The remaining problem still boggles my mind.

In a test designed merely to prove that the translation of simple joins succeeded to generate the right query, the database did not return the correct number of rows. 

from c in db.Customers
from o in c.Orders
from d in o.Details
select d.ProductId

which executes a MySql query that looks like this:

SELECT d.ProductId
FROM [Customers] AS c
LEFT OUTER JOIN [Orders] AS o ON o.CustomerID = c.CustomerID
LEFT OUTER JOIN [Order_Details] AS d ON d.OrderID = o.OrderID

I expected to get a number of rows corresponding to the total number of order-details in the database.  But I didn't.  I got a whole lot less.  So I started experimenting with different forms of the query.  If I selected o.OrderID instead of d.ProductID, I got a different number from either of the other two. If I selected c.CustomerID, again an entirely different number of rows.  It was only when I chose to select the entire order-detail object did I get the number of rows I was expecting.

Something strange seems to be going on.  On nobody's definition of SQL should I be getting a different number of rows depending on the columns in the selection (except if that SELECT has a DISTINCT operator specified.)  It was as if all queries were getting a default DISTINCT operation. When I took a look a the results when I selected out only ProductID, sure enough, all I got back were distinct product-id's.  MySQL has an 'ALL' keyword that is the semantic opposite of DISTINCT, so I tried adding that to the query, but to no avail. 

I don't know if this is a problem in the query engine or the transport layer, or if the MySQL folks actually think it is appropriate behavior.  As far as I'm concerned it is catastrophically bad; E_FAIL. For this problem alone I would recommend not using MySQL. Still, it may not be as bad as it sounds. It is likely very rare that you'd ever actually write a query that was expected to retrieve duplicate rows of data. So it may not ever impact you at all. I do recommend testing all your queries (and your application) before you put it into production.


SQLite - This is an open-source database product that is similar in nature to MS Access and SQL Compact, since it runs in-proc and is not really a server. After wrangling with MySQL, I thought I knew all I needed to know about different forms of SQL, until Jonathan Peppers sent me his go at making a SQLite provider. ( See, I told everyone that I'd add new providers if they would send them to me.)  Of course, SQLite had a few problems of its own.

While MySQL has a huge library of function, SQLite has a tiny one; many .Net API's don't have supported translations.  (This is of the set I had translated for MS SQL.  To be truthful, MS Access falls short on some of these too.)  So queries using API's other than some simple string manipulation or equality testing probably won't work against SQLite. 

In addition one of SQLite's big drawbacks is its lack of a rich type system.  SQLite's developers claim this to be a feature, and it may afford great flexibility, but is often a death sentence for LINQ queries, which are all strongly typed.  SQLite does have types, but only a few of them, like number, text and binary.  Problems arise when you try to use DateTime's, as they are not really their own type in SQLite, but a text layout.  For example, if you were trying to find all orders for a customer that happen in January, you might write this query.

from o in db.Orders
where o.CustomerID == cust && o.OrderDate.Month = 1
select o

this would produce this query.

SELECT o.OrderID, o.OrderDate
FROM Orders AS o
WHERE o.CustomerID = @cust AND STRFTIME('%m', o.OrderDate) = 1

The date function to extract the month out of the OrderDate column is really a text formatting function that extracts the month portion of the date-time text layout, which in this case is the text '01', since the date-time format is always padded to a specific width.  When this is compared against the number (1), another subtle difference crops up. For all other SQL's I've come across, text is considered the weakest form of type, so when two types are compared for equality text is always converted to the other form.  Yet, in SQLite, the opposite it true (which truthfully is more like C# and Java).  So when the two types are compared they are found incompatible, because the number (1) is turned into the text '1' and that is not the same as the text '01'. Adding insult to injury, there appears to be no type conversion functions at all, so I can't even work around the problem by injecting a conversion. My ignorance of SQLite may just be showing here, or a lack of sufficient documentation.

So my recommendation if you are using SQLite, to stay away from most API functions in your where clauses and such. API calls in the projection are okay, because these get executed on the client.

Transactions


I'm surprised no one's called me on this before. The DbQueryProvider and its ilk have been suspiciously lacking in support for transactions. The providers work, LINQ queries are converted into ADO Commands and executed, yet those ADO Command objects are never assigned an ADO transaction, even if you started one explicitly. 

Of course, the official word from Microsoft is to stop using the ADO transactions altogether and instead use the System.Transactions.TransactionScope object, that is newer, better and enables automatic use of distributed transactions, etc, etc, etc. And if you did use TransactionScope, then the problem I'm referring to would not be a problem. SqlCommand object's would implicitly enlist in the transation without me having to specify anything. 

Unfortunately, TransactionScope is mired with many problems and is not supported by all ADO providers so ADO transactions are still a necessity.  You can now use ADO transactions with query providers in a manner similar to LINQ to SQL.

provider.Transaction = provider.Connection.BeginTransaction();

// use the provider here to execute queries and updates, etc.

provider.Transaction.Commit();
provider.Transaction = null;

The provider will use whatever transaction object you give it when it creates new ADO command objects.


Entity Providers


I decided to formalize the pairing of query providers with a Table object that enables updates and other facilities.  The definition of an entity provider is now defined by these three interfaces.


public interface IEntityProvider : IQueryProvider

{

    IEntityTable<T> GetTable<T>(string tableId);

    IEntityTable GetTable(Type type, string tableId);

}

public interface IEntityTable : IQueryable, IUpdatable

{

    new IEntityProvider Provider { get; }

    string TableId { get; }

    object GetById(object id);

    int Insert(object instance);

    int Update(object instance);

    int Delete(object instance);

    int InsertOrUpdate(object instance);

}


public interface IEntityTable<T> : IQueryable<T>, IEntityTable, IUpdatable<T>

{

    new T GetById(object id);

    int Insert(T instance);

    int Update(T instance);

    int Delete(T instance);

    int InsertOrUpdate(T instance);

}



You can now always get to a table directly from a provider.  The two concepts are coupled together.  An entity table also has explicit CRUD methods and implements IUpdatable, so no more separation between normal tables and updatable tables. In my mind this simplifies things quite a bit. 

Of course, this caused me to want to rename DbQueryProvider.  Ooops.  This will likely cause you some grief as any of your existing code that was using DbQueryProvider directly is now not going to compile.  The new name for this class is now DbEntityProvider. It might not matter so much now that there is a nifty IEntityProvider interface.


Entity Sessions


One thing missing from the Toolkit so far has been all of that context stuff that LINQ to SQL and LINQ to Entities have.  When you use LINQ to SQL you have a change tracking service that detects when your objects change, and sends the updates for you all at the same time when you call SubmitChanges. 

An entity session is all of this change-tracking, deferred updating stuff packaged up together. It is distinctly different from an entity provider in these ways, yet similar to one in many others.

An entity session is defined below:


public interface IEntitySession

{

    IEntityProvider Provider { get; }

    ISessionTable<T> GetTable<T>(string tableId);

    ISessionTable GetTable(Type elementType, string tableId);

    void SubmitChanges();

}

public interface ISessionTable : IQueryable

{

    IEntitySession Session { get; }

    IEntityTable ProviderTable { get; }

    object GetById(object id);

    void SetSubmitAction(object instance, SubmitAction action);

    SubmitAction GetSubmitAction(object instance);

}


public interface ISessionTable<T> : IQueryable<T>, ISessionTable

{

    new IEntityTable<T> ProviderTable { get; }

    new T GetById(object id);

    void SetSubmitAction(T instance, SubmitAction action);

    SubmitAction GetSubmitAction(T instance);

}


public enum SubmitAction

{

    None,

    Update,

    PossibleUpdate,

    Insert,

    InsertOrUpdate,

    Delete

}


public static class SessionTableExtensions

{

    public static void InsertOnSubmit<T>(this ISessionTable<T> table, T instance)

    {

        table.SetSubmitAction(instance, SubmitAction.Insert);

    }


    public static void InsertOnSubmit(this ISessionTable table, object instance)

    {

        table.SetSubmitAction(instance, SubmitAction.Insert);

    }


    public static void InsertOrUpdateOnSubmit<T>(this ISessionTable<T> table, T instance)

    {

        table.SetSubmitAction(instance, SubmitAction.InsertOrUpdate);

    }


    public static void InsertOrUpdateOnSubmit(this ISessionTable table, object instance)

    {

        table.SetSubmitAction(instance, SubmitAction.InsertOrUpdate);

    }


    public static void UpdateOnSubmit<T>(this ISessionTable<T> table, T instance)

    {

        table.SetSubmitAction(instance, SubmitAction.Update);

    }


    public static void UpdateOnSubmit(this ISessionTable table, object instance)

    {

        table.SetSubmitAction(instance, SubmitAction.Update);

    }


    public static void DeleteOnSubmit<T>(this ISessionTable<T> table, T instance)

    {

        table.SetSubmitAction(instance, SubmitAction.Delete);

    }


    public static void DeleteOnSubmit(this ISessionTable table, object instance)

    {

        table.SetSubmitAction(instance, SubmitAction.Delete);

    }

}



As you can see, an entity session has tables, just like a provider.  Yet, those tables are not directly updatable.  Instead you can assign entity instances submit actions. These are the actions that take place later when you call SubmitChanges.  There are a bunch of extension methods defined to add the LINQ to SQL like InsertOnSubmit() methods to the interface.  These simply call the SetSubmitAction() method for you.

Also note that a session is not a provider. It is a service used in conjunction with a provider. You can use multiple different sessions with the same provider instance.   

There is one current implementation of an entity session in the Toolkit called (you guessed it) DbEntitySession.  You create a DbEntitySession by giving it an existing DbEntityProvider. The DbEntitySession hooks the provider in such a way that it gets first crack at all materialized objects before they are returned to you. In this way, the DbEntitySession can employ an identity cache so queries that retrieve the same entity will always return the same entity instance, and it can start automatic change tracking on all entities returned.

You are also not locked into the session's behavior.  At any time you can interact with the underlying provider instead for retrieving entities without passing through the identity cache or being changed tracked.  You can even get to the provider's table directly off a session table.


Provider Factory


Now with so many providers and one single way to write queries you'd think it would be easy to switch between them.  In reality it is not. You have to pick the provider you want, reference its library (IQToolkit.Data.XXX), reference its corresponding ADO library (System.Data.XXX), create the ADO connection, the mapping object and construct the provider.

var connection = new SqlConnection("...");
var mapping = new AttributeMapping(typeof(Northwind));
var provider = new SqlProvider(connection, mapping, QueryPolicy.Default, null);
var db = new Northwind(provider);

You can hide this all inside your database context class (or whatever you want to call yours), so you only have to write it once, but then your context class is tied to a specific provider.  Instead, you could wrap this code up into a factory method of your own devising, but then calls to the factory would be spread throughout your codebase.  There no good way to defer all this work to some configuration setting. Until now.

Introducing the new factory methods built into DbEntityProvider.

public static DbEntityProvider FromApplicationSettings();


public static DbEntityProvider From(string filename, string mappingId);

public static DbEntityProvider From(string provider, string connectionString, string mappingId);



These methods allow you to get up and running with only knowing a few bits of information.  You don't have to hard link you application to any particular provider.

The FromApplicationSettings method creates you a new instance of a provider from information found in the config file.  It looks for the "Provider", "Connection" and "Mapping" properties in the configuration and feeds them to the other factories.  It is also possible to look this information up in web settings, but I have not formalized that one yet.

The provider argument is a string that refers to the name of an assembly that contains the query provider.  These are generally of the form IQToolkit.Data.XXX.  If that assembly is not loaded, it will be loaded dynamically. This assembly can be in the assembly cache or in the same directory as your app (or other places that the runtime might look.)  From this assembly it will look for a type in the same namespace (as the name of the assembly) that derives from DbEntityProvider.

The connectionString and filename arguments are really the same thing. You can specify either the name of a database file or a full ADO connection string. If a file is specified, a correct connection string is obtained by calling the static GetConnectionString(string) method on the provider. A provider may be inferred from the file extension of a database file if none is specified.

The mappingId can either refer to the name of a context class (like Northwind) that has mapping attributes on it or the name of an xml file. 

So now you can write code like this to get your provider.

var db = new Northwind(DbEntityProvider.From(somedbfile, somemapfile));

Or better yet, you can use the FromApplicationSettings() method in the constructor of your context and still be configurable at runtime.


Madness


Of course, it wouldn't be a new toolkit release without some additional crazy changes.  One significant change is namespaces again. This time its not going to conflict with your code too much.  Most of the classes that where in IQToolkit.Data have been demoted into the namespace IQToolkit.Data.Common.  This includes most all classes that are implementation detail or base classes.  Mapping attributes and the like are now in IQToolkit.Data.Mapping.  This makes the namespace clean and obvious when you start looking for things via intellisense. 

DbEntityProvider and DbEntitySession are the only classes sitting in IQToolkit.Data, as these are the ones you'll likely need to reference when writing code.  IEntityProvider and IEntitySession are in IQToolkit namespace, because they are not specific to ADO (System.Data classes).



I hope you find this version feature rich enough to either build application directly on top of it, or model your own provider or data layer by using these techniques.


Don't forget the audio tapes.

