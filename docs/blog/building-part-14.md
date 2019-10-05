# Building a LINQ IQueryable provider – Part XIV: Mapping and Providers

Matt Warren - MSFT; April 8, 2009

---------------------------------

This is the fourteenth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you might request a weeks vacation, sit back, relax with a mochacino in one hand a netbook in the other, or if you've got better things to do with your time print them all out and stuff them under your pillow. Who knows, it might work better.


Complete list of posts in the Building an IQueryable Provider series


Okay, enough with all the post-is-late guilt! It's done now, so breathe a sigh of relief and get on with the reading.


What's inside:



More Mapping - Finally a real mapping system, with attributes and XML.


More Providers - MS Access and MS SQL Server Compact Edition


More POCO - Constructors, Enum and Interfaces.


More More More


The full source code can be found at:


http://www.codeplex.com/IQToolkit


More Mapping


Attribute Mapping - put attributes on the properties in the class that declares your tables.

This differs from LINQ to SQL mapping attributes which are placed on the entities themselves and is more like the proposed LINQ to Entity mapping attributes. However, I've not actually gone out of my way to make them the same. The advantages to this approach are 1) keeping the mapping separate from the entity objects (more POCO), and 2) being able to supply different mapping for the same entity type based on the table the entities are accessed from. 

Mapping attributes look like this:
[Table]

[Column(Member = "CustomerId", IsPrimaryKey = true)]

[Column(Member = "ContactName")]

[Column(Member = "CompanyName")]

[Column(Member = "Phone")]

[Column(Member = "City", DbType="NVARCHAR(20)")]

[Column(Member = "Country")]

[Association(Member = "Orders", KeyMembers = "CustomerID", RelatedEntityID = "Orders", RelatedKeyMembers = "CustomerID")]

public IUpdatableTable<Customer> Customers


You specify the Table, Column and Association attributes as necessary.  The 'Member' refers to the member in the entity type. If this is the same name as the database's column name you don't need to repeat it by specifying 'Name' too. 

You can specify nested mapping information by using a dot in the Member name. This allows you to have what some call value types, but to keep from clashing with .Net terminology I don't. For example, if you've defined an Address type that you want to use in a nested relationship (actually embedded in the same table row) you can do that like this:

[Table]

[Column(Member = "EmployeeID", IsPrimaryKey = true)]

[Column(Member = "LastName")]

[Column(Member = "FirstName")]

[Column(Member = "Title")]

[Column(Member = "Address.Street", Name = "Address")]

[Column(Member = "Address.City")]

[Column(Member = "Address.Region")]

[Column(Member = "Address.PostalCode")]

public IUpdatable<Employee> Employees


Xml Mapping -- this is same as attribute based mapping but data is read from an XML file. 

Xml mapping looks like this:
<?xml version="1.0" encoding="utf-8" ?>

<map>

  <Entity Id="Customers">

    <Table Name="Customers" />

    <Column Member = "CustomerId" IsPrimaryKey = "true" />

    <Column Member = "ContactName" />

    <Column Member = "CompanyName" />

    <Column Member = "Phone" />

    <Column Member = "City" DbType="NVARCHAR(20)" />

    <Column Member = "Country" />

    <Association Member = "Orders" KeyMembers = "CustomerID" RelatedEntityID = "Orders" RelatedKeyMembers = "CustomerID" />

  </Entity>

  <Entity Id="Orders">

    <Column Member = "OrderID" IsPrimaryKey = "true" IsGenerated = "true"/>

    <Column Member = "CustomerID" />

    <Column Member = "OrderDate" />

    <Association Member = "Customer" KeyMembers = "CustomerID" RelatedEntityID = "Customers" RelatedKeyMembers = "CustomerID" />

    <Association Member = "Details" KeyMembers = "OrderID" RelatedEntityID = "OrderDetails" RelatedKeyMembers = "OrderID" />

  </Entity>

  <Entity Id="OrderDetails">

    <Table Name="Order Details"/>

    <Column Member = "OrderID" IsPrimaryKey = "true" />

    <Column Member = "ProductID" IsPrimaryKey = "true" />

    <Association Member = "Product" KeyMembers = "ProductID" RelatedEntityID = "Products" RelatedKeyMembers = "ProductID" />

  </Entity>

