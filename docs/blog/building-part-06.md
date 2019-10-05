#LINQ: Building an IQueryable Provider – Part VI: Nested queries

Matt Warren - MSFT; August 9, 2007

---

This is the sixth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you might want to file for a government grant and take sabbatical because you've got a lot of catching up to do. ??


Complete list of posts in the Building an IQueryable Provider series 


So, again you thought I was done with this series, that I've given up and moved on to greener pastures. You think that since Select works wonderfully that that's all you need to know to make your own IQueryable provider? Ha! There's loads more to know. And, by the way, Select is still broken.


 


Finishing Select

Broken? How is this possible? I thought you were this bigshot Microsoft uber developer that never made mistakes! Yet, you claim you gave us shoddy code? I cut and pasted that stuff already into my product and my boss says we 'go live' next Monday! How could you do this to me?  Sniff.


Relax. It's not that broken. It's just a little bit broken.


Recall in the last post I invented these new expression nodes; Table, Column, Select and Projection. They worked great didn't they? Sure they did! The part that is broken is that I did not handle all the cases where they might appear. The case I handled was the most obvious, when Projection nodes appear on the top of the query tree.  After all, since I'm only allowing for Select and Where anyway, the last operation must be one of those. The code actually assumed that to be true.


That's not the problem.


The problem is Projection nodes may also appear inside selector expressions themselves.  For example, take a look at the following.


    var query = from c in db.Customers


                select new {


                    Name = c.ContactName,


                    Orders = from o in db.Orders


                             where o.CustomerID == c.CustomerID


                             select o


                };


I added a nested query in the select expression. This is very different than the queries I wrote before, which were only tabular. Now, I'm basically asking my provider to construct a hierarchy, each object is going to have a name and a collection of orders. How in the world am I going to do that? SQL can't even do that. And even if I wanted to disallow it outright, what happens if someone does write this?


Bang! I get an exception. Not the one I thought though. I guess the code is more buggy than I realized. I expected to get an exception when trying to compile the projector function, since this lovely query should leave a little ProjectionException fly in my selection expression soup. Didn't I claim that it was okay to invent my own expression nodes because no one was going to see them anyway?  Ha! Looks like I was wrong. (The actual exception I get is from assembling a bad expression tree because I was mistyping the Projection nodes when I constructed them. That's going to have to be fixed.)


Assuming I fix the typing bug, what am I going to do about these nested projection nodes?  I could just catch them and throw my own exception with an apologetic disclaimer that nesting is a no-no. But then I wouldn't be a good LINQ citizen, and I wouldn't have the fun of figuring out how to actually make it work.


So on to the good stuff.


 


Nested Queries

What I want to do when I see a nested ProjectionExpression is turn it into a nested query. SQL cannot actually do this, so I'm going to have to do it or something like it in my own code. However, I'm not going to shoot for a super advanced solution here, just one that actually retrieves the data.


Since the projector function has to be turned into executable code, I'm going to have to swap some piece of code into the expression where this ProjectionExpression node currently is.  It's got to be something that constructs the Orders collection out of something. It can't come from the current DataReader since that guy only holds tabular results. Therefore its got to come from another DataReader. What I really need is something that turns a ProjectionExpression into a function that when executed returns this collection.


Now where have I seen something like that before?


Thinking...


Right. That's what my provider already does, more or less. Whew, I thought this was going to be difficult.  My provider already converts an expression tree into a result sequence via the Execute method.  I guess I'm already half way home!


So what I need to do is add a function to my good ol' ProjectionRow class that executes a nested query. It can figure out how to get back to the provider for me in order to do the actual work.


Here's the new code for ProjectionRow and ProjectionBuilder.

    public abstract class ProjectionRow {

        public abstract object GetValue(int index);

        public abstract IEnumerable<E> ExecuteSubQuery<E>(LambdaExpression query);

    }

    internal class ProjectionBuilder : DbExpressionVisitor {

        ParameterExpression row;

        string rowAlias;

        static MethodInfo miGetValue;

        static MethodInfo miExecuteSubQuery;


        internal ProjectionBuilder() {

            if (miGetValue == null) {

                miGetValue = typeof(ProjectionRow).GetMethod("GetValue");

                miExecuteSubQuery = typeof(ProjectionRow).GetMethod("ExecuteSubQuery");

            }

        }


        internal LambdaExpression Build(Expression expression, string alias) {

            this.row = Expression.Parameter(typeof(ProjectionRow), "row");

            this.rowAlias = alias;

            Expression body = this.Visit(expression);

            return Expression.Lambda(body, this.row);

        }


        protected override Expression VisitColumn(ColumnExpression column) {

            if (column.Alias == this.rowAlias) {

                return Expression.Convert(Expression.Call(this.row, miGetValue, Expression.Constant(column.Ordinal)), column.Type);

            }

            return column;

        }


        protected override Expression VisitProjection(ProjectionExpression proj) {

            LambdaExpression subQuery = Expression.Lambda(base.VisitProjection(proj), this.row);

            Type elementType = TypeSystem.GetElementType(subQuery.Body.Type);

            MethodInfo mi = miExecuteSubQuery.MakeGenericMethod(elementType);

            return Expression.Convert(

                Expression.Call(this.row, mi, Expression.Constant(subQuery)),

                proj.Type

                );

        }

    }



