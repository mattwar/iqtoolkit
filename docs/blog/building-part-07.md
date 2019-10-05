# LINQ: Building an IQueryable provider – Part VII: Join and SelectMany

Matt Warren - MSFT; September 4, 2007

---


This is the seventh in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you might want to rethink your place in the universe. ??
Complete list of posts in the Building an IQueryable Provider series 
There's been a few weeks hiatus since the last installment. I hope that most of you have used the free time to explore building your own providers. I keep hearing about other people's LINQ to watchamacallit projects and it is encouraging. Today I want to show you how I went about adding 'join' capability to my provider.  Finally, it will be able to do something more interesting than just select and where.


Implementing Join

There are actually a couple different ways to represent a join using LINQ.  In C# or VB if I use more than one 'from' clause I am performing a cross product, and if I match keys from one with keys from the other I am performing a join. 

var query = from c in db.Customers

            from o in db.Orders

            where c.CustomerID == o.CustomerID

            select new { c.ContactName, o.OrderDate };


Of course, there is also the 'join' clause that is an explicit join.

var query = from c in db.Customers

            join o in db.Orders on c.CustomerID equals o.CustomerID

            select new { c.ContactName, o.OrderDate };


Both of these queries produce the same results.  So why are there two ways to do the same thing?


The answer to that is a bit involved, but I'll try to make a stab at it here.  The explicit join requires me to specify two key expressions to match; in database parlance this is known as an equi-join. The nested from clauses allow more flexibility. The reason the explicit join is so restrictive is that by being restrictive the LINQ to Objects implementation can be efficient in execution without needing to analyze and rewrite the query. The good news here is that almost all joins used in database queries are equi-joins. 


Also, by not being as expressive the explicit join is much simpler to implement. In this post I'll actually implement both, but I'll start with the explicit join because it has fewer pitfalls.


The definition of the Queryable Join method looks like this:

public static IQueryable<TResult> Join<TOuter,TInner,TKey,TResult>(
    this IQueryable<TOuter> outer, 
    IEnumerable<TInner> inner, 
    Expression<Func<TOuter,TKey>> outerKeySelector, 
    Expression<Func<TInner,TKey>> innerKeySelector, 
    Expression<Func<TOuter,TInner,TResult>> resultSelector
    )

That's a lot of arguments and a lot of generics!  Fortunately, its not as hard to understand as it looks.  The 'inner' and 'outer' parameters are referring to input sequences (the sequences on both sides of the join); each have their own key selector (the expressions that appear in the 'on' clause on opposite sides of the 'equals'); and finally an expression that is used to produce a result of the join.  This last 'resultSelector' might be a bit confusing since it does not seem to appear in the C# or VB syntax.  In fact, it actually does.  In my example above it is the select expression.  In other examples, not shown here, it might be a compiler generated projection that carries the data forward to the next query operation.


Either way, its rather straight forward to implement. In fact, I already have almost everything I need to implement it. What I don't have yet is a node to represent the join. 


At least that's easy to remedy.

    internal enum DbExpressionType {

        Table = 1000, // make sure these don't overlap with ExpressionType

        Column,

        Select,

        Projection,

        Join

    }

I modified my enum to make a new 'Join' node type, and then I implement a JoinExpression.

    internal enum JoinType {

        CrossJoin,

        InnerJoin,

        CrossApply,

    }

    internal class JoinExpression : Expression {

        JoinType joinType;

        Expression left;

        Expression right;

        Expression condition;

        internal JoinExpression(Type type, JoinType joinType, Expression left, Expression right, Expression condition)

            : base((ExpressionType)DbExpressionType.Join, type) {

            this.joinType = joinType;

            this.left = left;

            this.right = right;

            this.condition = condition;

        }

        internal JoinType Join {

            get { return this.joinType; }

        }

        internal Expression Left {

            get { return this.left; }

        }

        internal Expression Right {

            get { return this.right; }

        }

        internal new Expression Condition {

            get { return this.condition; }

        }

    }


I've also defined a JoinType enum and filled it out with all the join types I'll be needing to know about.  'CrossApply' is a SQL Server only join type. Ignore it for now, I don't need it for the equi-join. In fact, I only need the 'InnerJoin'.  The other two come later.  I told you this was the simpler case.