</map>


You use it like this:


XmlMapping mapping = XmlMapping.FromXml(TSqlLanguage.Default, File.ReadAllText(@"northwind.xml"));

SqlQueryProvider provider = new SqlQueryProvider(connection, mapping);


Multi-table mapping -- Map multiple tables into a single entity.  If you've got entity data spread out over multiple tables with a 1:1 association between them you can now specify the additional tables in mapping using the ExtensionTable attribute or equivalent XML element. 

Here's what a multi-table mapping looks like:
[Table(Name = "TestTable1", Alias = "TT1")]

[ExtensionTable(Name = "TestTable2", Alias = "TT2", KeyColumns = "ID", RelatedAlias = "TT1", RelatedKeyColumns = "ID")]

[ExtensionTable(Name = "TestTable3", Alias = "TT3", KeyColumns = "ID", RelatedAlias = "TT1", RelatedKeyColumns = "ID")]

[Column(Member = "ID", Alias = "TT1", IsPrimaryKey = true, IsGenerated = true)]

[Column(Member = "Value1", Alias = "TT1")]

[Column(Member = "Value2", Alias = "TT2")]

[Column(Member = "Value3", Alias = "TT3")]

public IUpdatable<MultiTableEntity> MultiTableEntities


Extension tables are specified similar to how Associations are specified, except you are never referring to members, only column names.  You use the 'Alias' value to connect column & association mappings with columns from particular tables.  All queries for this multi-table entity treat the 'Table' as the primary table queried, all other tables are queried with left-outer joins.  All keys for associations must be from the same alias.

Can I mix nested mapping with multi-table mapping?  I have not tried it, but in theory it should work. It should not matter which table your nested entity gets it's data from, so in effect you can have a composition relationship between one table and another as long as it is 1:1.  



What about many-to-many?  Not yet. Making the system query a many-to-many relationship is relatively easy.  I haven't yet figured out the right semantics for inserts & updates. Right now, all insert, updates and deletes are explicit via calls to the IUpdatable with real-live entities. Yet how do you make an explicit update to the link table that you don't have an entity directly mapped to?  I need to ponder this some more.  Possibly if one side of the relationship is a composition as opposed to an association, then it would be implied when that side is updated.  Yet what if you chose not to load the relationship, how do you tell the system to not delete all previous relationships?


More Providers


MS Access -- This new query provider works with both Access 2000 - 2003 and Access 2007 data files. I don't know what the true differences are between the Jet and the Ace engines; the query language appears to be identical (as per my limited tests so far), yet the filename extension changed in Access 2007 to 'accdb' instead of 'mdb' and the northwind sample database plumped up an extra 66% in disk size without any additional data.


In order to make this work I've added an AccessLanguage object that is necessary to get the correct semantics for MS Access queries and an AccessFormatter object that handles generating the correct command text. In order to salvage as much as I could from the TSqlFormatter, I moved most of this code to a common SqlFormatter base class, and now the TSQL and Access formatters only supply the deviations from the standard syntax.  (Of course, 'standard' is currently whatever I deem it to be so don't go getting some actual online specification and prove me wrong.) Access only allows one command at a time, so that added an extra wrinkle, but in the end there is now support in the system for providers that can only do one command at a time. This means there are multiple round-trips to the engine for things like inserting a record and getting back the computed keys. Luckily, the access engine is in-proc so this is not really a burden. A new property on QueryLanguage, 'AllowMultipleCommands' determines how the execution plan is generated and whether multiple commands can be lumped together into a single ADO command.

