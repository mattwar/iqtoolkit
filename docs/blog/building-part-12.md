# Building a LINQ IQueryable Provider – Part XII: Relationships and the IQToolkit

Matt Warren - MSFT; November 17, 2008

-------------------------------------

This is the twelfth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you probably were born yesterday. How could you possibly make sense of this post without any context at all?  At least make an attempt. Sometimes I don't know why I bother.


Complete list of posts in the Building an IQueryable Provider series 


<Insert standard disclaimer for why there have been no updates on this for 'like' forever>


It's been so long since I last posted we must be up to Web 5.0 by now. I suspect there are not any actual web developers left. Its all just twitter application programming and cybernetic mind melds now. Does anybody still write code? In a text based language? Without an electric shunt wired directly into your cerebral cortex?  Nobody?


Sigh. I'm probably just talking to an empty internet; everyone's moved on to Planck Space. But since I've got nothing better to do, I might as well get on with it.


IQueryable Toolkit

One of the first things you'll notice when you take a look at the source is that I changed it quite a bit.  I moved code around, changed names gratuitously, added & removed classes and broke a lot of continuity with the prior versions. One of the biggest changes is that the code is no longer just a sample.  All those internal classes are public, the project builds as a DLL, the tests are hosted separately and the namespace is no longer 'Sample.'  Why, oh why would I do this?  Simple.


I originally started the project to give developers a structured hands-on walk through of the construction of an IQueryable provider; hoping to inspire many of you to build your own. (Many of you have.) The source code was made available so you could learn from it and as a cheap way to get you to debug it for me. (Many of you haven't.) And so you could take from it what you would, copy a class here, steal a method there, etc, to make your job easier to get your own LINQ to XYZ up off the  ground quicker.


Yet, I received so many requests to re-use the entire code base wholesale, that I now realize what you really want is a toolkit not just some sample code. As a toolkit it is still sample code. The sources are still there and you can take from it what you will.  Or you can build it into a shiny DLL and then build your code on top of it.  All the pieces I've built so far are publicly re-usable. Many of the classes have been enhanced for extensibility, so you can mix & match and override to your hearts content to build the system you want out of the pieces I already have.


How it breaks down

At the top there is a namespace 'IQ'.  I thought that it was cleaver & consise. Maybe someone out there will come up with a better one, something catchy, like a cold, and I'll feel compelled to catch it, fall under-the-weather for a few weeks and emerge blissfully sedated enough to go with it. Maybe. Under the 'IQ' namespace exists all the bits and pieces that are generally useful for any IQueryable impementation.  This is where you'll find the basic ExpressionVisitor class, the generic Query and base QueryProvider class.  You'll also get these:



ExpressionComparer - compares two expression trees for equality. immensely useful.


ExpressionWriter - Get a better translation of that narly expression tree into a  C#-ish syntax that you can actually read.  Helps debugging a lot.


Grouping - It's that dead simple implementation of the LINQ IGrouping interface. 


CompoundKey - A class that helps you represent compound key values.


ScopedDictionary - 'cuz I needed it and now you can have it too.  Works sort of like a Dictionary but with nested scopes.


TypeHelper - Used to be called TypeSystem (how pretentious of me).  Helps you know a few things about types.


DeferredList - An implementation of IList that enables deferred loading.  (Bells are probably ringing in your head about now.)


Nested inside this namespace is another one, called simply 'Data'  (I know, original, huh). In here you'll find all those other classes that made up most of my previous posts like the provider itself, DbQueryProvider, that works on top of any DbConnection.  To really understand what has gone on in my head you need to get into this class and look around.


Logically, it still does the same thing.  It primarily just implements the IQueryable.Execute method, converting query expressions into SQL commands, executing them using the DbConnection and translating the results into objects. That's all still there, its just been re-arranged a bit and made a lot more extensible.


How it is extended is the interesting bit that flavors everything else to come. The DbQueryProvider's brains are now fully pluggable. I took a look at what was going on during execution and broke it down into logically atomic steps. The prior version used to first translate the query, build a generic reader and then execute and return.  That's still sort-of what happens, but there's an even better break-down.


Queries are translated, but the translation goes through three distinct phases: 



1) mapping is applied


