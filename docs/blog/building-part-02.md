# LINQ: Building an IQueryable Provider – Part II: Where and a reusable Expression tree visitor

Matt Warren - MSFT; July 31, 2007

This is the second post in a multi-post series on building LINQ providers using the IQueryable interface.
If you are new to this series please read the following post before proceeding.

---

Now, that I’ve laid the groundwork defining a reusable version of `IQueryable` and `IQueryProvider`, namely `Query<T>` and `QueryProvider`, 
I’m going to build a provider that actually does something.
As I said before, what a query provider really does is execute a little bit of ‘code’ defined as an expression tree instead of actual IL.
Of course, it does not actually have to execute it in the traditional sense.
For example, LINQ to SQL translates the query expression into SQL and sends it to the server to execute it.


My sample below is going to work much like LINQ to SQL in that it translates and executes a query against an ADO provider.
However, I must add a disclaimer here, this sample is not a full-fledge provider in any sense.
I’m only going to handle translating the ‘Where’ operation and I’m not even going to attempt to do anything more complicated than allow the predicate to contain a field reference and few simple operators.
I may expand on this provider in the future, but for now it is mostly for illustrative purposes only.
Please don’t copy and paste and expect to have ship-quality code.


The provider is going to basically do two things:
  1) translate the query into a SQL command text
  2) translate the result of executing the command into objects.

<br/>

## The Query Translator


The query translator is going to simply visit each node in the query’s expression tree and translate the supported operations into text using a StringBuilder.  For the sake of clarity assume there is a class called ExpressionVisitor that defines the base visitor pattern for Expression nodes.  (I promise I’ll actually include the code for this at the end of the post but for now bear with me.)


```csharp
internal class QueryTranslator : ExpressionVisitor
{
    StringBuilder sb;

    internal QueryTranslator()
    {
    }

    internal string Translate(Expression expression)
    {
        this.sb = new StringBuilder();
        this.Visit(expression);
        return this.sb.ToString();
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        return e;
    }

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
        {
            sb.Append("SELECT * FROM (");
            this.Visit(m.Arguments[0]);
            sb.Append(") AS T WHERE ");
            LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
            this.Visit(lambda.Body);
            return m;
        }

        throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Not:
                sb.Append(" NOT ");
                this.Visit(u.Operand);
                break;

            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }

        return u;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        sb.Append("(");

        this.Visit(b.Left);

        switch (b.NodeType)
        {
            case ExpressionType.And:
                sb.Append(" AND ");
                break;

            case ExpressionType.Or:
                sb.Append(" OR");
                break;

            case ExpressionType.Equal:
                sb.Append(" = ");
                break;

            case ExpressionType.NotEqual:
                sb.Append(" <> ");
                break;

            case ExpressionType.LessThan:
                sb.Append(" < ");
                break;

            case ExpressionType.LessThanOrEqual:
                sb.Append(" <= ");
                break;

            case ExpressionType.GreaterThan:
                sb.Append(" > ");
                break;

            case ExpressionType.GreaterThanOrEqual:
               sb.Append(" >= ");
                break;

            default:
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
        }

        this.Visit(b.Right);

        sb.Append(")");

        return b;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        IQueryable q = c.Value as IQueryable;

        if (q != null)
        {
            // assume constant nodes w/ IQueryables are table references
            sb.Append("SELECT * FROM ");
            sb.Append(q.ElementType.Name);
        }
        else if (c.Value == null)
        {
            sb.Append("NULL");
        }
        else
        {
            switch (Type.GetTypeCode(c.Value.GetType()))
            {
                case TypeCode.Boolean:
                    sb.Append(((bool)c.Value) ? 1 : 0);
                    break;

                case TypeCode.String:
                    sb.Append("'");
                    sb.Append(c.Value);
                    sb.Append("'");
                    break;

                case TypeCode.Object:
                    throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                default:
                    sb.Append(c.Value);
                    break;
            }
        }

        return c;
    }

    protected override Expression VisitMemberAccess(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            sb.Append(m.Member.Name);
            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }
}
```

 


