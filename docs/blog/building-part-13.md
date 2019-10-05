# Building a LINQ IQueryable Provider – Part XIII: Updates and Batch Processing

Matt Warren - MSFT; January 22, 2009

------------------------------------

This is the thirteenth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you probably have a life beyond the keyboard, but if you don't then follow the link below to find oodles more to help fill your meaningless existence.


Complete list of posts in the Building an IQueryable Provider series


It's been precisely the correct amount of time that it took for me to complete the additional goodness that is jam packed into this drop, less actual work, dinners out, dinners in, any interesting film and televisions programs, housework, trips out to the store, family game night, time reading fiction, napping on the couch and other assorted unavoidable activities.


The full source code can be found at:


http://www.codeplex.com/IQToolkit


I'll try to cover as much as I can in this post, however you'll like find other gems by scouring the source itself.


What's inside:



Updates - Insert, Update & Delete operations.


Batch processing - true SQL Server batch processing.


Server language type systems - correct parameter types.


Mapping Changes - use the same class with multiple tables, etc.


Insert, Update and Delete

It's about time that this toolkit actually got usable right out of the box.  My original intention with the series was to show how to build an IQueryable provider and that turned more and more into a fully working query engine that you could actually use to get real work done. Yet, how many real world applications only ever need to pull data out of a database and never push it back?  Not many.


So I knew I'd eventually want to add updates, because I knew that you'd eventually need to do it too. Yet, every time I started thinking about updates I always fell into the trap of thinking about full blown ORM's with object tracking, et al, and I did not really want to go there, at least not yet. As a toolkit I think its just fine to define the primitives that a more advanced system might be built out of. And there is nothing wrong with those primitives being generally useful on their own. So you should be able to use the toolkit as-is and not only get a pretty good query engine but also something that at least works as a rudimentary data access layer.


Common Primitives

So then I set about thinking about just the primitives for updating data. They should have semantics similar to the underlying SQL equivalent commands. That means they should not defer work until some time later, but execute immediately. There should be at least the familiar commands, Insert, Update and Delete; but also Upsert (both Insert & Update combined) since its so often the right thing for many situations.


Also, like other LINQ operations, update commands should be a pattern, and be available for any kind of provider. So I set out thinking about what the pattern would look like and how it might be specified.  This is what I came up with.

public interface IUpdatable : IQueryable

{

}

public interface IUpdatable<T> : IUpdatable, IQueryable<T>

{

}


public static class Updatable

{

    public static S Insert<T, S>(this IUpdatable<T> collection, T instance, Expression<Func<T, S>> resultSelector)

    public static S Update<T, S>(this IUpdatable<T> collection, T instance, Expression<Func<T, bool>> updateCheck, Expression<Func<T, S>> resultSelector)

    public static S InsertOrUpdate<T, S>(this IUpdatable<T> collection, T instance, Expression<Func<T, bool>> updateCheck, Expression<Func<T, S>> resultSelector)

    public static int Delete<T>(this IUpdatable<T> collection, T instance, Expression<Func<T, bool>> deleteCheck)

    public static int Delete<T>(this IUpdatable<T> collection, Expression<Func<T, bool>> predicate)

    public static IEnumerable<S> Batch<T,S>(this IUpdatable collection, IEnumerable<T> instances, Expression<Func<T, S>> fnOperation, int batchSize, bool stream)
    ...

}


This pattern works just like the LINQ Enumerable and Queryable patterns.  I've declared an interface 'IUpdatable' that extends IQueryable, so anything that is updatable is also queryable, and then an Updatable class with a bunch of new extension methods that encapsulate the pattern.  (I realize the IUpdatable name may be in conflict with some other library, but until I think of something better this is what it is.)


The Insert method inserts an object instance into a collection. It's not an ordinary insert, like with List<T>. The collection is considered to be remote and inserting into it copies the data from your instance. It has an optional result-selector argument that can be a function you supply to construct a result out of the object after it has been inserted. This, of course, is intended to occur on the server and can be used to read back auto-generated state and computed expressions.

IUpdatable<Customer> customers = ...;

Customer c = ...;

customers.Insert(c);

IUpdatable<Order> orders = ...;