What about outer joins? That will have to be a topic for another post. ??


Now, that I have a new JoinExpression I'll need to update my DbExpressionVisitor.

    internal class DbExpressionVisitor : ExpressionVisitor {

        protected override Expression Visit(Expression exp) {
            ...

            switch ((DbExpressionType)exp.NodeType) {
                ...

                case DbExpressionType.Join:

                    return this.VisitJoin((JoinExpression)exp);
                ...

            }

        }
        ...

        protected virtual Expression VisitJoin(JoinExpression join) {

            Expression left = this.Visit(join.Left);

            Expression right = this.Visit(join.Right);

            Expression condition = this.Visit(join.Condition);

            if (left != join.Left || right != join.Right || condition != join.Condition) {

                return new JoinExpression(join.Type, join.Join, left, right, condition);

            }

            return join;

        }

    }

So far so good.  Now, I just update QueryFormatter to know how to produce SQL text out of this new node.

    internal class QueryFormatter : DbExpressionVisitor {
        ...

        protected override Expression VisitSource(Expression source) {

            switch ((DbExpressionType)source.NodeType) {
                ...

                case DbExpressionType.Join:

                    this.VisitJoin((JoinExpression)source);

                    break;
                ...

            }
            ...

        }

        protected override Expression VisitJoin(JoinExpression join) {

            this.VisitSource(join.Left);

            this.AppendNewLine(Indentation.Same);

            switch (join.Join) {

                case JoinType.CrossJoin:

                    sb.Append("CROSS JOIN ");

                    break;

                case JoinType.InnerJoin:

                    sb.Append("INNER JOIN ");

                    break;

                case JoinType.CrossApply:

                    sb.Append("CROSS APPLY ");

                    break;

            }

            this.VisitSource(join.Right);

            if (join.Condition != null) {

                this.AppendNewLine(Indentation.Inner);

                sb.Append("ON ");

                this.Visit(join.Condition);

                this.AppendNewLine(Indentation.Outer);

            }

            return join;

        }

    }


The idea here is that JoinExpression nodes appear in the same place as other query source expressions such as SelectExpression and TableExpression.  Therefore, I've modified the VisitSource method to know about Joins, and I've added an implementation for VisitJoin.


Of couse, I'm not going to get anywhere if I don't know how to turn expression nodes calling the Queryable Join method into my new JoinExpression.  What I need is a method in QueryBinder just like the BindSelect and BindWhere methods.  This turns out to be the meat of the operation, however, it turns out to be rather straight-forward since I already have the support built in to handle the other operators.

    internal class QueryBinder : ExpressionVisitor {
        ...

        protected override Expression VisitMethodCall(MethodCallExpression m) {

            if (m.Method.DeclaringType == typeof(Queryable) ||

                m.Method.DeclaringType == typeof(Enumerable)) {

                switch (m.Method.Name) {
                    ...

                    case "Join":

                        return this.BindJoin(

                            m.Type, m.Arguments[0], m.Arguments[1],

                            (LambdaExpression)StripQuotes(m.Arguments[2]),

                            (LambdaExpression)StripQuotes(m.Arguments[3]),

                            (LambdaExpression)StripQuotes(m.Arguments[4])

                            );

                }

            }
            ...

        }
        ...

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector) {

            ProjectionExpression outerProjection = (ProjectionExpression)this.Visit(outerSource);

            ProjectionExpression innerProjection = (ProjectionExpression)this.Visit(innerSource);

            this.map[outerKey.Parameters[0]] = outerProjection.Projector;

            Expression outerKeyExpr = this.Visit(outerKey.Body);

            this.map[innerKey.Parameters[0]] = innerProjection.Projector;

            Expression innerKeyExpr = this.Visit(innerKey.Body);

            this.map[resultSelector.Parameters[0]] = outerProjection.Projector;

            this.map[resultSelector.Parameters[1]] = innerProjection.Projector;

            Expression resultExpr = this.Visit(resultSelector.Body);

            JoinExpression join = new JoinExpression(resultType, JoinType.InnerJoin, outerProjection.Source, innerProjection.Source, Expression.Equal(outerKeyExpr, innerKeyExpr));

            string alias = this.GetNextAlias();

            ProjectedColumns pc = this.ProjectColumns(resultExpr, alias, outerProjection.Source.Alias, innerProjection.Source.Alias);

            return new ProjectionExpression(

                new SelectExpression(resultType, alias, pc.Columns, join, null),

                pc.Projector

                );

        }

    }

