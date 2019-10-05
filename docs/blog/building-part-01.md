# LINQ: Building an IQueryable Provider - Part I: Reusable IQueryable base classes

Matt Warren - MSFT; July 30, 2007

This is the first in a series of posts on  how to build a LINQ IQueryable provider.  Each post builds on the last one.

---

I’ve been meaning for a while to start up a series of posts that covers building LINQ providers using `IQueryable`.
People have been asking me advice on doing this for quite some time now, whether through internal Microsoft email 
or questions on the forums or by cracking the encryption and mailing me directly.
Of course, I’ve mostly replied with “I’m working on a sample that will show you everything” letting them know that soon all will be revealed.
However, instead of just posting a full sample here I felt it prudent to go step by step so I can actual dive deep and 
explain everything that is going on instead of just dumping it all in your lap and letting you find your own way.

The first thing I ought to point out to you is that `IQueryable` has changed in Beta 2.
It’s no longer just one interface, having been factored into two: `IQueryable` and `IQueryProvider`.
Let’s just walk through these before we get to actually implementing them.


If you use Visual Studio to ‘go to definition’ you get something that looks like this: 

```csharp
    public interface IQueryable : IEnumerable
    {
        Type ElementType { get; }
        Expression Expression { get; }
        IQueryProvider Provider { get; }
    }

    public interface IQueryable<T> : IEnumerable<T>, IQueryable, IEnumerable
    {
    }
```

Of course, `IQueryable` no longer looks all that interesting; the good stuff has been pushed off into the new interface `IQueryProvider`.
Yet before I get into that, IQueryable is still worth looking at.  As you can see the only things `IQueryable` has are three read-only properties.
The first one gives you the element type (or the ‘T’ in `IQueryable<T>`).  
It’s important to note that all classes that implement IQueryable must also implement `IQueryable<T>` for some T and vice versa.
The generic `IQueryable<T>` is the one you use most often in method signatures and the like.
The non-generic IQueryable exist primarily to give you a weakly typed entry point primarily for dynamic query building scenarios.


The second property gives you the expression that corresponds to the query.
This is quintessential essence of IQueryable’s being.
The actual ‘query’ underneath the hood of an  `IQueryable` is an expression that represents the query as a tree of LINQ query operators/method calls.
This is the part of the `IQueryable` that your provider must comprehend in order to do anything useful.
If you look deeper you will see that the whole `IQueryable` infrastructure (including the `System.Linq.Queryable` version of LINQ standard query operators)
is just a mechanism to auto-construct expression tree nodes for you.
When you use the `Queryable.Where` method to apply a filter to an `IQueryable`, 
it simply builds you a new `IQueryable` adding a method-call expression node on top of the tree representing the call you just made to `Queryable.Where`.


Don’t believe me? Try it yourself and see what it does.


Now that just leaves us with the last property that gives us an instance of this new interface `IQueryProvider`.
What we’ve done is move all the methods that implement constructing new IQueryables and executing them off into a separate interface that more logically represents your true provider.

```csharp
    public interface IQueryProvider
    {
        IQueryable CreateQuery(Expression expression);
        IQueryable<TElement> CreateQuery<TElement>(Expression expression);
        object Execute(Expression expression);
        TResult Execute<TResult>(Expression expression);
    }
```

Looking at the `IQueryProvider` interface you might be thinking, “why all these methods?”
The truth is that there are really only two operations, `CreateQuery` and `Execute`, we just have both a generic and a non-generic form of each.
The generic forms are used most often when you write queries directly in the programming language and perform better since we can avoid using reflection to construct instances.


The CreateQuery method does exactly what it sounds like it does.
It creates a new instance of an IQueryable query based on the specified expression tree.
When someone calls this method they are basically asking your provider to build a new instance of an IQueryable that when enumerated will invoke your query provider and process this specific query expression.
The Queryable form of the standard query operators use this method to construct new IQueryable’s that stay associated with your provider.
Note the caller can pass any expression tree possible to this API. It may not even be a legal query for your provider.
However, the only thing that must be true is that expression itself must be typed to return/produce a correctly typed IQueryable.
You see the IQueryable contains an expression that represents a snippet of code that if turned into actual code and executed would reconstruct that very same IQueryable (or its equivalent).