Order o = ...;

var id = orders.Insert(o, d => d.OrderID);


The Update method updates a corresponding object already in the collection with the values in the instance supplied. This is a complete overwrite, not a dynamic update like LINQ to SQL would have generated. I have not yet defined a piecemeal update operation, but I still can.  We'll see how it goes.  In addition to an update selector (like the one for insert) you can also specify an update-check predicate. This is an expression evaluated against the server's state and can be used to implement optimistic concurrency by basically checking to see if the server's state is still the same as you remembered it. An ORM layer built on top of this primitive might choose to generate this expression automatically, based on mapping information, but here you must specify it manually if you want to use it.

IUpdatable<Customer> customers = ...;

Customer c = ...;

var computedValue = customers.Update(c, d => d.City == originalCity, d => d.ComputedColumn);

The InsertOrUpdate is the 'Upsert' operation.  It will basically insert an object into the collection if a corresponding one does not exist, or update the one that does with the new values. You specify it just like you'd specify an update, instead you call InsertOrUpdate.


There are two flavors of Delete. The first one lets you delete the object in the collection corresponding to the instance. You can optionally specify a delete-check, which is similar to the update-check, a predicate function evaluated against the server's state. The delete will only occur if the check passes. The second flavor just lets you specify a predicate. It's basically a delete-all-where method and will delete all objects from the collection that match the predicate. So far, its the only SQL-like 'set-based' operation I've defined.

IUpdatable<Customer> customers = ...;

Customer c = ...;

customers.Delete(c, d => d.City == originalCity);

IUpdatable<Customer> customers = ...;

Customer c = ...;

customers.Delete(c => c.CustomerID == "ALFKI");


The last operation is Batch.  It will allow you to specify an operation to apply to a whole set of instances. The operation can be one of the other commands like Insert or Update.  You can use this method Insert, Update or Delete a whole bunch of objects all at the same time. If possible, the provider will use optimized batching techniques to give you extra performance.

IUpdatable<Customer> customers = ...;

Customer[] custs = new Customer[] { ... };

customers.Batch(custs, c => customers.Insert(c));


If you've got many objects to update and you want to have instance specific update-checks done, you can sneak the extra information into the batch process by combining the data together into a single collection and then piecing them apart in the operation.

IUpdatable<Customer> customers = ...;

var oldAndNew = new [] { new { Old = oldCustomer, New = newCustomer }, ...};

customers.Batch(oldAndNew, (u, x) => u.Update(x.New, d => d.City == x.Old.City));
 

Updates and DbQueryProvider

In order to make use of this new capability I'm going to need a new object to declare the IUpdatable interface.  The Query<T> class only implemented IQueryable<T>, and that was fine as long as I only ever want to query.  Now I also want to be able to update, so I need a new class to represent the root of my query that I can also update. These things in databases are called tables, so that's what I'll stick with. 

public interface IQueryableTable : IQueryable

{

    string TableID { get; }

}

public interface IQueryableTable<T> : IQueryable<T>, IQueryableTable

{

}


public class QueryableTable<T> : Query<T>, IQueryableTable<T>

{

    string id;


    public QueryableTable(IQueryProvider provider, string id)

        : base(provider)

    {

        this.id = id;

    }


    public QueryableTable(IQueryProvider provider)

        : this(provider, null)

    {

    }


    public string TableID

    {

        get { return this.id; }

    }

}


public interface IUpdatableTable : IQueryableTable, IUpdatable

{

}


public interface IUpdatableTable<T> : IQueryableTable<T>, IUpdatable<T>, IUpdatableTable

{

}


public class UpdatableTable<T> : QueryableTable<T>, IUpdatableTable<T>

{

    public UpdatableTable(IQueryProvider provider, string id)

        : base(provider, id)

    {

    }


    public UpdatableTable(IQueryProvider provider)

        : this(provider, null)

    {

    }

}


You'll note that not only did I define a UpdatableTable<T> class, which is specifically what I wanted, I also went ahead and made a QueryableTable<T>, and extra interfaces to correspond to them.  This is intentional.  Eventually, I may want to add more methods specific to tables here and I'll need a place to put them.  Right now I've only added a property 'TableID'.  You can ignore it for now, though it will get more interesting when I discuss the mapping changes.