As you can see, there’s not much there and still it is rather complicated.
What I’m expecting to see in the expression tree is at most a method call node with arguments referring to the source (argument 0) and the predicate (argument 1).
Take a look at the VisitMethodCall method above.
I explicitly handle the case of the `Queryable.Where` method, generating a “SELECT * FROM (“, recursively visiting the source and then appending “) AS T WHERE “,
and then visiting the predicate.
This allows for other query operators present in the source expression to be nested sub-queries.
I don’t handle any other operators, but if more than one call to Where is used then I’m able to deal with that gracefully.
It doesn’t matter what the table alias is that is used (I picked ‘T’) since I’m not going to generate references to it anyway.
A more full-fledged provider would of course want to do this.


There’s a little helper method included called `StripQutotes`.
Its job is to strip away any `ExpressionType.Quote` nodes on the method arguments (which may happen) so I can get at the pure lambda expression that I’m looking for.


The `VisitUnary` and `VisitBinary` methods are straightforward.
They simply inject the correct text for the specific unary and binary operators I’ve chosen to support.
The interesting bit of translation comes in the `VisitConstant` method.
You see, table references in my world are just the root IQueryable’s.
If a constant node holds one of my `Query<T>` instances then I’ll just assume it’s meant to represent the root table
so I’ll append “SELECT * FROM” and then the name of the table which is simply the name of the element type of the query.
The rest of the translation for constant nodes just deals with actual constants.
Note, these constants are added to the command text as literal values.
There is nothing here to stop injection attacks that real providers need to deal with.


Finally, `VisitMemberAccess` assumes that all field or property accesses are meant to be column references in the command text.
No checking is done to prove that this is true.
The name of the field or property is assumed to match the name of the column in the database.


Given a class `Customers` with fields matching the names of the columns in the Northwind sample database, this query translator will generate queries that look like this:


#### For the query:

```csharp
Query<Customers> customers = ...;
IQueryable<Customers> q = customers.Where(c => c.City == "London");
```

#### Resulting SQL:

```SQL
SELECT * FROM (SELECT *FROM Customers) AS T WHERE (city = ‘London’)
```

<br/>

## The Object Reader

The job of the object reader is to turn the results of a SQL query into objects.
I’m going to build a simple class that takes a `DbDataReader` and a type `T` and I’ll make it implement `IEnumerable<T>`.
There are no bells and whistles in this implementation.
It will only work for writing into class fields via reflection.
The names of the fields must match the names of the columns in the reader and the types must match whatever the `DataReader` thinks is the correct type.

```csharp
internal class ObjectReader<T> : IEnumerable<T>, IEnumerable where T : class, new()
{
    Enumerator enumerator;

    internal ObjectReader(DbDataReader reader)
    {
        this.enumerator = new Enumerator(reader);
    }

    public IEnumerator<T> GetEnumerator()
    {
        Enumerator e = this.enumerator;

        if (e == null)
        {
            throw new InvalidOperationException("Cannot enumerate more than once");
        }

        this.enumerator = null;
        return e;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    class Enumerator : IEnumerator<T>, IEnumerator, IDisposable
    {
        DbDataReader reader;
        FieldInfo[] fields;
        int[] fieldLookup;
        T current;

        internal Enumerator(DbDataReader reader)
        {
            this.reader = reader;
            this.fields = typeof(T).GetFields();
        }

        public T Current
        {
            get { return this.current; }
        }

        object IEnumerator.Current
        {
            get { return this.current; }
        }

        public bool MoveNext()
        {
            if (this.reader.Read())
            {
                if (this.fieldLookup == null)
                {
                    this.InitFieldLookup();
                }

                T instance = new T();

                for (int i = 0, n = this.fields.Length; i < n; i++)
                {
                    int index = this.fieldLookup[i];

                    if (index >= 0)
                    {
                        FieldInfo fi = this.fields[i];

                        if (this.reader.IsDBNull(index))
                        {
                            fi.SetValue(instance, null);
                        }
                        else
                        {
                            fi.SetValue(instance, this.reader.GetValue(index));
                        }
                    }
                }

                this.current = instance;

                return true;
            }

            return false;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
            this.reader.Dispose();
        }

        private void InitFieldLookup()
        {
            var map = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0, n = this.reader.FieldCount; i < n; i++)
            {
                map.Add(this.reader.GetName(i), i);
            }

            this.fieldLookup = new int[this.fields.Length];

            for (int i = 0, n = this.fields.Length; i < n; i++)
            {
                int index;

                if (map.TryGetValue(this.fields[i].Name, out index))
                {
                    this.fieldLookup[i] = index;
                }
                else
                {
                    this.fieldLookup[i] = -1;
                }
            }
        }
    }
}
```

