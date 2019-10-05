# LINQ: Building an IQueryable Provider – Part IV: Select

Matt Warren - MSFT; August 2, 2007

---


This post is the fourth in a series of posts covering how to build a LINQ IQueryable provider. If you have not read the previous posts, please do so before proceeding.


Complete list of posts in the Building an IQueryable Provider series 



I just could not leave well enough alone.  I had the crude LINQ provider working with just a translation of the Where method into SQL.  I could execute the query and convert the results into my objects.  But that’s not good enough for me, and I know it’s not good enough for you. You probably want to see it all; the transformation of a little sample program into a full-fledged working ORM system. Well, I’m probably not going to do that.  However, I still think there’s a lot of common ground I can cover, that you can make use of in your provider, by showing you how I’m going to implement Select.


Implementing Select


Translating the Where method was easy compared to Select.  I’m not talking about your garden variety SQL operation of selecting five columns out of ten.  I’m talking about a LINQ Select operation where you can transform your data into just about any shape you want.  The selector function of the LINQ Select operator can be any transforming expression that the user can imagine.  There could be object constructors, initializers, conditionals, binary operators, method calls; the whole lot. How am I going to translate any of that to SQL, let alone reproduce that structure in the objects I return?


Fortunately, I don’t really have to do any of that.  Why not?  Surely, that’s got to be a lot of code to write?  Right?  The truth is I already have the code to do it, at least most of it, and I didn’t stay up all night writing it in a bout of binge programming.  I didn’t write it at all.  The user did when he wrote the query.


The selector function is the code to construct the results.  If this were LINQ to objects instead of my IQueryable provider, the selector function would be the code that was run to produce the results. Why should it be any different now?


Aha, not so fast.  Surely, the selector function would be just dandy if it were actual code and not an expression tree, and even if it were code it would just be a function that converts one object into another.  Yet, I don’t have an object to convert. I have a DbDataReader and it has fields.  It’s an object I want to produce.  I don’t already have one.


Of course, you might be able to think of a real cheesy solution that just combines the prior ObjectReader with basically a LINQ to Objects version of Select to convert the results of retrieving all the data into a different shape.  Yet, that would be a gross misuse of the fabric of space-time. We shouldn’t be retrieving all the data. We should just bring back the bits that are needed to produce the results. What a dilemma.


Fortunately, it’s still easy.  I just have to convert the selector function I already have into the one I need.  What is the one I need?  Well, one that reads data from a DbDataReader for starters.  Okay, sure, but maybe if I abstract the DataReader out of the problem and make it about getting the data from a method called ‘GetValue’.  Yes, I know DataReader already has one of those, but it also has this nasty habit of returning DbNull’s.


public abstract class ProjectionRow {


    public abstract object GetValue(int index);


}



So here’s this simple abstract base class that represents a row of data.  If my selector expression was pulling data out of this guy by calling ‘GetValue’ and then using an Expression.Convert operation I’d be smiling.


Let’s take a look at the code that builds my new selector.


internal class ColumnProjection {


    internal string Columns;


    internal Expression Selector;


}


 


internal class ColumnProjector : ExpressionVisitor {


    StringBuilder sb;


    int iColumn;


    ParameterExpression row;


    static MethodInfo miGetValue;


 


    internal ColumnProjector() {


        if (miGetValue == null) {


            miGetValue = typeof(ProjectionRow).GetMethod("GetValue");


        }


    }


 


    internal ColumnProjection ProjectColumns(Expression expression, ParameterExpression row) {


        this.sb = new StringBuilder();


        this.row = row;


        Expression selector = this.Visit(expression);


        return new ColumnProjection { Columns = this.sb.ToString(), Selector = selector };


    }


 


    protected override Expression VisitMemberAccess(MemberExpression m) {


        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter) {


            if (this.sb.Length > 0) {


                this.sb.Append(", ");


            }


            this.sb.Append(m.Member.Name);


            return Expression.Convert(Expression.Call(this.row, miGetValue, Expression.Constant(iColumn++)), m.Type);


        }


        else {


            return base.VisitMemberAccess(m);


        }


    }


}



Now, that doesn’t look like all that much code does it.  ColumnProjector is a visitor that walks down an expression tree converting column references (or what it thinks are references to columns) into tiny expressions that access the individual data values via a call to this GetValue method.  Where does this method come from?  There’s a ParameterExpression called ‘row’ that is typed to be a ProjectionRow that abstract class I defined above.  Not only am I rebuilding this selector expression I am going to turn it eventually into the body of a lambda expression that takes a ProjectionRow as an argument. That way I can convert the LambdaExpression into a delegate by calling the LambdaExpression.Compile method.


Notice this visitor also builds up a string representing our SQL select clause.  Bonus!  Now whenever I see a Queryable.Select in the query expression I can convert the selector into both the function that produces the results and the select clause I need for the command text.


Let’s just see where this fits in.  Here’s my modified QueryTranslator. (The relevant bits anyway.)


internal class TranslateResult {


    internal string CommandText;


    internal LambdaExpression Prsojector;


}



internal class QueryTranslator : ExpressionVisitor {


    StringBuilder sb;


    ParameterExpression row;


    ColumnProjection projection;


 


    internal QueryTranslator() {


    }


 


    internal TranslateResult Translate(Expression expression) {


        this.sb = new StringBuilder();


        this.row = Expression.Parameter(typeof(ProjectionRow), "row");


        this.Visit(expression);


        return new TranslateResult {


            CommandText = this.sb.ToString(),


            Projector = this.projection != null ? Expression.Lambda(this.projection.Selector, this.row) : null


        };


    }


 