Take a look at the Northwind class in the test source code and you'll see how I made use of my new table class.


The Plumbing

Of course, update commands work in the query provider just like queries do.  First there are a bunch of new DbExpression nodes to represent them.

public abstract class CommandExpression : DbExpression

{

}



public abstract class CommandWithResultExpression : CommandExpression

{

    public abstract Expression Result { get; }

}


public class InsertExpression : CommandWithResultExpression

{

    public TableExpression Table { get; }

    public ReadOnlyCollection<ColumnAssignment> Assignments { get; }

    public override Expression Result { get; }

}

public class ColumnAssignment

{

    public ColumnExpression Column { get; }

    public Expression Expression { get; }

}


public class UpdateExpression : CommandWithResultExpression

{

    public TableExpression Table { get; }

    public Expression Where { get; }

    public ReadOnlyCollection<ColumnAssignment> Assignments { get; }

    public override Expression Result { get; }

}


public class UpsertExpression : CommandWithResultExpression

{

    public Expression Check { get; }

    public InsertExpression Insert { get; }

    public UpdateExpression Update { get; }

    public override Expression Result { get; }

}


public class DeleteExpression : CommandExpression

{

    public TableExpression Table { get; }

    public Expression Where { get; }

}


public class BatchExpression : CommandExpression

{

    public Expression Input { get; }

    public LambdaExpression Operation { get; }

    public Expression BatchSize { get; }

    public Expression Stream { get; }

}


Then there's the standard visit method in DbExpressionVisitor, DbExpressionWriter, etc.  Binding them happens in the QueryBinder just like all other query operations, but the work of deciding what nodes to generate gets doled out to the QueryMapping object.  Luckily, the base QueryMapping class has a default implementation that builds the correct DbExpression node.  If you want to map a single object into multiple tables or some other crazy scheme you'll probably have to have a more advanced mapping implementation. ??


These nodes get plumbed through the system until they are encountered by the ExecutionBuilder and are formatted using the QueryLanguage rules. The TSQL formatter converts the nodes into corresponding TSQL text.  Depending on the contents of the command expression, the generated SQL may have one or more actual TSQL operations.


Batch Processing

ADO.Net has this nice feature built into its SqlClient API; the ability to get high-performance batch processing. Yet, the only way to get at it is through use of DataSet's or DataReaders. As far as I'm concerned this is rather low level and a bit complicated to use if you are starting out with domain objects and not DataSets. Your data access layer should do this for you. Yet, in order for it to do it, the abstraction for batch processing has to exist, which is why I added it to the updatable pattern.  After that it was a cinch. ??  Not really. 


What I needed fundamentally was something that would execute the same database command over and over again with different sets of parameters. This is basically what TSQL batching does as it is sent over the wire. So I needed to add this abstraction to DbQueryProvider. Yet, since only SqlClient supports this actually behavior I'd need a fall back plan. So DbQueryProvider implements a method to do batch processing, but it does not actually do it optimally. 

public virtual IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream)

{

    var result = this.ExecuteBatch(query, paramSets);

    if (!stream)

    {

        return result.ToList();

    }

    else

    {

        return new EnumerateOnce<int>(result);

    }

}

private IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets)

{

    this.LogCommand(query, null);

    DbCommand cmd = this.GetCommand(query, null);

    foreach (var paramValues in paramSets)

    {

        this.LogMessage("");

        this.LogParameters(query, paramValues);

        this.SetParameterValues(cmd, paramValues);

        int result = cmd.ExecuteNonQuery();

        yield return result;

    }

}


public virtual IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<DbDataReader, T> fnProjector, int batchSize, bool stream)

{

    var result = this.ExecuteBatch(query, paramSets, fnProjector);

    if (!stream)

    {

        return result.ToList();

    }

    else

    {

        return new EnumerateOnce<T>(result);

    }

}


private IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<DbDataReader, T> fnProjector)