The Execute method is the entry point into your provider for actually executing query expressions.
Having an explicit execute instead of just relying on IEnumerable.GetEnumerator() is important because it allows execution of expressions that do not necessarily yield sequences.
For example, the query “myquery.Count()” returns a single integer.
The expression tree for this query is a method call to the Count method that returns the integer.
The Queryable.Count method (as well as the other aggregates and the like) use this method to execute the query ‘right now’.


There, that doesn’t seem so frightening does it?
You could implement all those methods easily, right?
Sure you could, but why bother.
I’ll do it for you.
Well all except for the execute method.
I’ll show you how to do that in a later post.


First let’s start with the `IQuerayble`.
Since this interface has been split into two, it’s now possible to implement the `IQueryable` part just once and re-use it for any provider.
I’ll implement a class called `Query<T>` that implements `IQueryable<T>` and all the rest.


```csharp
    public class Query<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable {
        QueryProvider provider;
        Expression expression;

        public Query(QueryProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException("provider");

            this.provider = provider;
            this.expression = Expression.Constant(this);
        }

        public Query(QueryProvider provider, Expression expression)
        {
            if (provider == null) 
                throw new ArgumentNullException("provider");

            if (expression == null)
                throw new ArgumentNullException("expression");

            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
                throw new ArgumentOutOfRangeException("expression");

            this.provider = provider;
            this.expression = expression;
        }

        Expression IQueryable.Expression
        {
            get { return this.expression; }
        }

        Type IQueryable.ElementType
        {
            get { return typeof(T); }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return this.provider; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.provider.Execute(this.expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.provider.Execute(this.expression)).GetEnumerator();
        }

        public override string ToString()
        {
            return this.provider.GetQueryText(this.expression);
        }
    }
````

As you can see now, the IQueryable implementation is straightforward.
This little object really does just hold onto an expression tree and a provider instance. The provider is where it really gets juicy.


Okay, now I need some provider to show you.  I’ve implemented an abstract base class called QueryProvider that `Query<T>` referred to above.
A real provider can just derive from this class and implement the Execute method.


```csharp
    public abstract class QueryProvider : IQueryProvider
    {
        protected QueryProvider()
        {
        }

        IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
        {
            return new Query<S>(this, expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);

            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        S IQueryProvider.Execute<S>(Expression expression)
        {
            return (S)this.Execute(expression);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return this.Execute(expression);
        }

        public abstract string GetQueryText(Expression expression);

        public abstract object Execute(Expression expression);
    }
```


I’ve implemented the IQueryProvider interface on my base class `QueryProvider`.
The `CreateQuery` methods create new instances of `Query<T>` and the Execute methods forward execution to this great new and not-yet-implemented Execute method.


I suppose you can think of this as boilerplate code you have to write just to get started building a LINQ IQueryable provider.
The real action happens inside the Execute method.
That’s where your provider has the opportunity to make sense of the query by examining the expression tree.


And that’s what I’ll start showing next time.


<br/>

## UPDATE:


It looks like I’ve forget to define a little helper class my implementation was using, so here it is:

``` csharp
    internal static class TypeSystem 
    {
        internal static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null) return seqType;
            return ienum.GetGenericArguments()[0];
        }

        private static Type FindIEnumerable(Type seqType) 
        {
            if (seqType == null || seqType == typeof(string))
                return null;

            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());

            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                        return ienum;
                }
            }

            Type[] ifaces = seqType.GetInterfaces();

            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces) 
                {
                    Type ienum = FindIEnumerable(iface);

                    if (ienum != null)
                        return ienum;
                }
            }

            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }

            return null;
        }
    }
```

Yah, I know. There’s more ‘code’ in this helper than in all the rest.


Sigh.

