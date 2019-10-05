# LINQ: Building an IQueryable Provider – Part XI: More of Everything

Matt Warren - MSFT; July 14, 2008

---------------------------------

This is the eleventh in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you’ll want to do so before proceeding, or at least before proceeding to copy the code into your own project and telling your boss you single-handedly solved the data layer problem over the weekend.


Complete list of posts in the Building an IQueryable Provider series 


No excuses for a late post this time. In my estimation, not only am I early but I have arrived with abundance. This is not my ordinary, keep it to the basics, slow and steady post about adding that next incremental feature to a feature-starved sample program. This is the Big Kahuna! This is the post where I finally go over the edge, lose my cool and let loose on the code. No more Mr. Nice guy.  Now, I’m really taking it to the cleaners. 


There’s so much goodness here I don’t even know where to start.  Even if I did, how could I possibly describe it all in one little post? I’m not even going to try. You’re going to have to download the source to get the real truth. A link is located at the bottom of the post. Click it. Feel the power flow over the internet, through the wires and onto your screen. Feel the radiance seep in through your eyes and take hold of your brain with the vice-like grip of a big-time wrestler.


So just what have I been cooking?  Let’s take a look at the highlights.


More Query Operators

That’s right; a lot more. Don’t believe me? Look at the code and see for yourself. Too busy? Boss breathing down your neck. Okay, here’s the list.


Distinct - Not only is there translation for this, but Distinct also makes an appearance inside most aggregates! AVG(DISTINCT t0.Price) anyone!


Skip & Take – Both work and work well together using the ROW_NUMBER & BETWEEN combo that gets incredible performance.


First, FirstOrDefault, Single & SingleOrDefault – No, these are not just the same thing. They really do behave differently.


Any, All & Contains - Not only do these guys work, but all three work with local collections. Use collection.Any(xxx) to get any predicate expression expanded into a series of OR conditions over the input set. 


Framework Methods and Properties


Hallelujahs, brothers! Now you can write queries that reference simple String, DateTime, Decimal, and Math operations, and get them translated into equivalent SQL.  I’ve not implemented every possible method but you’ll find a lot there. There are so many I can’t even list them all here. Rest assured you’ll get classics like string StartsWith, EndsWith and Contains and every variation of Equals and Compare that the framework can throw at me.


Parameters

That’s what I said, parameters. No more SQL injection attacks in the sample code, I’ve finally gotten around to adding parameter support. Take a look, it’s all rather straight forward; a new node type NamedValueExpression, a new visitor Parameterizer and a few extra lines of code to create and assign parameter values at execution time. Not every argument is parameterized either. Simple numeric values are left as literals.


Simpler Queries

The RedundantSubqueryRemover has been enhanced to know how to merge more types of SelectExpression’s together. Now, you’ll get even less layers of subqueries.


Compiled Queries

OMG! Can it be so? Yes, compiled queries too. How is it possible? It took a little refactoring of how queries are executed. There is now a new low-level Execute method on DbQueryProvider that takes a translated command, parameters and a function that converts a DbDataReader into a result (this could be sequence or a single value.)  Given that and LINQ Expression trees themselves, it is easy to see how I can turn a request to execute a LambdaExpression into constructing a function that when called, calls down to the low-level execute method with pre-translated SQL queries and a pre-translated object reader.


Unit Tests

Okay, get back up into your chair. I realized how easy it is to lose control and find the need to roll around the floor in ecstasy. However, your co-workers are starting to wonder if they should call for medical assistance. 


This does not mean I’m using TDD, or DDT or any TLA methodology; it just an apology really. The sample codebase has become so unwieldy that even I can’t trust myself anymore and have discovered so many prior features that I’ve broken in the process of doing bigger and better things that I literally broke down and wrote some simple unit tests. AND they are simple too. For the most part they prove 1) some LINQ operators are translated into the SQL I thought looked good at the time, and 2) the database server actually accepts these queries and does something with them.  There are over 200 of these tests now.  There is also a start at verifying actual semantics by looking at the results.  I started a second bank of tests for these and so far only have a few compiled-query tests listed.  More surely to come.


 


Whew!  I know that’s a lot to GROK, but take a look anyway.  If you’ve been building your own provider and have been stuck on how to do some of these kinds of features (except for tests, shame on you if you haven’t got your own tests yet), now you can get moving again.


But WAIT, there’s more!  I’m not done yet.  Even with all this I’ve shown you today, I still have a few more ideas.  More posts to come. 