So, just like I inject code to call GetValue when I see a ColumnExpression, I'm going to inject code to call ExecuteSubQuery when I see a ProjectionExpression.


I decided I needed to bundle up the projection and the parameter I was using to refer to my ProjectionRow, because as it turns out the ProjectionExpression also gets its ColumnExpressions converted.  Luckily, there was already a class designed to do that, LambdaExpression, so I used it as the argument type for ExecuteSubQuery.


Also notice how I pass the subquery as a ConstantExpression.  This is how I trick the Expression.Compile feature into not noticing that I've invented new nodes. Fortunately, I didn't want them to be compiled just yet anyway.


Next to take a look at is the changed ProjectionReader. Of course, the Enumerator now implements ExecuteSubQuery for me.

    internal class ProjectionReader<T> : IEnumerable<T>, IEnumerable {

        Enumerator enumerator;

        internal ProjectionReader(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider) {

            this.enumerator = new Enumerator(reader, projector, provider);

        }


        public IEnumerator<T> GetEnumerator() {

            Enumerator e = this.enumerator;

            if (e == null) {

                throw new InvalidOperationException("Cannot enumerate more than once");

            }

            this.enumerator = null;

            return e;

        }


        IEnumerator IEnumerable.GetEnumerator() {

            return this.GetEnumerator();

        }


        class Enumerator : ProjectionRow, IEnumerator<T>, IEnumerator, IDisposable {

            DbDataReader reader;

            T current;

            Func<ProjectionRow, T> projector;

            IQueryProvider provider;


            internal Enumerator(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider) {

                this.reader = reader;

                this.projector = projector;

                this.provider = provider;

            }


            public override object GetValue(int index) {

                if (index >= 0) {

                    if (this.reader.IsDBNull(index)) {

                        return null;

                    }

                    else {

                        return this.reader.GetValue(index);

                    }

                }

                throw new IndexOutOfRangeException();

            }


            public override IEnumerable<E> ExecuteSubQuery<E>(LambdaExpression query) {

                ProjectionExpression projection = (ProjectionExpression) new Replacer().Replace(query.Body, query.Parameters[0], Expression.Constant(this));

                projection = (ProjectionExpression) Evaluator.PartialEval(projection, CanEvaluateLocally);

                IEnumerable<E> result = (IEnumerable<E>)this.provider.Execute(projection);

                List<E> list = new List<E>(result);

                if (typeof(IQueryable<E>).IsAssignableFrom(query.Body.Type)) {

                    return list.AsQueryable();

                }

                return list;

            }


            private static bool CanEvaluateLocally(Expression expression) {

                if (expression.NodeType == ExpressionType.Parameter ||

                    expression.NodeType.IsDbExpression()) {

                    return false;

                }

                return true;

            }


            public T Current {

                get { return this.current; }

            }


            object IEnumerator.Current {

                get { return this.current; }

            }


            public bool MoveNext() {

                if (this.reader.Read()) {

                    this.current = this.projector(this);

                    return true;

                }

                return false;

            }


            public void Reset() {

            }


            public void Dispose() {

                this.reader.Dispose();

            }

        }

    }



Now, you can see that when I construct the ProjectionReader I pass the instance of my provider here. I'm going to use that to execute the subquery down in the ExecuteSubQuery function.


Looking at ExectueSubQuery... Hey, what is that Replacer.Replace thing?


I haven't shown you that bit of magic yet. I will in just a moment. Let me explain what is going on in this method first.  I've got the argument that is a LambdaExpression that holds onto the original ProjectionExpression in the body and the parameter that was used to reference the current ProjectionRow.  That's all fine and dandy though.  The problem I have is that I can't just execute the projection expression via a call back to my provider because all of the ColumnExpressions that used to reference the outer query (think join condition in the Where clause) are now GetValue expressions. 