Inside the BindJoin method It's almost like I'm handling two operators at once. I've got two sources, so I end up with two different source projections.  I use these projections to seed the global map that is used to translate parameter references and then I translate each of the key expressions.  Finally, the same goes for the result expression, except that the result expression can see both source projections instead of just one.


Once I have translated all the input expressions I have enough to represent the join, so I go ahead and construct the JoinExpression.  Then I use ProjectColumns to build me a column list for the new SelectExpression that I'm going to wrap around the whole thing.  Notice there is one small change in ProjectColumns.  It allows me to specify more than one pre-existing alias.  This is important, because with a Join there are actually two aliases that my result expression may be referring to.


That's it. I'm actually done. Join should work as advertised.


Let's try it out.

var query = from c in db.Customers

            where c.CustomerID == "ALFKI"

            join o in db.Orders on c.CustomerID equals o.CustomerID

            select new { c.ContactName, o.OrderDate };

Console.WriteLine(query);


foreach (var item in query) {

    Console.WriteLine(item);

}



Running this produces the following output:


SELECT t2.ContactName, t4.OrderDate
FROM (
  SELECT t1.CustomerID, t1.ContactName, t1.Phone, t1.City, t1.Country
  FROM (
    SELECT t0.CustomerID, t0.ContactName, t0.Phone, t0.City, t0.Country
    FROM Customers AS t0
  ) AS t1
  WHERE (t1.CustomerID = 'ALFKI')
) AS t2
INNER JOIN (
  SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
  FROM Orders AS t3
) AS t4
  ON (t2.CustomerID = t4.CustomerID)
{ ContactName = Maria Anders, OrderDate = 8/25/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 10/3/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 10/13/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 1/15/1998 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 3/16/1998 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 4/9/1998 12:00:00 AM }
 


Now for the hard stuff. ??


Implementing SelectMany  

If you've got any experience with SQL you are probably confounded by my insistence that nested 'from' case should be difficult. After all, with SQL this might only seem to be the difference between a CROSS JOIN and an INNER JOIN.  For those non-SQL inclined a CROSS JOIN is not really a join at all, it's a cross product. To make it a join, it relies on a join condition placed in the WHERE clause to do the actually joining.  So the only real difference, as far as SQL is concerned, is a CROSS JOIN puts is join condition in the WHERE clause and a INNER JOIN puts its join condition in the ON clause.  There shouldn't be so much fuss.


Oh, but there is.  There's a lot of fuss.  The problem isn't in the SQL, its more about what's not in the SQL.  You see a LINQ nested from is not the same thing as a CROSS JOIN.  Sometimes it's the same, but not always.


The problem comes down to what is visible when. A join takes two completely independent sub queries and joins them together using a join condition. The join condition can see columns from each side of the join, but that's the only expression that can. A LINQ nested from is very different. The inner source expression can see items enumerated from the outer source. Think of them as nested foreach loops. The inner one can reference the variable for the outer one.


The problem comes in finding a suitable translation for queries that are specified with these rogue references to the outer variable.


If your query looks like this then no problem:

var query = from c in db.Customers

            from o in db.Orders

            where c.CustomerID == o.CustomerID

            select new { c.ContactName, o.OrderDate };

This translates into method calls that look like this:

var query = db.Customers

              .SelectMany(c => db.Orders, (c, o) => new { c, o })

              .Where(x => x.c.CustomerID == x.o.CustomerID)

              .Select(x => new { x.c.ContactName, x.o.OrderDate });


The SelectMany's collection expression 'db.Orders' never references anything from 'c'. This is easy to translate to SQL since we can simply put db.Customers and db.Orders on opposite sides of a join.


However, if you simply change how you write the query to this:

var query = from c in db.Customers

            from o in db.Orders.Where(o => o.CustomerID == c.CustomerID)

            select new { c.ContactName, o.OrderDate };

Now, you've got a very different beast. Translated to method calls this becomes:

var query = db.Customers

              .SelectMany(

                 c => db.Orders.Where(o => c.CustomerID == o.CustomerID),

                 (c, o) => new { c.ContactName, o.OrderDate }

                 );

Now the join condition exists as part of the SelectMany's collection expression, so it references 'c'.  Translation can no longer simply be a process of putting both source expressions on either side of a join in SQL, whether CROSS or INNER.


So how am I going to solve this problem?  I'm not.  Not really.  I'm only going to solve it in the crudest sense.  I'm going to let Microsoft's SQL solve it for me, mostly.  Microsoft SQL2005 introduced a new type of join operator, CROSS APPLY, that has the exact same semantics as what I'm looking for, a very happy coincidence indeed. A CROSS APPLY's right-hand-side expression can reference columns from the left-hand-side.  That's why I included it when defining the JoinType enum.


A large part of LINQ to SQL translation engine exists to reduce CROSS APPLY's into CROSS JOIN in an opportunistic fashion.  Without this work, LINQ to SQL would likely not work very well with SQL2000, and even so it is not always possible.  Adding capability to do this to the sample provider would also be a lot of work that I'm reluctant to do right now. However, I'm not entirely unfeeling, so I've thrown in a bone.  I'm going to catch the easy case and convert that to a CROSS JOIN.


So let's take a look at the code.

    internal class QueryBinder : ExpressionVisitor {

        protected override Expression VisitMethodCall(MethodCallExpression m) {

            if (m.Method.DeclaringType == typeof(Queryable) ||

                m.Method.DeclaringType == typeof(Enumerable)) {

                switch (m.Method.Name) {
                    ...

                    case "SelectMany":

                        if (m.Arguments.Count == 2) {

                            return this.BindSelectMany(

                                m.Type, m.Arguments[0],

                                (LambdaExpression)StripQuotes(m.Arguments[1]),

                                null

                                );

                        }

                        else if (m.Arguments.Count == 3) {

                            return this.BindSelectMany(

                                m.Type, m.Arguments[0],

                                (LambdaExpression)StripQuotes(m.Arguments[1]),

                                (LambdaExpression)StripQuotes(m.Arguments[2])

                                );

                        }

                        break;
                    ...

                }

            }
            ...

        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector) {

            ProjectionExpression projection = (ProjectionExpression)this.Visit(source);

            this.map[collectionSelector.Parameters[0]] = projection.Projector;

            ProjectionExpression collectionProjection = (ProjectionExpression)this.Visit(collectionSelector.Body);

            JoinType joinType = IsTable(collectionSelector.Body) ? JoinType.CrossJoin : JoinType.CrossApply;

            JoinExpression join = new JoinExpression(resultType, joinType, projection.Source, collectionProjection.Source, null);

            string alias = this.GetNextAlias();

            ProjectedColumns pc;

            if (resultSelector == null) {

                pc = this.ProjectColumns(collectionProjection.Projector, alias, projection.Source.Alias, collectionProjection.Source.Alias);

            }

            else {

                this.map[resultSelector.Parameters[0]] = projection.Projector;

                this.map[resultSelector.Parameters[1]] = collectionProjection.Projector;

                Expression result = this.Visit(resultSelector.Body);

                pc = this.ProjectColumns(result, alias, projection.Source.Alias, collectionProjection.Source.Alias);

            }

            return new ProjectionExpression(

                new SelectExpression(resultType, alias, pc.Columns, join, null),

                pc.Projector

                );

        }


        private bool IsTable(Expression expression) {

            ConstantExpression c = expression as ConstantExpression;

            return c != null && IsTable(c.Value);

        }
        ...

    }



The first interesting thing to note is that there are two different forms of SelectMany that are interesting.  The first form takes a source expression and a collectionSelector expression. It produces a sequence of the same elements that are produced in the collectionSelector, only merging all the individual sequences together.  The second form adds the resultSelector expression that lets you project your own result out of the two joined items.  I've implemented BindSelectMany to work with or without the resultSelector being specified.


