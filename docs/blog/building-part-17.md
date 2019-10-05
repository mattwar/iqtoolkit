# Building a LINQ IQueryable Provider – Part XVII (IQToolkit 0.17): Automatic Caching and Loading Policies

Matt Warren - MSFT; February 9, 2010

------------------------------------

This is the seventeenth in a series of posts on how to build a LINQ IQueryable provider. If you thought this series would last this long then you must have some eerie metaphysical powers of insight that go beyond mere blind faith in the gods of the interwebs. So powerful, in fact, you may now be considered a threat to national security, since with all your witchery and kanoodling you might be secretly tapping the thoughts and provoking the actions of persons in higher office into doing unthinkable, irrevocable things. You may think you are doing good by protecting the innocent from master villains like Sylar, but the normals will still fear you anyway. You might want to keep a lookout for strange black sedans parked down the street from your home. I know I do.


Complete list of posts in the “Building an IQueryable Provider” series


I feel like I’m finally putting the finishing touches on this masterpiece. Sure, there’s always more to do, but the deliberate loose ends that I left in the product so you would have something to fret about are finally being seen to.


What's inside:



Cleaning House– I’ve thrown down the gauntlet on FieldReader. 


Automatic Caching – Entity providers get automatic query caching.


Automatic Loading – Finally a usable implementation of QueryPolicy.

Split Personalities – Language, Mapping and Policy each split into two classes.

Provider Abstraction – Build entity providers that use API’s other than ADO.


The full source code and redistributable DLL's can be found at:


http://www.codeplex.com/IQToolkit



Fielding a new Reader

I was not too happy with the state of the FieldReader class in the last installment. It was a bit quirky that it relied on exception handling to function whenever the entity member type did not match the database’s column type, at least in how the database provider chose to represent it.  I normally pride myself on sticking to my guns, and I’ve always told myself to never, ever, use exception handling for anything except for handling truly exceptional cases, and a mismatch like this was not really an exceptional case, since it happens quite a lot when the whole purpose of having a mapping system is to allow for just this sort of flexibility between the representation of objects in the programming language and that of the tables in the database.  And besides, it wasn’t fast enough.


What I was really trying to do was enable the object materialization code that is built by the ExecutionBuilder class to operate as fast as it could, without relying on a bunch of runtime checks and coercions to get the data into the right form.  I wanted to be able to determine at query build time the correct method to call on the DataReader without having to verify each time a field is read that its going to work (because most of the ADO providers just give up and throw exceptions when you ask for the data in the wrong form). I introduced the FieldReader class as a way to isolate the logic that would attempt to reduce this overhead (so I didn’t have to force all this logic in to pure expression node format).  Yet, the FieldReader itself still had the problem of trying to figure out if the call it was about to make against the DataReader was going to succeed or not.  So to avoid the call to GetFieldType() each time, I bailed out and just wrapped the whole thing in a try-catch, falling back to the expensive GetValue call on the DataReader whenever the provider balked at my tenacity.


Looking at the problem again, I realized that it wasn’t the call to GetFieldType() that was so expensive, it was calling it every time I read the field.  If I could simply call it once per query per field, then I could know the right course of action for each row without the cost, and without the exceptions.  What I needed to be able to do was to save state about the specific query execution, per data reader, per field. But alas, the FieldReader class was just a bunch of static methods.


I needed to build a better mouse trap.


Getting state to be stored per query is not too difficult, since I create a new instance of a materializer for each query, so there is at least something to stick data onto, even if it might be awkward to do it.  Yet, getting state per DataReader is much more difficult since there may be multiple DataReaders in a single query (think hierarchical results with preloaded associations.)  What I needed was an abstraction for reading fields that would be different and adjustable for each DataReader. Of course, that’s the point where the aha for the aha moment occurred, and I realized that what I really needed was to abstract the DataReader out of the problem. 


Introducing the new FieldReader.  Its now a DataReader, or at least it looks like one.  It’s the equivalent of a DataReader but defined by the entity provider, not the ADO provider.  It wraps a DataReader and holds onto whatever state it pleases.


So let’s take a look at how it gets to be fast.


The FieldReader is an abstract base, and it doesn’t know a thing about DataReader’s at all.  It exposes ReadXXXX methods to the materializer, and abstracts over a DataReader by declaring abstract methods that correspond to DataReader methods.

public abstract class FieldReader

{

    TypeCode[] typeCodes;

    public FieldReader()

    {

    }


    protected void Init()