The `ObjectReader` creates a new instance of type `T` for each row read by the `DbDataReader`.
It uses the reflection API `FieldInfo.SetValue` to assign values to each field of the object.
When the `ObjectReader` is first created it instantiates an instance of the nested `Enumerator` class.
This enumerator is handed out when the `GetEnumerator` method is called. Since `DataReader`’s cannot reset and execute again,
the enumerator can only be handed out once.
If `GetEnumerator` is called a second time an exception is thrown.


The `ObjectReader` is lenient in the ordering of fields.
Since the `QueryTranslator` builds queries using `SELECT *` this is a must because otherwise the code has no way of knowing which column will appear first in the results.
Note, that it is generally inadvisable to use `SELECT *` in production code.
Remember this is just an illustrative sample to show how in general to put together a LINQ provider.
In order to allow for different sequences of columns, the precise sequence is discovered at runtime when the first row is read for the `DataReader`.
The `InitFieldLookup` function builds a map from column name to column ordinal and then assembles a lookup table `fieldLookup` that maps between the object’s fields and the ordinals.

<br/>

## The Provider


Now that we have these two pieces (and the classes define in the prior post) it’s quite easy to combine them together to make an actual IQueryable LINQ provider.


```csharp
public class DbQueryProvider : QueryProvider
{
    private readonly DbConnection connection;

    public DbQueryProvider(DbConnection connection)
    {
        this.connection = connection;
    }

    public override string GetQueryText(Expression expression)
    {
        return this.Translate(expression);
    }

    public override object Execute(Expression expression)
    {
        DbCommand cmd = this.connection.CreateCommand();
        cmd.CommandText = this.Translate(expression);
        DbDataReader reader = cmd.ExecuteReader();
        Type elementType = TypeSystem.GetElementType(expression.Type);

        return Activator.CreateInstance(
            typeof(ObjectReader<>).MakeGenericType(elementType),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { reader },
            null);
    }

    private string Translate(Expression expression)
    {
        return new QueryTranslator().Translate(expression);
    }
}
```

As you can see, building a provider is now simply an exercise in combining these two pieces.
The `GetQueryText` method just needs to use the `QueryTranslator` to produce the command text.
The `Execute` method uses both `QueryTranslator` and `ObjectReader` to build a `DbCommand` object, execute it and return the results as an `IEnumerable`.

<br/>

### Trying it Out


Now that we have our provider we can try it out.
Since I’m basically following the LINQ to SQL model I’ll define a class for the Customers table, 
a ‘Context’ that holds onto the tables (root queries) and a little program that uses them.