That's right, I've got references to the outer query in my sub query; chocolate in my peanut butter.  I can't leave those particular calls to GetValue in the projection, because they would be trying to access columns that don't exist in the new query.  What a dilema, Charlie Brown.


Thinking...


Aha! I've got it. Fortunately, all the data for those GetValue calls is readily available. It's sitting in the DataReader one object reference away from the code in ExecuteSubQuery. The data is available in the current row.  So what I want to do is somehow 'evaluate' those little bits of expressions right here and now and force those sub expressions to call those GetValue methods.  I wish I had code that could do that.  That would be perfect.


Wait, isn't that what Evaluator.PartialEval does?  Sure, but that won't work. Why?  Because those silly little expressions have references to my ProjectionRow parameter, and ParameterExpressions are the rule that tell it not to eval the expression.  If I could only get rid of those silly parameter references and instead have constants that point to my current running instance of ProjectionRow, then I could use Evaluator.PartialEval to turn those expressions into values!  Life would be good.


How to do that? I need a tool that will search my expression tree and replace some nodes with other nodes. Yah, that's the ticket.


Here's something, I call it Replacer.  It simply walks the tree looking for references to one node instance and swapping it for references to a different node.

    internal class Replacer : DbExpressionVisitor {

        Expression searchFor;

        Expression replaceWith;

        internal Expression Replace(Expression expression, Expression searchFor, Expression replaceWith) {

            this.searchFor = searchFor;

            this.replaceWith = replaceWith;

            return this.Visit(expression);

        }

        protected override Expression Visit(Expression exp) {

            if (exp == this.searchFor) {

                return this.replaceWith;

            }

            return base.Visit(exp);

        }

    }


Beautiful!  Sometimes I amaze even myself.


Okay, great, now I can swap out those nasty references to the ProjectionRow parameter with the real honest-to-goodness instance.  That's what the first line in ExecuteSubQuery does. And it only took a few dozen lines of English to explain it. ??


The second line calls Evaluate.PartialEval.  Just what I wanted. The line after that calls the provider to execute! Hurray! Then I throw the results into a List object. Finally, I recognize that I might have to turn the result back into an IQueryable.  Weird, I know, but the type of the 'Orders' property in the original query was IQueryable<Order> because that's how IQueryable query operators work, so C# invented the anonymous type using that for the member type.  If I try to just return the list, the projector that combines the results together will be none-too-pleased.  Fortunately, I already have a facility to turn IEnumerable's into IQueryables; Queryable.AsQueryable.


Wow! It's almost as if someone designed this stuff to work together.


Full disclosure:  I did cheat a little bit.  I had to modify the Evaluator class. I had to get it to understand my new expression types.  I know, I know, I said no one else needed to know about them, but it is my code too, so I guess that's alright.  I'll save that one-liner for you to view in the attached zip file.  I only post mega-long code snippets, not measly one-liners.


I also had to invent a new CanEvaluateLocally rule for Evaluator to use.  I needed to make sure that it would never think it was possible to evaluate one of my new nodes locally.


So now let's take a look on how DbQueryProvider changed

    public class DbQueryProvider : QueryProvider {

        DbConnection connection;

        TextWriter log;

        public DbQueryProvider(DbConnection connection) {

            this.connection = connection;

        }


        public TextWriter Log {

            get { return this.log; }

            set { this.log = value; }

        }


        public override string GetQueryText(Expression expression) {

            return this.Translate(expression).CommandText;

        }


        public override object Execute(Expression expression) {

            return this.Execute(this.Translate(expression));

        }


        private object Execute(TranslateResult query) {

            Delegate projector = query.Projector.Compile();


            if (this.log != null) {

                this.log.WriteLine(query.CommandText);

                this.log.WriteLine();

            }


            DbCommand cmd = this.connection.CreateCommand();

            cmd.CommandText = query.CommandText;

            DbDataReader reader = cmd.ExecuteReader();


            Type elementType = TypeSystem.GetElementType(query.Projector.Body.Type);

            return Activator.CreateInstance(

                typeof(ProjectionReader<>).MakeGenericType(elementType),

                BindingFlags.Instance | BindingFlags.NonPublic, null,

                new object[] { reader, projector, this },

                null

                );

        }


        internal class TranslateResult {

            internal string CommandText;

            internal LambdaExpression Projector;

        }


        private TranslateResult Translate(Expression expression) {

            ProjectionExpression projection = expression as ProjectionExpression;

            if (projection == null) {

                expression = Evaluator.PartialEval(expression);

                projection = (ProjectionExpression)new QueryBinder().Bind(expression);

            }

            string commandText = new QueryFormatter().Format(projection.Source);

            LambdaExpression projector = new ProjectionBuilder().Build(projection.Projector, projection.Source.Alias);

            return new TranslateResult { CommandText = commandText, Projector = projector };

        }

    }