The good news is that the access engine passes almost all the Northwind tests; some are not possible (mostly ones testing translation of framework methods that have no apparent equivalent in the access expression engine).  There were a lot of hairy strange & subtle differences in syntax between Access and TSQL, but most were handled by having different format rules, some required new expression visitors to change the query tree, like no explicit cross joins!  This caused me to write a visitor to attempt to get rid of cross joins (often injected by my visitor that tries to get rid of cross-apply joins) which is now generally useful to everyone, and if that didn't do it, another visitor that would attempt to isolate out the cross joins from any other joins and push them into sub-queries where Access lets me use the old-style comma-list, which is truly a cross join, though it just can't be mixed with other kinds of joins in the same from clause.



SQL Compact -- Yes, even more SQL Server.  Though to be truthful, SQL Server Compact Edition (aka SQL CE, aka SQL Compact, aka Skweelzy) is not really SQL Server, it is some other entirely different product that handles a subset of TSQL syntax, and is not a server at all since it runs in-proc just like MS Access.


What about MySQL or Oracle?
One day. The fact is that MS SQL and MS Access are easy for me to get to, they are already on my box. Getting something else up and running would take actual effort, and the MS secret database police might come get me. Meanwhile, if someone out there wants to put together a provider implementation I'll add it into the drop.

# Building a LINQ IQueryable provider – Part XIV

Matt Warren - MSFT April 8, 2009

Where did the SqlQueryProvider go?
I moved it. With the addition of the new providers it became apparent that I'd have to start factoring out all this ADO provider specific nonsense, otherwise all uses of the toolkit would have direct dependencies to way more than necessary. So I made separate projects, each building its own library. I may end up separating all the core 'data' stuff out into its own project too.

The solution builds these libraries now:


- IQToolkit.dll
- IQToolkit.Data.Access.dll
- IQToolkit.Data.SqlClient.dll
- IQToolkit.Data.SqlServerCe.dll


More POCO


Constructors -- Use entities that don't have default constructors.  It is now possible to have entities that require invocation of a constructor with parameters.  The binding process will figure out how to call your constructor and the client side code will call it for you as long as the constructor parameter names match property names. You can even have fully read-only entities if all data member are accounted for in the constructor.


Enums -- They actually sort of work now. You can have a member in your entity typed as some enum and you get automatic mapping between that enum and a numeric type in the database.


Interfaces and abstract base classes -- You can now declare you IQueryable's as IQueryable<SomeInterface> or IQueryable<SomeAbstractClass> and have the provider create instances of a compatible type under the covers, automatically translating all your references to interface or abstract class members to the appropriate mapped members. You can have mutiple entities share a common interface or base class and get different mapping for each. You can write code using generics and constrain generic parameters based on an interface and write queries that will get correct translation at runtime.  (Note, variation of mapping likely won't work with compiled queries, since the translation is fixed on the first execution.)

Less Policy


There's not a whole lot of policy being used right now and the policy objects dependence on the mapping object was no where near as deep as the mapping object's dependence on the language.  So policy is now independent of mapping, which means you can construct providers without specifying policy and/or reusing mapping with different policies.  Now if I could only make it simpler to specify/construct mapping without needing to know the language.  Back to the drawing board.


More Insanity


I apologize for the churn. The namespace changed so now all heck is going to break loose. Gone is the simple 'IQ' namespace and in its place is the 'IQToolkit' namespace.  I really did like the 'IQ' name, it was short, classy and made you feel intelligent just by looking at it.  Yet, it was hard to guess at if you did not already know what it was.  I chose to change the namespace name to match the product name and the DLL name. You add reference to the IQToolkit.dll and you import/use the IQToolkit namespace. No fuss, no muss.  Except for all those files you'll have to edit now. But hey, this is pre-pre-pre-pre beta stuff. Some people may think they are something special by snarkily keeping all their products in beta. They've got a lot to learn.


I hope this toolkit is becoming useful to many. I realize there have been a variety of requests for new things in the toolkit that I just have not gotten time to put in yet.  So you can expect plenty more in the future.


So enough with reading.  It's time to code!