2) policy is enforced


3) the target query language has the last say. 


Each of these steps is pluggable.  How?  There are three new classes to get acquainted to.



QueryMapping - Defines information & rules to map an object model onto a database model


QueryPolicy - Defines information & rules to determine inclusion of related data & execution plans


QueryLanguage - Defines information & rules to adhere to a target language, including converting the query into text


Every DbQueryProvider is now supplied these three at runtime, each can be overriden by you to take control at different points in then translation and execution process. The mapping, policy & language can each override a portion of the query translation pipeline, injecting its own rules as rewrites of the query expression using the common DbExpression primitives provided.


In addition, the QueryLanguage controls the final translation of the tree to SQL text (or whatever target language is being used), and the QueryPolicy is invoked to build the execution plan.  Note, the execution plan is not the simple object-reader of yore.  It is now an entire runtime generated program for completely executing the database query & constructing objects out of the results.  The QueryPolicy is allowed to do whatever final rewrite is necessary to turn the translated query expression into an executable piece of code.


Of course, by default, these three mostly do what was being done before, and if you care to look you'll see that the QueryLanguage's Format method (for turning queries into text) simply defers to the TSqlFormatter class (which used to be called QueryFormatter.)  You can overidde this method and have it call your own formatter.  You can even override the TSqlFormatter and make a few minor changes if your back end SQL is largely the same.


If you want to implement a different strategy to retrieving hierarchical results, you can inject your own logic both during query translation and you can take over the entire process of building the execution plan.  The default BuildExecutionPlan on the QueryPolicy class defers to the ExecutionBuilder (used to be ProjectionBuilder in its former life), but you can change that and do it your own way.  Of course, all the source is available, so you can build your version based off of the one that exists in the toolkit.


The crazy logic for inferring mapping information by simply using the names of types and members is retained, but walled off in a specific implementation of mapping called the ImplicitMapping.  You can re-use this one if your needs are as meager as my demos.  Or you can build your own.