    protected override Expression VisitMethodCall(MethodCallExpression m) {


        if (m.Method.DeclaringType == typeof(Queryable)) {


            if (m.Method.Name == "Where") {


                sb.Append("SELECT * FROM (");


                this.Visit(m.Arguments[0]);


                sb.Append(") AS T WHERE ");


                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);


                this.Visit(lambda.Body);


                return m;


            }


            else if (m.Method.Name == "Select") {


                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);


                ColumnProjection projection = new ColumnProjector().ProjectColumns(lambda.Body, this.row);


                sb.Append("SELECT ");


                sb.Append(projection.Columns);


                sb.Append(" FROM (");


                this.Visit(m.Arguments[0]);


                sb.Append(") AS T ");


                this.projection = projection;


                return m;


            }


        }


        throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));


    }



    . . .


}


 


As you can see, the QueryTranslator now handles the Select method, building a SQL SELECT statement just like the Where method did.  However, it also remembers that last ColumnProjection (the result of calling ProjectColumns) and returns the newly reconstructed selector as a LambdaExpression in the TranslateResult object.


Now all I need is an ObjectReader that works off this LambdaExpression instead of just constructing a fixed object. 


Look, here’s one now. 


internal class ProjectionReader<T> : IEnumerable<T>, IEnumerable {


    Enumerator enumerator;


 


    internal ProjectionReader(DbDataReader reader, Func<ProjectionRow, T> projector) {


        this.enumerator = new Enumerator(reader, projector);


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


 


        internal Enumerator(DbDataReader reader, Func<ProjectionRow, T> projector) {


            this.reader = reader;


            this.projector = projector;


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



The ProjectionReader class is nearly identical to the ObjectReader class shown in Part II, except that the logic for constructing an object out of fields is missing.  In its place is just a call to this new ‘projector’ delegate.  The idea is that this is going to be the same exact delegate that we can get out of our rebuilt selector expression. 


If you recall, I rebuilt the selector expression to reference a parameter of type ProjectionRow.  Now, we see here that the Enumerator class inside the ProjectionReader implements this ProjectionRow.  It’s a good thing too, because it’s the only object around that knows about the DbDataReader.  It also makes it really easy for us to simply pass the ‘this’ expression into the delegate when we invoke it. 


It’s nice when everything just seems to fit together.  Now I just need to assemble the pieces in the DbQueryProvider.


So here’s my new provider:


public class DbQueryProvider : QueryProvider {


    DbConnection connection;


 


    public DbQueryProvider(DbConnection connection) {


        this.connection = connection;


    }


 


    public override string GetQueryText(Expression expression) {


        return this.Translate(expression).CommandText;


    }


 


    public override object Execute(Expression expression) {


        TranslateResult result = this.Translate(expression);


 


        DbCommand cmd = this.connection.CreateCommand();


        cmd.CommandText = result.CommandText;


        DbDataReader reader = cmd.ExecuteReader();


 


        Type elementType = TypeSystem.GetElementType(expression.Type);


        if (result.Projector != null) {


            Delegate projector = result.Projector.Compile();


            return Activator.CreateInstance(


                typeof(ProjectionReader<>).MakeGenericType(elementType),


                BindingFlags.Instance | BindingFlags.NonPublic, null,


                new object[] { reader, projector },


                null


                );


        }


        else {


            return Activator.CreateInstance(


                typeof(ObjectReader<>).MakeGenericType(elementType),


                BindingFlags.Instance | BindingFlags.NonPublic, null,


                new object[] { reader },


                null


                );


        }


    }


 


    private TranslateResult Translate(Expression expression) {


        expression = Evaluator.PartialEval(expression);


        return new QueryTranslator().Translate(expression);


    }


}


 


The call to Translate gives me back everything I need.  I just need to convert the LambdaExpression into a delegate by calling Compile.  Notice how I still need to keep around the old ObjectReader.  That’s just in case there was never a Select in the query at all.


Now let’s see if I can really use it.


string city = "London";


var query = db.Customers.Where(c => c.City == city)
              .Select(c => new {Name = c.ContactName, Phone = c.Phone});


Console.WriteLine("Query:\n{0}\n", query);



var list = query.ToList();


foreach (var item in list) {


    Console.WriteLine("{0}", item);


}


 


 


Running this now produces the following result:


Query:
SELECT ContactName, Phone FROM (SELECT * FROM (SELECT * FROM Customers) AS T WHERE (City = 'London')) AS T


{ Name = Thomas Hardy, Phone = (171) 555-7788 }
{ Name = Victoria Ashworth, Phone = (171) 555-1212 }
{ Name = Elizabeth Brown, Phone = (171) 555-2282 }
{ Name = Ann Devon, Phone = (171) 555-0297 }
{ Name = Simon Crowther, Phone = (171) 555-7733 }
{ Name = Hari Kumar, Phone = (171) 555-1717 }



Look, I’m no longer returning all the results.  That’s just what I wanted.  The translated selector expression turned into a delegate that included the ‘new xxx’ anonymous type initializser and calls to GetValue that read the results from the DataReader right into my object w/o having to use reflection on every single field.  This just gets better and better.


You must be thinking now that I’m finally done.  This provider rocks!  What more could be left to do?


There’s still a lot more that can be done.  Yet, even with select, even with how good it appears to be working, there are some major holes/flaws in this solution.  Fixing them is going to take a major overhaul of the code.  


Fortunately, for me, that’s the fun part.  See you next time in Part V.


 


Matt

