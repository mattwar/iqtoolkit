# Building a LINQ IQueryable Provider – Part XVI (IQToolkit 0.16): Performance and Caching

Matt Warren - MSFT; September 15, 2009

--------------------------------------


This is the sixteenth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you might try rolling into a ball and crying for momma. That’s right, reading on is as hazardous to your health as a skinned knee. Just be warned and have an anti-biotic on hand.


Complete list of posts in the Building an IQueryable Provider series


I would have had the update out sooner but I couldn’t think of what to write in this excuse slot. My excuse generator was down for scheduled maintenance but did not come back on line as planned and wouldn’t you know it the techie responsible gave me all sorts of lame excuses. Some people!


What's inside:



Performance – I’ve actually tried to improve things.


Caching – Re-use queries without the burden of defining delegates.


Evaluation – Execute expression trees without Reflection.Emit.


Bug Fixes – Most of the bugs reported on CodePlex are fixed.



The full source code and redistributable DLL's can be found at:


http://www.codeplex.com/IQToolkit


Faster is Better

One of the big things I tried to tackle this time around was to finally do some performance improvements.  Until now, the only performance considerations were in the design of compiled queries and the use of compiled LINQ expressions for materializing objects.  Yet, when I looked at actual performance of compiled queries versus straight ADO, there was still a lot of overhead.


Where was the time being spent?  As it turns out, even though I was compiling a LINQ expression to represent the execution plan and an explicit function to convert DataReader rows into objects, ideally making the writing of data into objects as fast as possible, there was still room for improvement. The problem was not the writing of data into objects, but reading the data from the DataReader. I wholely blame myself for this. In an attempt to simplify the IQToolkit source code way back in one of the original posts I chose to read data using the general DataReader.GetValue method. This has two undesirable side effects; 1) the value is returned typed as object, which mean all non reference types (mostly numeric and datetime types) are boxed, which is measurably significant, and 2) in order to make sure the value was in the correct form to write it into the object I now had to coerce it back (which immediately led to an unbox operation, equally as significant.)


I tried many variations of solutions. Ideally, the runtime code would simply know which field access method of the DataReader to call and to coerce the results only if necessary. Unfortunately, the translation engine does not track enough information to be 100% certain of the data type the provider will return. It can make a good guess, but if it is wrong then the query will die in a fiery exception that halts the program and sends your manager storming toward your office. The solution I chose was sort of a hybrid.  Based on an expected value type a type specific helper method is now invoked. This method calls the equivalent DataReader method inside of a try-catch. I know, I hate having to use try-catch in this way, but the cheap cost of setting up the try-catch and the rare condition where the guess is wrong led me to the dark side. I will now change the color of my light-saber from blue to red.

Here’s an example of the GetInt32 helper method.

public static class FieldReader

{
  ...
  public static Int32 GetInt32(DbEntityProviderBase provider, DbDataReader reader, int ordinal)

  {

     if (reader.IsDBNull(ordinal))

     {

         return default(Int32);

     }

     try

     {

         return reader.GetInt32(ordinal);

     }

     catch

     {

         return (Int32)provider.Convert(reader.GetValue(ordinal), typeof(Int32));

     }

  }

  ...

}

As long as the expected type is correct, the faster DataReader.GetInt32 method is called. If that fails, then the fallback is to call the general GetValue method and coerce the result.  This should rarely happen.


This is all it took to get the compiled query into very low overhead versus direct ADO calls; mostly less than 3%. I’ve added a performance test you can run to check it out on your set up. Of course, this will vary depending on the query, the hardware, the load on the server and the network latency.


Caching Queries

“Why can’t the provider simply cache the queries for me.”  I’ve gotten this request a lot.  Sometimes from direct pleas in email, other times from those of you trying to do it yourself and asking for advice. 


It seems natural to imagine that it would only take a little bit of work for a provider to determine if a query being executed now is the same or similar to one executed before, so why is the only way to re-use a query to specify one using the cryptic syntax of generic methods, lambda expressions and delegates?  And why do I have to hold onto the thing myself, can’t some service do this for me?


Of course, my usual reaction is to give a heavy sigh in the privacy of my office and then craft a quite sensible fire-and-brimstone reply, complete with infallible logic and dramatic consternation, as to why and how you really are better off with the compiled query solution. But I’m tired of doing that, so instead of impressing you with my sound reasoning I’m going to show you how I went ahead and just did it. I like challenges, and there’s no better challenge than the impossible, or the sometimes impractical, or the generally ill-advised.


What I built is a new piece of code called the ‘QueryCache’.  It is actually implemented to be generic enough that it will work with your own IQueryable provider. Yet its not currently integrated into any provider, though you may choose to embed it into yours.  You can, however, use the cache as is to execute your queries and take advantage of its cache-y goodness. You don’t have to make delegates and invoke them, you simply have to give the cache your query and it will give you back the results.


var cache = new QueryCache(10);