{

    this.LogCommand(query, null);

    DbCommand cmd = this.GetCommand(query, null);

    cmd.Prepare();

    foreach (var paramValues in paramSets)

    {

        this.LogMessage("");

        this.LogParameters(query, paramValues);

        this.SetParameterValues(cmd, paramValues);

        var reader = cmd.ExecuteReader();

        if (reader.HasRows)

        {

            reader.Read();

            yield return fnProjector(reader);

        }

        else

        {

            yield return default(T);

        }

        reader.Close();

    }

}


What I have here are four methods, two of which are just private implementations, but two others that are virtual so can be overridden. Batching can work via streaming or not. If streamed the results of each execution (or batch) is yielded out. This works great if the number of individual items is large, but takes a lot of discipline to remember to actually inspect the results or nothing gets executed at all!  By requesting no streaming (stream == false) the execution occurs immediately and the results are packaged into a list that you can conveniently ignore if you so choose. That's why the implementation is separated out, so that the enumerable can be captured and converted to a list, enabling either behavior.  The two types of ExecuteBatch differ in whether a result is computed via information coming back from the server or not.


Now that these are defined, I can implement a new kind of provider, a SqlClient specific version that makes automatic use of optimized batching.

public class SqlQueryProvider : DbQueryProvider

{

    ...


    public override IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream)

    {

        var result = this.ExecuteBatch(query, paramSets, batchSize);

        if (!stream)

        {

            return result.ToList();

        }

        else

        {

            return new EnumerateOnce<int>(result);

        }

    }

    private IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize)

    {

        SqlCommand cmd = (SqlCommand)this.GetCommand(query, null);

        DataTable dataTable = new DataTable();

        for (int i = 0, n = query.Parameters.Count; i < n; i++)

        {

            var qp = query.Parameters[i];

            cmd.Parameters[i].SourceColumn = qp.Name;

            dataTable.Columns.Add(qp.Name, qp.Type);

        }

        SqlDataAdapter dataAdapter = new SqlDataAdapter();

        dataAdapter.InsertCommand = cmd;

        dataAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;

        dataAdapter.UpdateBatchSize = batchSize;


        this.LogMessage("-- Start SQL Batching --");

        this.LogCommand(query, null);


        IEnumerator<object[]> en = paramSets.GetEnumerator();

        using (en)

        {

            bool hasNext = true;

            while (hasNext)

            {

                int count = 0;

                for (; count < dataAdapter.UpdateBatchSize && (hasNext = en.MoveNext()); count++)

                {

                    var paramValues = en.Current;

                    dataTable.Rows.Add(paramValues);

                    this.LogMessage("");

                    this.LogParameters(query, paramValues);

                }

                if (count > 0)

                {

                    int n = dataAdapter.Update(dataTable);

                    for (int i = 0; i < count; i++)

                    {

                        yield return (i < n) ? 1 : 0;

                    }

                    dataTable.Rows.Clear();

                }

            }

        }


        this.LogMessage(string.Format("-- End SQL Batching --"));

    }

}


Note that I only have an implementation for the variation of ExecuteBatch that computes no user specified result. This is due to there being no back-channel available when using SqlClient batching.


The implementation uses DataTable's and DataAdapters to make this work.  A DataTable is created filled with the parameters necessary for executing the command.  The DataAdapter is used to invoke the batch using the Update method.  Of course, this doesn't actually have to be an update command. I can also use this to batch inserts and deletes too, or really any TSQL that I want to execute as long as I don't expected to get a bunch of data back.


Server Language Types

One thing that has always bugged me about SQL Server was the need to get the command parameters right. If I declare the parameter to be the wrong text flavor I can cause serious performance issues for the query. So just setting the parameter values and having the SqlCommand object guess at the right SqlType encoding is really not a good plan. Fortunately, it is often possible to figure out the correct parameter types to use if the information is available via the mapping. Parameters are often never just sent to the server for no reason. If I use a parameter in a query I'm usually comparing it against a column.  In most cases I can simply infer that the parameter should have the same server type as the column.


So I've gone ahead and defined a new server type system abstraction and threaded it into some of the DbExpressions and make use of it in some of the visitors. 

public abstract class QueryType

{

    public abstract DbType DbType { get; }

    public abstract bool NotNull { get; }