Of course, now that you've had a moment to think about it, given so much flexibility, you'll inevitably start asking about what database providers are supported.  (Still just TSQL.)  You'll also want to know what complex mapping models I've invented and how they compare to ones used by ORM's.  (Still just the implicit demo one.)  And then you'll want to know what I did to improve reading data hierarchies because that N+1 strategy is just a loser.  (Here's where you finally get something out of me.)


Relationships and Loading

The query translation finally understands relationship properties. The QueryMapping understands the abstraction of mapping properties, these can map to columns or relationships (like associations) or whatever you want.  The mapping decides what's possible.  What have I implemented?  The basic mapping understands association relationships only; those kinds of relationships that are made via a join matching one or more columns across two tables.


The interesting part (at least to me) is how relationships are dealt with during query translation and later during execution.  Does a one to many relationship property residing in the output projection lead to extra queries at execution time or not?  If so, how many? 


I've implemented a few rewriters that deal with relationships.  For example, the SingletonProjectionRewriter finds projections of singleton relationships (one to one or many to one) and folds them into the query itself using a server-side join.  A singleton query doesn't change the cardinality of the results, so it is generally safe.  Yet, what happens when you refer to the same singleton relationship property more than once? Do you get the same join over and over again? To hold back the flood of redundant joins I had to come up with another rewriter, one that would find the duplicate uses and get them to place nice together, but because this situation can happen more often than just as the side effect of doing the singleton rewrites, I wrote it to work on joins and not just nested projections. The RedundantJoinRemover looks for identical joins expressed in the same query and throws out all the extra ones.  It does not look through into sub-queries, so its best to first removed redundant sub-query layers using the RedundantSubqueryRemover in hopes of forcing as many joins into the same layer.


The ClientJoinedProjectionRewriter attempts to do something very different. It looks to convert nested projections (ones that would become extra queries per object at execution time) into client-side joins. So you'll get one join per included relationship. This is not the "Big Join" strategy that LINQ to SQL uses. It precisely the strategy that I still consider inferior if I'm asked to ensure correctness of the results. So why did I choose to implement it? I'm not being asked to insure the correctness of results, nor am I being asked to ensure that large result sets can stream back from the server. I assume that the normal degree of ambiguity of results you get any time you attempt to get related results via more than one query is okay in this case too. I also assume that a user will choose to employ a transaction to get better consistency. You can also disable this one easily if you want. You can implement a policy that uses one or more other strategies.


This is what the ClientJoinedProjectionRewriter actually does.  If the SelectExpression of a nested projection is constrained to the outer query via an equality comparison, like this column equals that parameter and so on I figure I can easily turn this into a client-side LINQ to object style join.  So I rewrite the query so it restates all the significant bits of the queries that came before it.  So if you wanted to retrieve customers in London and all their orders you'll get one query for the customers in London and then another query for all the orders for all the customers in London. The idea is that the second query is actually run first and all the objects are stuffed into a lookup table. When the query for customers executes, it picks up the matching orders right out of the lookup table.  Neat.


Taking it for a Spin

Given a simple query for customers in London:

var query = from c in db.Customers

            where c.City == "London"

            select c;

And, assuming I have a policy that tells the provider that the Orders property on customers is supposed to be included whenever I ask for customers.  (The TestPolicy used by the supplied test project lets me pick and choose properties by name.)


When I start out, the provider first sees the query as LINQ expression tree:

Query(Test.Customer).Where(c => (c.City = "London"))


After mapping the query expression now looks like this: 

Project(

  @"SELECT t0.City, t0.ContactName, t0.Country, t0.CustomerID, t0.Phone

  FROM [Customers] AS t0

  WHERE (t0.City = 'London')",

  new Customer() {

    City = A0.Column("City"),

    ContactName = A0.Column("ContactName"),

    Country = A0.Column("Country"),

    CustomerID = A0.Column("CustomerID"),

    Phone = A0.Column("Phone")

  },

  p => Queryable.AsQueryable(p)

)

It's got the SelectExpression (formatted by default as TSQL), followed by an expression that constructs a customer out of the query's result columns, and function that converts the presumed resulting IEnumerable<Customer> into the expected type. 


Next the QueryPolicy is asked to translate, so it applies rules like determining if a relationship property is included in the output or not.  Since I set up the policy to automatically include orders, I get a new expression that looks like this:

Project(

  @"SELECT t0.City, t0.ContactName, t0.Country, t0.CustomerID, t0.Phone

  FROM [Customers] AS t0

  WHERE (t0.City = 'London')",

  new Customer() {

    City = A0.Column("City"),

    ContactName = A0.Column("ContactName"),

    Country = A0.Column("Country"),

    CustomerID = A0.Column("CustomerID"),

    Phone = A0.Column("Phone"),

    Orders = Project(

      @"SELECT t0.CustomerID, t0.OrderDate, t0.OrderID

      FROM [Orders] AS t0

      WHERE (t0.CustomerID = t2.CustomerID)",

      new Order() {

        CustomerID = A1.Column("CustomerID"),

        OrderDate = A1.Column("OrderDate"),

        OrderID = A1.Column("OrderID")

      },

      p => Enumerable.ToList(p)

    )

  },

  p => Queryable.AsQueryable(p)

)

Now I've got two queries, one nested inside the other.  If nothing happens to this, the inner query will get executed once per row in the outer query results.


Next, the QueryPolicy also applies the the ClientJoinedProjectionRewriter.  It attempts to convert nested projections into a query to fetch all the data (that will be executed once per outer query) with the resulting objects joined on the client machine.

Project(

  @"SELECT t0.City, t0.ContactName, t0.Country, t0.CustomerID, t0.Phone

  FROM [Customers] AS t0

  WHERE (t0.City = 'London')",

  new Customer() {

    City = A0.Column("City"),

    ContactName = A0.Column("ContactName"),

    Country = A0.Column("Country"),

    CustomerID = A0.Column("CustomerID"),

    Phone = A0.Column("Phone"),

    Orders = ClientJoin(

      OuterKey(A0.Column("CustomerID")),

      InnerKey(A1.Column("CustomerID")),

      Project(

        @"SELECT t0.CustomerID, t1.Test, t1.CustomerID AS CustomerID1, t1.OrderDate, t1.OrderID

        FROM [Customers] AS t0

        OUTER APPLY (

          SELECT t2.CustomerID, t2.OrderDate, t2.OrderID, 1 AS Test

          FROM [Orders] AS t2

          WHERE (t2.CustomerID = t0.CustomerID)

          ) AS t1

        WHERE (t0.City = 'London')",

        Outer(

          A1.Column("Test"),

          new Order() {

            CustomerID = A1.Column("CustomerID1"),

            OrderDate = A1.Column("OrderDate"),

            OrderID = A1.Column("OrderID")

          }

        ),

        p => Enumerable.ToList(p)

      )

    )

  },

  p => Queryable.AsQueryable(p)

)

Now I have an embedded ClientJoin node.  Notice how the inner query has a join so that it will retrieve all the orders for all the customers in London now.  Unfortunately, its an OUTER APPLY join when it does not really need to be.  But don't fear.  The QueryLanguage will soon fix it.  The combined query also gets a new column 'Test'. This is used to determine if the join found at least one matching row or not. If no match was found instead of the value 1 the 'Test' column will be null. I can use this information to make sure the resulting collection for a non-match is empty.


Next the QueryLanguage get its shot at translating the tree.  When it does, it attempts to get rid of any APPLY node if at all possible; OUTER APPLY's become LEFT OUTER JOIN's and CROSS APPLY's become INNER JOIN's.

Project(

  @"SELECT t0.City, t0.ContactName, t0.Country, t0.CustomerID, t0.Phone

  FROM [Customers] AS t0

  WHERE (t0.City = 'London')",

  new Customer() {

    City = A0.Column("City"),

    ContactName = A0.Column("ContactName"),

    Country = A0.Column("Country"),

    CustomerID = A0.Column("CustomerID"),

    Phone = A0.Column("Phone"),

    Orders = ClientJoin(

      OuterKey(A0.Column("CustomerID")),

      InnerKey(A1.Column("CustomerID")),

      Project(

        @"SELECT t0.CustomerID, t1.Test, t1.CustomerID AS CustomerID1, t1.OrderDate, t1.OrderID

        FROM [Customers] AS t0

        LEFT OUTER JOIN (

          SELECT t2.CustomerID, t2.OrderDate, t2.OrderID, 1 AS Test

          FROM [Orders] AS t2

          ) AS t1

          ON (t1.CustomerID = t0.CustomerID)

        WHERE (t0.City = 'London')",

        Outer(

          A1.Column("Test"),

          new Order() {

            CustomerID = A1.Column("CustomerID1"),

            OrderDate = A1.Column("OrderDate"),

            OrderID = A1.Column("OrderID")

          }

        ),

        p => Enumerable.ToList(p)

      )

    )

  },

  p => Queryable.AsQueryable(p)

)

Now the query expression is fully translated we only have left to build the execution plan.  The QueryPolicy is asked to turn the expression into a program that will do the actual execution.

Invoke(

  null,

  lookup1 => ((IQueryable<Customer>)ExecutionBuilder.Sequence(new Object[] {

    ExecutionBuilder.Assign(

      lookup1,

      Enumerable.ToLookup(

        Enumerable.Where(

          ((DbQueryProvider)Query(Test.Customer).Provider).Execute(

            new QueryCommand<KeyValuePair<String,Order>>(

              @"SELECT t0.CustomerID, t1.Test, t1.CustomerID AS CustomerID1, t1.OrderDate, t1.OrderID

              FROM [Customers] AS t0

              LEFT OUTER JOIN (

                SELECT t2.CustomerID, t2.OrderDate, t2.OrderID, 1 AS Test

                FROM [Orders] AS t2

                ) AS t1

                ON (t1.CustomerID = t0.CustomerID)

              WHERE (t0.City = @p0)",

              new String[] {"p0"},

              r1 => new KeyValuePair<String,Order>(

                r1.IsDBNull(0)

                  ? null

                  : ((String)Convert.ChangeType(

                    r1.GetValue(0),

                    System.String

                  )),

                r1.IsDBNull(1)

                  ? null

                  : new Order() {

                    CustomerID = r1.IsDBNull(2)

                      ? null

                      : ((String)Convert.ChangeType(

                        r1.GetValue(2),

                        System.String

                      )),

                    OrderDate = r1.IsDBNull(3)

                      ? new DataTime("1/1/0001 12:00:00 AM")

                      : ((DateTime)Convert.ChangeType(

                        r1.GetValue(3),

                        System.DateTime

                      )),

                    OrderID = r1.IsDBNull(4)

                      ? 0

                      : ((Int32)Convert.ChangeType(

                        r1.GetValue(4),

                        System.Int32

                      ))

                  }

              )

            ),

            new Object[] {((Object)"London")}

          ),

          kvp => kvp.Value != null

        ),

        kvp => kvp.Key,

        kvp => kvp.Value

      )

    ),

    Queryable.AsQueryable(((DbQueryProvider)Query(Test.Customer).Provider).Execute(

      new QueryCommand<Customer>(

        @"SELECT t0.City, t0.ContactName, t0.Country, t0.CustomerID, t0.Phone

        FROM [Customers] AS t0

        WHERE (t0.City = @p0)",

        new String[] {"p0"},

        r0 => new Customer() {

          City = r0.IsDBNull(0)

            ? null

            : ((String)Convert.ChangeType(

              r0.GetValue(0),

              System.String

            )),

          ContactName = r0.IsDBNull(1)

            ? null

            : ((String)Convert.ChangeType(

              r0.GetValue(1),

              System.String

            )),

          Country = r0.IsDBNull(2)

            ? null

            : ((String)Convert.ChangeType(

              r0.GetValue(2),

              System.String

            )),

          CustomerID = r0.IsDBNull(3)

            ? null

            : ((String)Convert.ChangeType(

              r0.GetValue(3),

              System.String

            )),

          Phone = r0.IsDBNull(4)

            ? null

            : ((String)Convert.ChangeType(

              r0.GetValue(4),

              System.String

            )),

          Orders = Enumerable.ToList(lookup1.get_Item(r0.IsDBNull(3)

            ? null

            : ((String)Convert.ChangeType(

              r0.GetValue(3),

              System.String

            ))))

        }

      ),

      new Object[] {((Object)"London")}

    ))

  }))

  )



Now we're cookin' with GAS!  This is the exact LINQ expression that can be run to execute the query and produce the results.  Notice how the code directly calls the DbQueryProvider.Execute method that takes the command text, parameters and a function that converts a DbDataReader into a sequence of objects.  That function is described inline with the rest of the LINQ expression.  Yes, you can do that.  That's how all those other LINQ operators work.


Also notice how I cheat with expression trees.  The process to create the lookup table requires variables, assignments and statement sequences; all things not currently possible with expression trees -- or so you thought.  The variable is really a parameter to a lambda expression.  If you directly invoke an inline lambda expression you basically create a variable that is in scope over the body of the lambda.  Next I call a method on ExecutionBuilder called Sequence.  This lets me simulate having statement sequences.  All the expressions are evaluated in order because the results are turned into an argument array.  The Sequence method simply returns the last value in the array.  The last cheat is the assignment.  While there is no assignment operator in the expression tree (at least not yet), it is possible to pass a parameter to a method as a by-ref argument.  Yes, this is ugly and causes side-effects, but that's exactly what I wanted.  (Until we get support for statements sometime in the future!)  The ExecutionBuilder Assign method take a ref argument and a value and simply assigns the value to the ref argument. 


When the query is finally executed, all the data is retrieved up front in only two queries.

SELECT t0.CustomerID, t1.Test, t1.CustomerID AS CustomerID1, t1.OrderDate, t1.OrderID

FROM [Customers] AS t0

LEFT OUTER JOIN (

  SELECT t2.CustomerID, t2.OrderDate, t2.OrderID, 1 AS Test

  FROM [Orders] AS t2

  ) AS t1

  ON (t1.CustomerID = t0.CustomerID)

WHERE (t0.City = @p0)

-- @p0 = [London]

SELECT t0.City, t0.ContactName, t0.Country, t0.CustomerID, t0.Phone

FROM [Customers] AS t0

WHERE (t0.City = @p0)

-- @p0 = [London]



 


Well that's it then.  Kudos, if you've read this far you really are trying to understand how to build your own IQueryable provider.  Either that or you are up late at night with insomnia trying to find something softer than a hammer to put yourself to sleep with.


Have fun with the new code!