var query = from c in db.Customers where c.City == "London" select c;

var result = cache.Execute(query);

Here’s how it works.  The cache maintains a most-recently-used list of compiled queries.  Every time you execute a query via the cache, the cache compares your query against the ones in the cache. If it finds a match, it simply uses the existing compiled query and invokes it. If not, it makes a new compiled query and adds it to the list.


Of course, that’s the easy part. 


The hard part is figuring out how to compare an IQueryable query object against a list of compiled-query delegate objects and determine which ones can be reused. For example, are these two the same query?


var query1 = from c in db.Customers where c.City == "London" select c;
var query2 = from c in db.Customers where c.City == "Seattle" select c;

Technically they are different expression trees, but if that’s the deciding factor then I might as well give up now. They are structurally similar and so it is logical to assume that a query compiled for one should be nearly identical to a query compiled for the other. If I were using compiled queries directly I would simply choose to make the name of the city a parameter to the compiled query delegate and invoke it with different values. So isn’t that just what I want the cache to do for me? 


To do this I need a little tool that will take a look at a query tree and decide which bits should be considered parameters and then give me back a new query tree with the parameters in place of those bits. As it turns out this is not really all that hard since it seems obvious for the most generality that anything that appears in the query tree as a constant should be deemed a parameter. A trivial expression visitor can produce this.


If it also wraps the rewritten query tree in a lambda expression and gives me back the set of constants that it extracted then I have everything I need to make a compiled query and invoke it with an array of values. I also have what I need to find an already compiled query in the cache, since compiled queries hold onto their defining query trees. So if my first query in the example above is already in the cache, it already has a parameterized query tree defining it and that ought to look awfully similar to the parameterized version of the second query.


The only thing I need now is a way to compare two lambda expressions to see if they are similar.  Fortunately, I wrote that ages ago.  The ExpressionComparer does just this. 


If I cobble all these parts together into a reasonable order I get my QueryCache. Now, I can use compiled queries without ever having to manually define them again!  Huzzah!


Yet, if this is so grand, why haven’t I taken that one last step and simply plugged into the base DbEntityProvider class?


Unfortunately, reality is not as rosy and I would hope.


There are a few problems holding me back.  The most significant is the silly act of parameterizing itself. The problem with the cache is that it doesn’t know which constants in the query should be made into parameters, so it turns them all into parameters. Yet, as much as I’d like it to be otherwise, databases don’t treat all parameters equally. Sometimes a parameter is terrific for performance; a database like SQL Server optimizes by storing parameterized query plans so you basically get the same benefit client and server.  Yet, sometimes, for some bits of SQL, for some back ends, parameters either cannot be used or have crippling effects on speed. So it's usually the best policy to be explicit and judicious when using parameters, something the cache in all its glory cannot chose for you.


So I leave the cache, for now, as something you can explicitly opt-in to using, per query.


Please, give it a try.  Feedback is welcome.


Expression Evaluator

Sometimes you may want to execute expression trees as code without actually turning it into true runtime code. You may be running on a platform that does not currently support the Expression.Compile() method (or the ability to use Reflection.Emit to generate IL at runtime) like Windows Mobile. I’ve been encouraged by folks here at Microsoft to think about this from time to time, so I set out to explore this space and the result is a whole mess code called the ExpressionEvaluator, which is an expression tree interpreter. I added my experimental solution to the toolkit in case it is beneficial for someone. It is a lot of code, so it doesn’t compile into the library by default. You have to enable it with a specific #define, NOREFEMIT.  This will also switch over all direct uses of Reflection.Emit in the toolkit to using this evaluator.


It has a hefty performance overhead, so I would not consider it a viable alternative to Reflection.Emit. I’ve even added many tricks to avoid calls to reflection and boxing. This has made it a lot faster than a naive solution, but still it pales in comparison to real IL.  However, in cases where there is no alternative, its probably the best you are going to get.


Crazy Antics

In keeping with the spirit of making changes for no good reason, I’ve unbundled the base set of toolkit code from the ADO provider specific bits. So now if you are using IQToolkit to build your own non-ADO based provider you don’t have to be dragging the rest of it along for the ride.  All the SQL translation and DbEntityProvider goodness is now in its own DLL, IQToolkit.Data.dll. This means you’ll probably have to tweak your projects to include this new DLL, but that’s about all.


The DLL list is now:



IQToolkit.dll
IQToolkit.Data.dll
IQToolkit.Data.Access.dll
IQToolkit.Data.SqlClient.dll
IQToolkit.Data.SqlServerCe.dll
IQToolkit.Data.MySql.dll
IQToolkit.Data.SQLite.dll


Of course, as always, you only need around the ones you are using, or none of them if you are simply lifting the code out as source.


That’s All Folks

I’m sure there are many more wonderful nuggets of goodness I added but forgot to mention.  If you discover any of them please file a full report at http://www.codeplex.com/iqtoolkit. 