```csharp
public class Customers
{
    public string CustomerID;
    public string ContactName;
    public string Phone;
    public string City;
    public string Country;
}

public class Orders
{
    public int OrderID;
    public string CustomerID;
    public DateTime OrderDate;
}

public class Northwind
{
    public Query<Customers> Customers;
    public Query<Orders> Orders;

    public Northwind(DbConnection connection)
    {
        QueryProvider provider = new DbQueryProvider(connection);
        this.Customers = new Query<Customers>(provider);
        this.Orders = new Query<Orders>(provider);
    }
}

class Program
{
    static void Main(string[] args)
    {
        string constr = @"…";

        using (SqlConnection con = new SqlConnection(constr))
        {
            con.Open();
            Northwind db = new Northwind(con);

            IQueryable<Customers> query =
                 db.Customers.Where(c => c.City == "London");

            Console.WriteLine("Query:\n{0}\n", query);

            var list = query.ToList();

            foreach (var item in list)
            {
                Console.WriteLine("Name: {0}", item.ContactName);
            }

            Console.ReadLine();
        }
    }
}
```


Now if we run this we should get the following output:  (Note that you will have to add your own connection string to the program above.)

```
Query: SELECT * FROM (SELECT * FROM Customers) AS T WHERE (City = 'London')

Name: Thom7as Hardy
Name: Victoria Ashworth
Name: Elizabeth Brown
Name: Ann Devon
Name: Simon Crowther
Name: Hari Kumar
```

Excellent, just what I wanted.  I love it when a plan comes together.


That’s it folks.  That’s a LINQ IQueryable provider.  Well, at least a crude facsimile of one.
Of course, yours will do so much more than mine.  It will handle all the edge cases and serve coffee. 

 <br/>

## APPENDIX – The Expression Visitor


Now you are in for it. I think I’ve received about an order of magnitude more requests for this class than for help on building a query provider.
There is an `ExpressionVisitor` class in `System.Linq.Expressions`, however it is internal so it’s not for your direct consumption as much as you’d like it to be.
If you shout real loud you might convince us to make that one public in the next go ‘round.


This expression visitor is my take on the (classic) visitor pattern.
In this variant there is only one visitor class that dispatches calls to the general Visit function out to specific `VisitXXX` methods corresponding to different node types.
Note not every node type gets it own method, for example all binary operators are treated in one `VisitBinary` method.
The nodes themselves do not directly participate in the visitation process. They are treated as just data.
The reason for this is that the quantity of visitors is actually open ended. You can write your own.
Therefore no semantics of visiting is coupled into the node classes.  It’s all in the visitors.
The default visit behavior for node XXX is baked into the base class’s version of VisitXXX.


Another variant is that all `VisitXXX` methods return a node. The `Expression` tree nodes are immutable.
In order to change the tree you must construct a new one. The default `VisitXXX` methods will construct a new node if any of its sub-trees change.
If no changes are made then the same node is returned. 
That way if you make a change to a node (by making a new node) deep down in a tree, the rest of the tree is rebuilt automatically for you.


Here’s the code. Enjoy.