    public abstract int Length { get; }

    public abstract short Precision { get; }

    public abstract short Scale { get; }

}

public abstract class QueryTypeSystem

{

    public abstract QueryType Parse(string typeDeclaration);

    public abstract QueryType GetColumnType(Type type);

}


A QueryType encodes the typical database scalar type. It has a few properties that you can use to inspect common attributes of a server type, but most code won't really care to know the details, it will just pass the information along until it ends up where I need it.  A QueryTypeSystem is basically a factory for producing QueryType's.  The Parse method will construct a language-specific QueryType out of some text encoding.  This is typically the server language syntax for declaring a column of that particular type, like 'VARCHAR(10)'. 


A QueryTypeSystem is specific to a language, so QueryLanguage is where you go to get one.

public abstract class QueryLanguage

{

    public abstract QueryTypeSystem TypeSystem { get; }
}

One place I definitely know where to encode server types is in the ColumnExpression.  If a ColumnExpression knows what its server type is, then when I get to the point of comparing parameters to columns I know which server type is in play.

public class ColumnExpression : DbExpression, IEquatable<ColumnExpression>

{

    public QueryType QueryType { get; }

}

I've also stuck it into NamedValueExpression, because that's the type I'm using for parameters.

public class NamedValueExpression : DbExpression

{

    public QueryType QueryType { get; }
}

And I've basically modified Parameterizer, so that if a column and parameter (named-value expression) are ever paired together in any binary expression, I'll infer the parameter to have the same server type as the column.

public class Parameterizer : DbExpressionVisitor

{

    protected override Expression VisitBinary(BinaryExpression b)

    {

        Expression left = this.Visit(b.Left);

        Expression right = this.Visit(b.Right);

        if (left.NodeType == (ExpressionType)DbExpressionType.NamedValue

         && right.NodeType == (ExpressionType)DbExpressionType.Column)

        {

            NamedValueExpression nv = (NamedValueExpression)left;

            ColumnExpression c = (ColumnExpression)right;

            left = new NamedValueExpression(nv.Name, c.QueryType, nv.Value);

        }

        else if (b.Right.NodeType == (ExpressionType)DbExpressionType.NamedValue

         && b.Left.NodeType == (ExpressionType)DbExpressionType.Column)

        {

            NamedValueExpression nv = (NamedValueExpression)right;

            ColumnExpression c = (ColumnExpression)left;

            right = new NamedValueExpression(nv.Name, c.QueryType, nv.Value);

        }

        return this.UpdateBinary(b, left, right, b.Conversion, b.IsLiftedToNull, b.Method);

    }

    protected override ColumnAssignment VisitColumnAssignment(ColumnAssignment ca)

    {

        ca = base.VisitColumnAssignment(ca);

        Expression expression = ca.Expression;

        NamedValueExpression nv = expression as NamedValueExpression;

        if (nv != null)

        {

            expression = new NamedValueExpression(nv.Name, ca.Column.QueryType, nv.Value);

        }

        return this.UpdateColumnAssignment(ca, ca.Column, expression);

    }
}


Of course, the same goes for ColumnAssignment used by Insert and Update commands. You'll notice that I'm not having these types flow throughout the expression tree like normal types.  I could probably get more edge cases correct if I did, but for now this handles most of the cases. 


The GetCommand method in  DbQueryProvider will now make use of this info when constructing parameters. The SqlQueryProvider expects to see a new TSqlType that's made available by a new TSqlTypeSystem found on the TSqlLanguage object.  ??

public class SqlQueryProvider : DbQueryProvider

{

    protected override DbCommand GetCommand(QueryCommand query, object[] paramValues)

    {

        // create command object (and fill in parameters)

        SqlCommand cmd = new SqlCommand(query.CommandText, (SqlConnection)this.Connection);

        for (int i = 0, n = query.Parameters.Count; i < n; i++)

        {

            QueryParameter qp = query.Parameters[i];

            TSqlType sqlType = (TSqlType)qp.QueryType;

            if (sqlType == null)

                sqlType = (TSqlType)this.Language.TypeSystem.GetColumnType(qp.Type);

            var p = cmd.Parameters.Add("@" + qp.Name, sqlType.SqlDbType, sqlType.Length);

            if (sqlType.Precision != 0)

                p.Precision = (byte)sqlType.Precision;

            if (sqlType.Scale != 0)

                p.Scale = (byte)sqlType.Scale;

            if (paramValues != null)

            {

                p.Value = paramValues[i] ?? DBNull.Value;

            }

        }

        return cmd;

    }
}