Note that on the fourth line of the function I determine which kind of Join I'm going to use to represent the SelectMany call.  If I can determine that the collectionSelector is just a simple table query then I know it cannot have references to outer query variable (the parameter to the collectionSelector lambda expression).  Therefore I know I can safely chose to use the CROSS JOIN instead of the CROSS APPLY. If I wanted to be a bit more sophisticated I could have written a visitor to prove that the collectionSelector made no references.  Maybe I'll do that next time. I have a feeling I'm going to need to know this for other reasons. Yet for now this is the simple test I'm going to use. 


All in all, this code is not too different from the BindJoin function or the others.  I do have to handle the case without the resultSelector.  In this case, I simply get to reuse the collectionProjection again as the final projection.


 


Let's try the new code too.

var query = from c in db.Customers

            where c.CustomerID == "ALFKI"

            from o in db.Orders

            where c.CustomerID == o.CustomerID

            select new { c.ContactName, o.OrderDate };

Console.WriteLine(query);


foreach (var item in query) {

    Console.WriteLine(item);

}



Running this code now produces the following results:


SELECT t6.ContactName, t6.OrderDate
FROM (
  SELECT t5.CustomerID, t5.ContactName, t5.Phone, t5.City, t5.Country, t5.OrderID, t5.CustomerID1, t5.OrderDate
  FROM (
    SELECT t2.CustomerID, t2.ContactName, t2.Phone, t2.City, t2.Country, t4.OrderID, t4.CustomerID AS CustomerID1, t4.OrderDate
    FROM (
      SELECT t1.CustomerID, t1.ContactName, t1.Phone, t1.City, t1.Country
      FROM (
        SELECT t0.CustomerID, t0.ContactName, t0.Phone, t0.City, t0.Country
        FROM Customers AS t0
      ) AS t1
      WHERE (t1.CustomerID = 'ALFKI')
    ) AS t2
    CROSS JOIN (
      SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
      FROM Orders AS t3
    ) AS t4
  ) AS t5
  WHERE (t5.CustomerID = t5.CustomerID1)
) AS t6

{ ContactName = Maria Anders, OrderDate = 8/25/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 10/3/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 10/13/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 1/15/1998 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 3/16/1998 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 4/9/1998 12:00:00 AM }


Yikes! The queries are starting too look a bit hefty.  I guess that's what happens when I keep mindlessly adding new select layers. Maybe one of these days I'll figure out a way to reduce this to just what is necessary. ??


Of course, If I write the query in such a way that it does not pass my simple check I'm going to get a CROSS APPLY.

var query = db.Customers

              .Where(c => c.CustomerID == "ALFKI")

              .SelectMany(

                 c => db.Orders.Where(o => c.CustomerID == o.CustomerID),

                 (c, o) => new { c.ContactName, o.OrderDate }

                 );

Console.WriteLine(query);


foreach (var item in query) {

    Console.WriteLine(item);

}



This code produces the following:


SELECT t2.ContactName, t5.OrderDate
FROM (
  SELECT t1.CustomerID, t1.ContactName, t1.Phone, t1.City, t1.Country
  FROM (
    SELECT t0.CustomerID, t0.ContactName, t0.Phone, t0.City, t0.Country
    FROM Customers AS t0
  ) AS t1
  WHERE (t1.CustomerID = 'ALFKI')
) AS t2
CROSS APPLY (
  SELECT t4.OrderID, t4.CustomerID, t4.OrderDate
  FROM (
    SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
    FROM Orders AS t3
  ) AS t4
  WHERE (t2.CustomerID = t4.CustomerID)
) AS t5

{ ContactName = Maria Anders, OrderDate = 8/25/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 10/3/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 10/13/1997 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 1/15/1998 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 3/16/1998 12:00:00 AM }
{ ContactName = Maria Anders, OrderDate = 4/9/1998 12:00:00 AM }
 


Exactly what I was expecting! 


Now my provider handles Join and SelectMany calls.  Do I hear a "woot woot" out there?  Maybe my ears are just ringing.  This provider does a lot, but there are still obvious holes to fill and operators to implement.  I should get paid for doing this.