```csharp
public abstract class ExpressionVisitor
{
    protected ExpressionVisitor()
    {
    }

    protected virtual Expression Visit(Expression exp)
    {
        if (exp == null)
            return exp;

        switch (exp.NodeType)
        {
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.ArrayLength:
            case ExpressionType.Quote:
            case ExpressionType.TypeAs:
                return this.VisitUnary((UnaryExpression)exp);

            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.Divide:
            case ExpressionType.Modulo:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.Coalesce:
            case ExpressionType.ArrayIndex:
            case ExpressionType.RightShift:
            case ExpressionType.LeftShift:
            case ExpressionType.ExclusiveOr:
                return this.VisitBinary((BinaryExpression)exp);

            case ExpressionType.TypeIs:
                return this.VisitTypeIs((TypeBinaryExpression)exp);

            case ExpressionType.Conditional:
                return this.VisitConditional((ConditionalExpression)exp);

            case ExpressionType.Constant:
                return this.VisitConstant((ConstantExpression)exp);

            case ExpressionType.Parameter:
                return this.VisitParameter((ParameterExpression)exp);

            case ExpressionType.MemberAccess:
                return this.VisitMemberAccess((MemberExpression)exp);

            case ExpressionType.Call:
                return this.VisitMethodCall((MethodCallExpression)exp);

            case ExpressionType.Lambda:
                return this.VisitLambda((LambdaExpression)exp);

            case ExpressionType.New:
                return this.VisitNew((NewExpression)exp);

            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
                return this.VisitNewArray((NewArrayExpression)exp);

            case ExpressionType.Invoke:
                return this.VisitInvocation((InvocationExpression)exp);

            case ExpressionType.MemberInit:
                return this.VisitMemberInit((MemberInitExpression)exp);

            case ExpressionType.ListInit:
                return this.VisitListInit((ListInitExpression)exp);

            default:
                throw new Exception(string.Format("Unhandled expression type: '{0}'", exp.NodeType));
        }
    }

    protected virtual MemberBinding VisitBinding(MemberBinding binding)
    {
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:
                return this.VisitMemberAssignment((MemberAssignment)binding);

            case MemberBindingType.MemberBinding:
                return this.VisitMemberMemberBinding((MemberMemberBinding)binding);

            case MemberBindingType.ListBinding:
                return this.VisitMemberListBinding((MemberListBinding)binding);

            default:
                throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
        }
    }

    protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
    {
        ReadOnlyCollection<Expression> arguments = this.VisitExpressionList(initializer.Arguments);

        if (arguments != initializer.Arguments)
        {
            return Expression.ElementInit(initializer.AddMethod, arguments);
        }

        return initializer;
    }

    protected virtual Expression VisitUnary(UnaryExpression u)
    {
        Expression operand = this.Visit(u.Operand);

        if (operand != u.Operand)
        {
            return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
        }

        return u;
    }

    protected virtual Expression VisitBinary(BinaryExpression b)
    {
        Expression left = this.Visit(b.Left);
        Expression right = this.Visit(b.Right);
        Expression conversion = this.Visit(b.Conversion);

        if (left != b.Left || right != b.Right || conversion != b.Conversion)
        {
            if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                return Expression.Coalesce(left, right, conversion as LambdaExpression);
            else
                return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
        }

        return b;
    }

    protected virtual Expression VisitTypeIs(TypeBinaryExpression b)
    {
        Expression expr = this.Visit(b.Expression);

        if (expr != b.Expression)
        {
            return Expression.TypeIs(expr, b.TypeOperand);
        }

        return b;
    }

    protected virtual Expression VisitConstant(ConstantExpression c)
    {
        return c;
    }

    protected virtual Expression VisitConditional(ConditionalExpression c)
    {
        Expression test = this.Visit(c.Test);
        Expression ifTrue = this.Visit(c.IfTrue);
        Expression ifFalse = this.Visit(c.IfFalse);

        if (test != c.Test || ifTrue != c.IfTrue || ifFalse != c.IfFalse)
        {
            return Expression.Condition(test, ifTrue, ifFalse);
        }

        return c;
    }

    protected virtual Expression VisitParameter(ParameterExpression p)
    {
        return p;
    }

    protected virtual Expression VisitMemberAccess(MemberExpression m)
    {
        Expression exp = this.Visit(m.Expression);

        if (exp != m.Expression)
        {
            return Expression.MakeMemberAccess(exp, m.Member);
        }

        return m;
    }

    protected virtual Expression VisitMethodCall(MethodCallExpression m)
    {
        Expression obj = this.Visit(m.Object);
        IEnumerable<Expression> args = this.VisitExpressionList(m.Arguments);

        if (obj != m.Object || args != m.Arguments)
        {
            return Expression.Call(obj, m.Method, args);
        }

        return m;
    }

    protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
    {
        List<Expression> list = null;

        for (int i = 0, n = original.Count; i < n; i++)
        {
            Expression p = this.Visit(original[i]);

            if (list != null)
            {
                list.Add(p);
            }
            else if (p != original[i])
            {
                list = new List<Expression>(n);

                for (int j = 0; j < i; j++)
                {
                    list.Add(original[j]);
                }

                list.Add(p);
            }
        }

        if (list != null)
        {
            return list.AsReadOnly();
        }

        return original;
    }

    protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
    {
        Expression e = this.Visit(assignment.Expression);

        if (e != assignment.Expression)
        {
            return Expression.Bind(assignment.Member, e);
        }

        return assignment;
    }

    protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
    {
        IEnumerable<MemberBinding> bindings = this.VisitBindingList(binding.Bindings);

        if (bindings != binding.Bindings)
        {
            return Expression.MemberBind(binding.Member, bindings);
        }

        return binding;
    }

    protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
    {
        IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(binding.Initializers);

        if (initializers != binding.Initializers)
        {
            return Expression.ListBind(binding.Member, initializers);
        }

        return binding;
    }

    protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
    {
        List<MemberBinding> list = null;

        for (int i = 0, n = original.Count; i < n; i++)
        {
            MemberBinding b = this.VisitBinding(original[i]);

            if (list != null)
            {
                list.Add(b);
            }
            else if (b != original[i])
            {
                list = new List<MemberBinding>(n);

                for (int j = 0; j < i; j++)
                {
                    list.Add(original[j]);
                }

                list.Add(b);
            }
        }

        if (list != null)
            return list;

        return original;
    }

    protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
    {
        List<ElementInit> list = null;

        for (int i = 0, n = original.Count; i < n; i++)
        {
            ElementInit init = this.VisitElementInitializer(original[i]);

            if (list != null)
            {
                list.Add(init);
            }
            else if (init != original[i])
            {
                list = new List<ElementInit>(n);

                for (int j = 0; j < i; j++)
                {
                    list.Add(original[j]);
                }

                list.Add(init);
            }
        }

        if (list != null)
            return list;

        return original;
    }

    protected virtual Expression VisitLambda(LambdaExpression lambda)
    {
        Expression body = this.Visit(lambda.Body);

        if (body != lambda.Body)
        {
            return Expression.Lambda(lambda.Type, body, lambda.Parameters);
        }

        return lambda;
    }

    protected virtual NewExpression VisitNew(NewExpression nex)
    {
        IEnumerable<Expression> args = this.VisitExpressionList(nex.Arguments);

        if (args != nex.Arguments)
        {
            if (nex.Members != null)
                return Expression.New(nex.Constructor, args, nex.Members);
            else
                return Expression.New(nex.Constructor, args);
        }

        return nex;
    }

    protected virtual Expression VisitMemberInit(MemberInitExpression init)
    {
        NewExpression n = this.VisitNew(init.NewExpression);
        IEnumerable<MemberBinding> bindings = this.VisitBindingList(init.Bindings);

        if (n != init.NewExpression || bindings != init.Bindings)
        {
            return Expression.MemberInit(n, bindings);
        }

        return init;
    }

    protected virtual Expression VisitListInit(ListInitExpression init)
    {
        NewExpression n = this.VisitNew(init.NewExpression);
        IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(init.Initializers);

        if (n != init.NewExpression || initializers != init.Initializers)
        {
            return Expression.ListInit(n, initializers);
        }

        return init;
    }

    protected virtual Expression VisitNewArray(NewArrayExpression na)
    {
        IEnumerable<Expression> exprs = this.VisitExpressionList(na.Expressions);

        if (exprs != na.Expressions)
        {
            if (na.NodeType == ExpressionType.NewArrayInit)
            {
                return Expression.NewArrayInit(na.Type.GetElementType(), exprs);
            }
            else
            {
                return Expression.NewArrayBounds(na.Type.GetElementType(), exprs);
            }
        }

        return na;
    }

    protected virtual Expression VisitInvocation(InvocationExpression iv)
    {
        IEnumerable<Expression> args = this.VisitExpressionList(iv.Arguments);
        Expression expr = this.Visit(iv.Expression);

        if (args != iv.Arguments || expr != iv.Expression)
        {
            return Expression.Invoke(expr, args);
        }

        return iv;
    }
}
```