The only thing that changed is my Translate method. It recognizes when it is handed a ProjectionExpression and chooses not do the work to turn an users query expression into a ProjectionExpression. Instead, it just skips down to the step that builds the command text and projection.


Did I forget to mention I added a 'Log' feature just like LINQ to SQL has.  That will help us see what's going on.  I added it here in my Context class too.

    public class Northwind {

        public Query<Customers> Customers;

        public Query<Orders> Orders;

        private DbQueryProvider provider;

        public Northwind(DbConnection connection) {

            this.provider = new DbQueryProvider(connection);

            this.Customers = new Query<Customers>(this.provider);

            this.Orders = new Query<Orders>(this.provider);

        }


        public TextWriter Log {

            get { return this.provider.Log; }

            set { this.provider.Log = value; }

        }

    }

 

Taking it for a Spin

Now let's give this new magic mojo a spin.

        string city = "London";

        var query = from c in db.Customers

                    where c.City == city

                    select new {

                        Name = c.ContactName,

                        Orders = from o in db.Orders

                                 where o.CustomerID == c.CustomerID

                                 select o

                    };

        foreach (var item in query) {

            Console.WriteLine("{0}", item.Name);

            foreach (var ord in item.Orders) {

                Console.WriteLine("\tOrder: {0}", ord.OrderID);

            }

        }



 


Run this and it outputs the following:


 

Thomas Hardy
        Order: 10355
        Order: 10383
        Order: 10453
        Order: 10558
        Order: 10707
        Order: 10741
        Order: 10743
        Order: 10768
        Order: 10793
        Order: 10864
        Order: 10920
        Order: 10953
        Order: 11016
Victoria Ashworth
        Order: 10289
        Order: 10471
        Order: 10484
        Order: 10538
        Order: 10539
        Order: 10578
        Order: 10599
        Order: 10943
        Order: 10947
        Order: 11023
Elizabeth Brown
        Order: 10435
        Order: 10462
        Order: 10848
Ann Devon
        Order: 10364
        Order: 10400
        Order: 10532
        Order: 10726
        Order: 10987
        Order: 11024
        Order: 11047
        Order: 11056
Simon Crowther
        Order: 10517
        Order: 10752
        Order: 11057
Hari Kumar
        Order: 10359
        Order: 10377
        Order: 10388
        Order: 10472
        Order: 10523
        Order: 10547
        Order: 10800
        Order: 10804
        Order: 10869
 


Here are the queries it executed:  (I used the new .Log property to capture these)


SELECT t2.ContactName, t2.CustomerID
FROM (
  SELECT t1.CustomerID, t1.ContactName, t1.Phone, t1.City, t1.Country
  FROM (
    SELECT t0.CustomerID, t0.ContactName, t0.Phone, t0.City, t0.Country
    FROM Customers AS t0
  ) AS t1
  WHERE (t1.City = 'London')
) AS t2


SELECT t4.OrderID, t4.CustomerID, t4.OrderDate
FROM (
  SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
  FROM Orders AS t3
) AS t4
WHERE (t4.CustomerID = 'AROUT')
SELECT t4.OrderID, t4.CustomerID, t4.OrderDate
FROM (
  SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
  FROM Orders AS t3
) AS t4
WHERE (t4.CustomerID = 'BSBEV')
SELECT t4.OrderID, t4.CustomerID, t4.OrderDate
FROM (
  SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
  FROM Orders AS t3
) AS t4
WHERE (t4.CustomerID = 'CONSH')
SELECT t4.OrderID, t4.CustomerID, t4.OrderDate
FROM (
  SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
  FROM Orders AS t3
) AS t4
WHERE (t4.CustomerID = 'EASTC')
SELECT t4.OrderID, t4.CustomerID, t4.OrderDate
FROM (
  SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
  FROM Orders AS t3
) AS t4
WHERE (t4.CustomerID = 'NORTS')
SELECT t4.OrderID, t4.CustomerID, t4.OrderDate
FROM (
  SELECT t3.OrderID, t3.CustomerID, t3.OrderDate
  FROM Orders AS t3
) AS t4
WHERE (t4.CustomerID = 'SEVES')


 


Okay, maybe lots of extra little queries is not ideal. Still, its better than throwing an exception!


 


Now, finally, Select is done. It really can handle any projection.  Maybe. 