    {

        this.typeCodes = new TypeCode[this.FieldCount];

    }


    public Byte ReadByte(int ordinal)

    {

        if (this.IsDBNull(ordinal))

        {

            return default(Byte);

        }

        while (true)

        {

            switch (typeCodes[ordinal])

            {

                case TypeCode.Empty:

                    typeCodes[ordinal] = GetTypeCode(ordinal);

                    continue;

                case TypeCode.Byte:

                    return this.GetByte(ordinal);

                case TypeCode.Int16:

                    return (Byte)this.GetInt16(ordinal);

                case TypeCode.Int32:

                    return (Byte)this.GetInt32(ordinal);

                case TypeCode.Int64:

                    return (Byte)this.GetInt64(ordinal);

                case TypeCode.Double:

                    return (Byte)this.GetDouble(ordinal);

                case TypeCode.Single:

                    return (Byte)this.GetSingle(ordinal);

                case TypeCode.Decimal:

                    return (Byte)this.GetDecimal(ordinal);

                default:

                    return this.GetValue<Byte>(ordinal);

            }

        }

    }


The ReadByte method shows an example of how this works.  The FieldReader holds onto an array of TypeCodes, one per field, and uses it to determine which underlying method to call on the DataReader.  It doesn’t actually call the DataReader directly, since the FieldReader class does not actually know about a DataReader, but it does have abstract methods that a provider can implement to communicate to its model of reading data.  The first time this method is called for a particular field ordinal, the TypeCode has a value of Empty.  If this is the case it simply computes the correct TypeCode and tries again.  A switch statement directs the code to the correct data reading method.


Now that I’ve done it, it seems almost obvious.  Getting the rest of the stack to talk about FieldReaders instead of DataReaders took some work, and lead to more fiddling and changes, but I’ll get to that later.


Automatic Caching


In the last version I introduce the QueryCache object and showed how you could use it to get many of the benefits of a pre-compiled query without actually pre-compiling it.  Yet, still, in order to use it you had to use it directly each time you wanted to execute a query. That was a bit awkward, but I was hesitant about going further at the time, worried that an automatic caching feature may have undesired effects for particular kinds of queries.  I speculated that it was not possible to determine precisely which sub expressions in a query ought to be handled as database parameters and which could be safely identified as constants (some queries cannot work with parameters in particular spots). However, an IQToolkit user called me out on this and showed me the light.  I was humbled.


Now you are receiving the benefit in a new feature.  The automatic query cache. 


Each EntityProvider (the new name for the provider base class, and yes it does change every version) now has a Cache property that can be assigned an instance of a QueryCache.  If the entity provider is given a cache it will automatically use that cache when executing queries. So now you can simply execute queries like normal, without pre-compiling, and if you execute what looks to be the same query again, it executes faster the second time.


How much faster is it really?

Unfortunately, it is still not as fast as having pre-compiled queries. Not by a long shot. The cost of actually looking in the cache and finding an equivalent already compiled query is still rather expensive. 


What is the expensive part?


The most expensive part of this re-use is not comparing expression trees, but isolating the parameter values. I have to break the query down into two parts, the common query base and the parameters specified in the new query. I then match the new query’s common base with the prior executed queries to find a previously compiled query to reuse, but in order to execute the previous query I need to supply the new parameter values and in order to do that I have to turn expression tree fragments into values.


I can evaluate LINQ expressions by turning each one into a zero-parameter lambda and using LINQ’s own feature of compiling lambdas into delegates that can then be invoked to get values. Yet that feature, like pre-compiled queries, works great if I am going to evaluate the same expression over and over again.  It’s a huge amount of overhead if I just want to evaluate the expression once.


The worse part of it all is realizing that was exactly what the partial evaluator was doing.  It was isolating sub expressions and evaluating them using the LINQ compile feature, and yet these compiled delegates were only ever being invoked once.


I seriously needed a way to evaluate LINQ expressions without using Reflection.Emit.  This is probably the point where YOU think “aha”, and predict I’m just being sneaky by leading up to discussing that very same expression evaluator that I included as source code in the last version.  But I’m not.  I’m not sneaky or even leading up to it.  Because that evaluator wasn’t any faster.


What’s a developer left to do? 


Handle the common cases as simply and straight-forward as possible and leave the ugly stuff to LINQ.  So, I’ve added a bit of *quick* direct reflection calls to the partial evaluator that gets used in the common cases of basic field accesses, which happens a lot in C# when you write queries that reference locals that get captured in state classes. 


It is really bizarre to discover that reflection API is the faster solution, after spending so much effort trying to work around it.


Automatic Loading


The only piece missing so far in making the toolkit a ready-to-use library for building apps on top of was its lack in any meaningful way of controlling deferred loading of associations.  I designed in the QueryPolicy abstraction as a way to guide the query translator and executor in doing the right thing, but the only way of actually setting up a policy was to invent your own implementation of one.  I figure about three of you actually did that.  The rest sent me mail saying that deferred loading did not work.


Fortunately, I’m here to tell you that has all changed. There’s a new class in town and its ready to be instantiated.


I give you the EntityPolicy.


It works similarly to LINQ to SQL’s DataLoadOptions. You construct an instance of one and then call some methods on it telling it how you would like each association to be loaded or not and whether to either defer its loading or load it up front.

var db = new Northwind(provider);

var policy = new EntityPolicy();

policy.IncludeWith<Customer>(c => c.Orders);

provider.Policy = policy;


var cust = db.Customers.Single(c => c.CustomerID == "ALFKI");


Use the IncludeWith() method to instruct the provider to include the Orders property when the Customer type is retrieved.  This will result in an query returns back all the data for customers and their related orders. 


The query looks like this when run against the Access test database:


PARAMETERS p0 NVarChar;
SELECT t1.[CustomerID], t1.[OrderDate], t1.[OrderID]
FROM [Customers] AS t0
LEFT OUTER JOIN [Orders] AS t1
  ON (t1.[CustomerID] = t0.[CustomerID])
WHERE (t0.[CustomerID] = p0)
-- p0 = [ALFKI]


PARAMETERS p0 NVarChar;
SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
FROM [Customers] AS t0
WHERE (t0.[CustomerID] = p0)
-- p0 = [ALFKI]



It is actually two queries; one for customers and one for orders.


If you want to retrieve order details too you’ll need to modify your policy like this:

var db = new Northwind(provider);

var policy = new EntityPolicy();

policy.IncludeWith<Customer>(c => c.Orders);

policy.IncludeWith<Order>(o => o.Details);

provider.Policy = policy;


var cust = db.Customers.Single(c => c.CustomerID == "ALFKI");


And the query will turn into three separate queries; one each for customers, orders and order-details:


PARAMETERS p0 NVarChar;
SELECT t2.[OrderID], t2.[ProductID]
FROM ([Customers] AS t0
LEFT OUTER JOIN [Orders] AS t1
  ON (t1.[CustomerID] = t0.[CustomerID]))
LEFT OUTER JOIN [Order Details] AS t2
  ON (t2.[OrderID] = t1.[OrderID])
WHERE (t0.[CustomerID] = p0)
-- p0 = [ALFKI]


PARAMETERS p0 NVarChar;
SELECT t1.[CustomerID], t1.[OrderDate], t1.[OrderID]
FROM [Customers] AS t0
LEFT OUTER JOIN [Orders] AS t1
  ON (t1.[CustomerID] = t0.[CustomerID])
WHERE (t0.[CustomerID] = p0)
-- p0 = [ALFKI]


PARAMETERS p0 NVarChar;
SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
FROM [Customers] AS t0
WHERE (t0.[CustomerID] = p0)
-- p0 = [ALFKI]


If you want to include Orders with Customers, but not up front, you can specify an option on the IncludeWith method to defer loading of this property.  This will only work if the association property has been declared as a DeferredList<Order> or equivalent type (like a simple IList<Order> that can be assigned a DeferredList.)  Otherwise, you’ll simply force the deferred query to be executed immediately upon assignment, which will be much less efficient than simply instructing the policy to include them without deferring.

var db = new Northwind(provider);

var policy = new EntityPolicy();

policy.IncludeWith<Customer>(c => c.Orders, true);

provider.Policy = policy;


var cust = db.Customers.Single(c => c.CustomerID == "ALFKI");


Now, when you execute the query you only see the first query for customers until you actually look inside the Orders collection.


Automatic Filtering and Ordering Associations


Filtering or ordering an association collection can be done when you include it using the IncludeWith method or separately by using the AssociateWith method.

var db = new Northwind(provider);

var policy = new EntityPolicy();

policy.IncludeWith<Customer>(c => from o in c.Orders where o.OrderDate.Month == 1 select o);

provider.Policy = policy;


var cust = db.Customers.Single(c => c.CustomerID == "ALFKI");


This is logically the same as using IncludeWith and AssociateWith separately:

var db = new Northwind(provider);

var policy = new EntityPolicy();

policy.IncludeWith<Customer>(c => c.Orders);

policy.AssociateWith<Customer>(c => from o in c.Orders where o.OrderDate.Month == 1 select o);

provider.Policy = policy;


var cust = db.Customers.Single(c => c.CustomerID == "ALFKI");


Both produce the same queries:


PARAMETERS p0 NVarChar;
SELECT t1.[CustomerID], t1.[OrderDate], t1.[OrderID]
FROM [Customers] AS t0
LEFT OUTER JOIN [Orders] AS t1
  ON ((t1.[CustomerID] = t0.[CustomerID]) AND (Month(t1.[OrderDate]) = 1))
WHERE (t0.[CustomerID] = p0)
-- p0 = [ALFKI]


PARAMETERS p0 NVarChar;
SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
FROM [Customers] AS t0
WHERE (t0.[CustomerID] = p0)
-- p0 = [ALFKI]


It is also perfectly legal to specify the AssociateWith before the IncludeWith, or even without one.  An AssociateWith by itself will still cause filtering to occur when the association property is used as part of the query.


Automatic Filtering Entities


It is also possible to specify filter operations that apply to entities anytime they are referenced, not just via an association.  You can use the Apply() method to apply any query operation over an entity table any time it is referenced in the query.


For example, image you wanted to limit the customer list to only customers in a particular city, and any query over customers would automatically get this restriction.

var db = new Northwind(provider);

var policy = new EntityPolicy();

policy.Apply<Customer>(custs => from c in custs where c.City == "London" select c);

provider.Policy = policy;


var custCount = db.Customers.Count();


So even when the Count() aggregate is used, the query includes this filter.


PARAMETERS p0 NVarChar;
SELECT COUNT(*)
FROM [Customers] AS t0
WHERE (t0.[City] = p0)
-- p0 = [London]


Of course, you can use this feature for more than just filtering, as long as the query returns the same types of objects that originated in the original table.


Split Personalities


If you dig a little beneath the surface of the new sources, or if you really are building your own providers based on the toolkit, you’ll notice a big change that has occurred with the definition of the language, mapping and policy objects.  Each of them has been split into two.  The intention is to separate the models from the behaviors.  In their prior incarnations, each of these classes both specified an API that exposed information about each area and at the same time had methods that directly took responsibility over a stage of query translation. 


So with a QueryLanguage you get a QueryLinguist that applies language specific transformations to queries; with QueryMapping you get a QueryMapper that applies the mapping and with QueryPolicy you get the QueryPolice, that enforces the rules about associations and inclusion.  (And as a benefit you can with a straight face, tell users that yes there are query police and they’ll come get if you continue to writer queries like that.)


Separating these out had the side effect of solving some awkward problems in the design.  For example, the necessity to have a fully specified QueryLanguage in existence before a QueryMapping can be created, since the translation steps of the mapper sometimes depend on the model of the language, yet a correct QueryLanguage model must be chosen  by the provider, which in turn depends on the mapping.  So there was an almost cyclic dependency that required the user to know more than would otherwise be necessary about the provider before constructing one.


Separating out the behaviors allow each language, mapping and policy to be specified separate of one another, enabling the provider to choose the language directly.


Provider Abstraction


It always bothered me that the backend models for these IQToolkit providers where directly tied to System.Data.  It made a sort of sense, because most existing data access API’s on .Net are System.Data providers, so this dependency works for most uses.  However, there is so much other code in the toolkit that could be applied to build query providers on top of other API’s that it was unfortunately that the ADO types, connection, transaction, etc, were tied in at such a low level.


So keeping in the spirit of always changing everything, I’ve once again refactored the base classes for entity providers. 


So EntityProvider is the new abstract base class for providers that brings in the model of query translations, languages, mapping and policies, but does not depend on ADO.  DbEntityProvider then derives from this and introduces connections and transactions.  The existing query providers for TSQL, Access, SQL compact, MySql and SqlLite are all based off the  DbEntityProvider class. Yet, it is now possible to build a provider that is based on another API and yet still make use of the rest of the infrastructure.


EntitySession is the class to use for sessions.  It is not an abstract base, but it no longer depends on ADO types and functions entirely in conjunction with an EntityProvider.  So it’s possible to use sessions with new style providers.


Even QueryTypeSystem has been simplified to not reference System.Data types, and a new DbTypeSystem class has been introduced to add knowledge of data types like DbType and SqlType.