Mapping Changes

I've made some changes to the mapping system (or QueryMapping to be particular.)  I came across a variety of odd behavior while developing update logic that basically boiled down to the ImplictMapping object not being able to tell the difference between a type that was intended to correspond to a database table and others that appeared there just for the sake of representation, like LINQ anonymous types.  Some other mapping implementations might be able to tell the difference, but the simplest one couldn't so I needed to find another solution.


Obviously, everything that is an entity in a query comes from somewhere, and that's either from one of the roots of the query (a table) or via a relationship property. It was a mistake to think otherwise (or not think about it at all.) What I needed was a more explicit representation in the expression tree of what was an entity and what was not.  I figured I could either add annotations to the tree in every node, or find some nominal solution that would do the trick. 


I chose to make a new expression node, EntityExpression, which I use as a wrapper around any expression that would be normally constructing an entitiy.  This node is placed into the system when the QueryMapping first creates the sub-express for constructing an entity or relationship, at the time I actually know that I'm dealing with an entity and in particular which entity it is.

public class EntityExpression : DbExpression

{

    public MappingEntity Entity { get ; }

    public Expression Expression { get; }

}

I've also introduced a new abstraction called MappingEntity.  This how I let the QueryMapping object place a bread-crumb into the expression tree so it can be reminded which exact entity was being referred to.

public class MappingEntity

{

    public string TableID { get; }

    public Type Type { get; }

}

It' really just as simple little class that minimally remembers the correspondence between the runtime type and the table its being mapped to.  If you've been paying attention you'll realize that this 'TableID' is the same property that was added to the IQueryableTable interface.  That's how the query engine gets the table-id in the first place, right from the start of the query.  Of course, the IQueryableTable<T> interface also knows the runtime type, that's the 'T' part.  So your table's have all the information needed to make a MappingEntity.  Except that job is deferred to the QueryMapping object so it can do whatever bookkeeping it wants.

public abstract class QueryMapping

{
    public virtual MappingEntity GetEntity(Type type);

    public virtual MappingEntity GetEntity(Type type, string tableID);

}

You'll also notice that most of the other methods on QueryMapping are now modified to take a MappingEntity as an argument.

public abstract class QueryMapping

{

    public virtual bool IsMapped(MappingEntity entity, MemberInfo member)

    public virtual bool IsColumn(MappingEntity entity, MemberInfo member)

    public virtual bool IsIdentity(MappingEntity entity, MemberInfo member)

    public virtual bool IsComputed(MappingEntity entity, MemberInfo member)

    public virtual bool IsGenerated(MappingEntity entity, MemberInfo member)

    public virtual bool IsRelationship(MappingEntity entity, MemberInfo member)

    public virtual MappingEntity GetRelatedEntity(MappingEntity entity, MemberInfo member)

    public virtual bool IsAssociationRelationship(MappingEntity entity, MemberInfo member)
    ...

}

Now, what you're probably saying is "Gee, Matt, that's looks a bit ominous. Why aren't these all just methods on MappingEntity now?"  And you'd be right. They probably should be.  Yet, it makes it a lot more difficult to subclass the mapping object and merely override some of the behavior.  Not a big deal for complete mapping sub-systems that someday might exists, but painful for simple uses such as overriding the ImplicitMapping with a few additional rules.  So I'm leaving it as-is for now until I can think about it more.  Any thoughts from the peanut-gallery?


Also, its worth nothing, that given this new arrangement, with explicit entity information in the query tree and connecting each entity info back to the table it originated from, it is now possible to support mapping systems that allow individual runtime types to be mapped to more than one table.  So you can have your cake and eat it too! 


That's All Folks!

At least for today.  The future may hold more goodies.  Any suggestions are welcome, either as ideas or source-code contributions.

