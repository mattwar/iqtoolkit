# LINQ: Building an IQueryable Provider – Part X: GroupBy and Aggregates

Matt Warren - MSFT; July 8, 2008

--------------------------------


This is the tenth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you'll want to find a nice shady tree, relax and meditate on why your world is so confused and full of meaningless tasks that it has kept you from pursuing the perfection of writing IQueryable providers.


Complete list of posts in the Building an IQueryable Provider series 


Last time I blamed the television writers strike for delaying me in getting out my next installment.  This time I blame the lack of one, and sunny days, and my son riding his bicycle, and the grueling, tiresome task of getting paid.  Would you believe some of the code we are writing has nothing what-so-ever to do with IQueryable LINQ providers? Crazy, I know. Maybe it would be a different story around here if we had a few more shady trees. 


Fortunately for you I have a prime piece of savory source code ready for slow grilling over a bed of hot mesquite. It's something I'd like to say I was saving for a special occasion, but the truth is I've been putting it off because I thought the code would be just too overwhelming. I mean for me, not you. You see I've been trying to do two things with this series; one, provide a guide to help developers navigate and understand the power and flexibility of the IQuerayble interface, and two, attempt to prove to myself that code written in a purely functional style (or as pure as I can make it) can end up much cleaner and easier on the eyes. I've been horrified ever since tackling OrderBy that this subject would become my undoing. Knowing how involved the translation was for LINQ to SQL I was dreading the immutable madness that would ensue.  Obviously, I survived the ordeal and so will you.


Grappling with GroupBy and Aggregates

Translating just GroupBy itself might not be so difficult if I did not have to also account for aggregates. It seems that GroupBy is not very interesting without the ability to write expressions involving Count() and Max() and Sum(), and that's where the difficulty sets in.


A query involving groups and aggregates that looks like this:


var query = from o in db.Orders


            group o by o.CustomerID into g


            select g.Max(o => o.OrderID);


Is translated into a series of method calls that looks like this:


var query = db.Orders.GroupBy(o => o.CustomerID).Select(g => g.Max(o => o.OrderID));


 


And this is a problem because as I translate from the bottom up (as I have been doing all along) building individual SELECT subqueries for each query operator, I won’t even discover the use of the aggregate ‘Max’ until after I’ve created the SELECT with the GROUP BY.  So how do I get the two pieces together in the same place at the same time without violating my functional style and immutable expression nodes?  Weirder yet, how do I even know that a call to an aggregate method should be tied back to a particular GroupBy? What if I had two group-bys?


 


So where do I start to explain?  Maybe the easy stuff first.  I’ve added a GroupBy property to SelectExpression.


 


internal class SelectExpression : Expression {
    ...


    ReadOnlyCollection<Expression> groupBy;


 


    internal SelectExpression(
        ...,


        IEnumerable<Expression> groupBy


        )



    internal ReadOnlyCollection<Expression> GroupBy {


        get { return this.groupBy; }


    }


}


 


Now every place where I construct one I have to specify a collection of grouping expressions (or null).  GroupBy expressions also get visited after everything else (so far).  Here’s the new version of VisitSelect in DbExpressionVisitor.


 


protected virtual Expression VisitSelect(SelectExpression select) {


    Expression from = this.VisitSource(select.From);


    Expression where = this.Visit(select.Where);


    ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);


    ReadOnlyCollection<OrderExpression> orderBy = this.VisitOrderBy(select.OrderBy);


    ReadOnlyCollection<Expression> groupBy = this.VisitExpressionList(select.GroupBy);


    if (from != select.From


        || where != select.Where


        || columns != select.Columns


        || orderBy != select.OrderBy


        || groupBy != select.GroupBy


        ) {


        return new SelectExpression(select.Type, select.Alias, columns, from, where, orderBy, groupBy);


    }


    return select;


}


 


I also added an AggregateExpression class. It represents a call to an aggregate operator.


 


    internal enum AggregateType {


        Count,


        Min,


        Max,


        Sum,


        Average


    }


 


    internal class AggregateExpression : Expression {


        AggregateType aggType;


        Expression argument;


        internal AggregateExpression(Type type, AggregateType aggType, Expression argument)


            : base((ExpressionType)DbExpressionType.Aggregate, type) {


            this.aggType = aggType;


            this.argument = argument;


        }


        internal AggregateType AggregateType {


            get { return this.aggType; }


        }


        internal Expression Argument {


            get { return this.argument; }


        }


    }


 


Now I at least know what to turn those aggregates into.  But how do I get these aggregate expressions into the right place in the query tree?  What if I did nothing and just let the aggregates fall where they may?  What would it look like?  Would it even be legal SQL?


 


SELECT MAX(t5.OrderID)


FROM (


  SELECT t0.CustomerID


  FROM Orders AS t0


  GROUP BY t0.CustomerID


  ) AS t5


 


Oh, no. Danger Will Robinson! This is not legal SQL. Where does OrderID even come from? It’s not even projected out of the sub-query with the GROUP BY. And even if it were somehow projected, the result of the query would be all wrong.  It would give me a single maximum instead of one for each group.  Getting group-by & aggregates to work together is going to be tricky.


 


Is it even possible to solve it and maintain my layering approach?  What about correlated sub-queries?  I could form a sub-query that joins back to the original query based on the grouping expressions and have it contain the aggregate.


 


SELECT (


  SELECT MAX(t2.OrderID)


  FROM Orders AS t2


  WHERE ((t2.CustomerID IS NULL AND t5.CustomerID IS NULL) OR (t2.CustomerID = t5.CustomerID))


  ) AS c0


FROM (


  SELECT t0.CustomerID


  FROM Orders AS t0


  GROUP BY t0.CustomerID


  ) AS t5


 


Now that at least executes on the server. It might not be pretty and likely not efficient unless the server is really good at unscrambling my query, but it does execute and produce the correct results. So it is technically legal. But surely I can do better!


The GroupBy Operator

 


By now you are thinking, OMG, he’s going to stick us with a wacky solution like he did with nested queries!  Never fear, my friend.  Do not look at the man behind the current.  Cast your gaze ahead and all will be made clear. In my hands, I have the GroupBy operator.  Watch as it transforms into a SQL query inside the QueryBinder!


 


    protected override Expression VisitMethodCall(MethodCallExpression m) {


        if (m.Method.DeclaringType == typeof(Queryable) ||


            m.Method.DeclaringType == typeof(Enumerable)) {


            switch (m.Method.Name) {
                ...


                case "GroupBy":


                    if (m.Arguments.Count == 2) {


                        return this.BindGroupBy(


                            m.Arguments[0],


                            (LambdaExpression)StripQuotes(m.Arguments[1]),


                            null,


                            null


                            );


                    }


                    else if (m.Arguments.Count == 3) {


                        return this.BindGroupBy(


                            m.Arguments[0],


                            (LambdaExpression)StripQuotes(m.Arguments[1]),


                            (LambdaExpression)StripQuotes(m.Arguments[2]),


                            null


                            );


                    }


                    else if (m.Arguments.Count == 4) {


                        return this.BindGroupBy(


                            m.Arguments[0],


                            (LambdaExpression)StripQuotes(m.Arguments[1]),


                            (LambdaExpression)StripQuotes(m.Arguments[2]),


                            (LambdaExpression)StripQuotes(m.Arguments[3])


                            );


                    }


                    break;
                ...


            }


        }


        return base.VisitMethodCall(m);


    }


 


As you can see, I’m handling three different variations of GroupBy.  The first one simply takes a single keySelector lambda (the thing to group by).  It is supposed to return a sequence of grouping’s that each have a group key value and a collection of elements that make up that group.  The second form is the same as the first except it also includes an elementSelector lambda that allows me to specify a map between an item in the source sequence and its shape of the elements in the group. The last form includes a resultSelector lambda that allows me to specify what the key and group collection turn into, instead of assuming they always form into instances of IGrouping<K,E>. With all these variations, the translation of group-by is going to get a bit involved, so bear with me.


 


Get ready. Get set. Open your eyes. Close them. Now open them again and really look. Go!


 


    protected virtual Expression BindGroupBy(Expression source, LambdaExpression keySelector, LambdaExpression elementSelector, LambdaExpression resultSelector) {


        ProjectionExpression projection = this.VisitSequence(source);


       


        this.map[keySelector.Parameters[0]] = projection.Projector;


        Expression keyExpr = this.Visit(keySelector.Body);           


 


        Expression elemExpr = projection.Projector;


        if (elementSelector != null) {


            this.map[elementSelector.Parameters[0]] = projection.Projector;


            elemExpr = this.Visit(elementSelector.Body);


        }


       


        // Use ProjectColumns to get group-by expressions from key expression


        ProjectedColumns keyProjection = this.ProjectColumns(keyExpr, projection.Source.Alias, projection.Source.Alias);


        IEnumerable<Expression> groupExprs = keyProjection.Columns.Select(c => c.Expression);


 


        // make duplicate of source query as basis of element subquery by visiting the source again


        ProjectionExpression subqueryBasis = this.VisitSequence(source);


 


        // recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate)


        this.map[keySelector.Parameters[0]] = subqueryBasis.Projector;


        Expression subqueryKey = this.Visit(keySelector.Body);


       


        // use same projection trick to get group-by expressions based on subquery


        ProjectedColumns subqueryKeyPC = this.ProjectColumns(subqueryKey, subqueryBasis.Source.Alias, subqueryBasis.Source.Alias);


        IEnumerable<Expression> subqueryGroupExprs = subqueryKeyPC.Columns.Select(c => c.Expression);


        Expression subqueryCorrelation = this.BuildPredicateWithNullsEqual(subqueryGroupExprs, groupExprs);


       


        // compute element based on duplicated subquery


        Expression subqueryElemExpr = subqueryBasis.Projector;


        if (elementSelector != null) {


            this.map[elementSelector.Parameters[0]] = subqueryBasis.Projector;


            subqueryElemExpr = this.Visit(elementSelector.Body);


        }


       


        // build subquery that projects the desired element


        string elementAlias = this.GetNextAlias();


        ProjectedColumns elementPC = this.ProjectColumns(subqueryElemExpr, elementAlias, subqueryBasis.Source.Alias);


        ProjectionExpression elementSubquery =


            new ProjectionExpression(


                new SelectExpression(TypeSystem.GetSequenceType(subqueryElemExpr.Type), elementAlias, elementPC.Columns, subqueryBasis.Source, subqueryCorrelation),


                elementPC.Projector


                );


 


        string alias = this.GetNextAlias();



        // make it possible to tie aggregates back to this group-by


        GroupByInfo info = new GroupByInfo(alias, elemExpr);


        this.groupByMap.Add(elementSubquery, info);


 


        Expression resultExpr;


        if (resultSelector != null) {


            Expression saveGroupElement = this.currentGroupElement;


            this.currentGroupElement = elementSubquery;


            // compute result expression based on key & element-subquery


            this.map[resultSelector.Parameters[0]] = keyProjection.Projector;


            this.map[resultSelector.Parameters[1]] = elementSubquery;


            resultExpr = this.Visit(resultSelector.Body);


            this.currentGroupElement = saveGroupElement;


        }


        else {


            // result must be IGrouping<K,E>


            resultExpr = Expression.New(


                typeof(Grouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type).GetConstructors()[0],


                new Expression[] { keyExpr, elementSubquery }


                );


        }


 


        ProjectedColumns pc = this.ProjectColumns(resultExpr, alias, projection.Source.Alias);


 


        // make it possible to tie aggregates back to this group-by


        Expression projectedElementSubquery = ((NewExpression)pc.Projector).Arguments[1];


        this.groupByMap.Add(projectedElementSubquery, info);


 


        return new ProjectionExpression(


            new SelectExpression(TypeSystem.GetSequenceType(resultExpr.Type), alias, pc.Columns, projection.Source, null, null, groupExprs),


            pc.Projector


            );


    }


 


It starts off easy, following the same binding pattern as the other operators.  First I bind the key selector and get a key expression. Then if I have an element selector I bind that too, to get an element expression, otherwise I use the projector expression from the incoming source as the element expression. Got it?


 


Now I have to figure out what the grouping expressions are going to be. You might be thinking I already know what they are, and there’s only one of them. It’s the key expression, right? Well, not exactly. It turns out its common to need to group by more than one property or column of data. The only way to do that with LINQ is to construct an anonymous type with multiple parts. So a key might be an anonymous type with multiple interesting fields. How do I turn that into one or more expression that can be evaluated as part of a SQL group by clause?


 


It turns out I already have a handy function that does what I want. If I treat the key expression they same way I treat a projection expression, I can use the ColumnProjector to turn the expression into a list of column declarations, each with its own scalar expression.  Then I can simply throw away the column declarations and keep the expressions that define the columns and voila I have grouping expressions!  Don’t try this at home.  Err, I mean, go ahead and try this at home. It’s easy. Watch, I’ll do it again in a moment.


 


Moving along we come to some code trying to compute a ‘basis’. What’s that?  Well, it turns out SQL doesn’t really support a GroupBy operator the way it is defined in LINQ. The LINQ operator, by default, produces a sequence of groups with each group containing the elements that got designated as part of that group. SQL group-by can only produce aggregate computations over the groups, not the groups themselves.  Oh, dear, what to do, what to do?


 


The only way to solve this problem is to form a self join back to the data I want. If I take the result of the group-by and join it back to the original query (everything up until the group-by) matching group-by key values I get all the data back that SQL was not going to let me have.  That’s what the ‘subqueryBasis’ is.  It’s a rebinding of the original input sequence. Then we rebind the key again and do my ColumnProjector trick to get a new collection of group expressions. Now I can use both sets to form a predicate for a join.


 


Next, you can see that if I did specify an element selector I now bind it with respect to this new ‘basis’ and use the resulting expression as the projection coming out of this new branch of the query tree.


 


Alas, I am not yet done.  What about that third argument?  If it’s specified then I have another lambda I have to do something with and if it is not specified then I need to figure out how I’m going to return an IGrouping<K,E>.


 


Okay, that doesn’t turn out to be too difficult.  I created a Grouping<K,E> class that I can use in case I need to actually return this stuff all the way back to the calling code.


 


    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement> {


        TKey key;


        IEnumerable<TElement> group;


 


        public Grouping(TKey key, IEnumerable<TElement> group) {


            this.key = key;


            this.group = group;


        }


 


        public TKey Key {


            get { return this.key; }


        }


 


        public IEnumerator<TElement> GetEnumerator() {


            return this.group.GetEnumerator();


        }


 


        IEnumerator IEnumerable.GetEnumerator() {


            return this.group.GetEnumerator();


        }


    }


 


Even if I don’t, I can still use this type as a way to encode the fact that I’m returning a grouping, which is what I do. The final result of the call to GroupBy (unless a result selector was supplied) is a sequence of these groupings, so the projector expression resulting from this operation is simply a construction of one of these Grouping<K,E> types via a NewExpression node. If a result selector is supplied that lambda determines the shape of the result so I just bind that the same way I bind all the other lambdas.  In both cases, I’m using the newly created extra ‘basis’ query that projects out elements to form the group collection that is either transformed with the result selector or returned to the caller.


 


That’s about it; clean and neat. Right? What about those other few lines of code referring to ‘GroupByInfo’ and ‘currentGroupElement’? That, sir, is the man behind the current. Now I’ll show you a little of him.


 


Later, when I’m binding aggregates I will need to invent a way to tie those aggregates back to this group-by; if they belong to this group-by.  So, later, I’m going to need to know something about this group-by (and certainly all group-by’s that might have already been encountered) to be able to pick the right one.  This is what the GroupByInfo is.  It’s the bits of information I hope will be useful later to correlate the two pieces together.  If we move along now to aggregate binding I’ll show you how it’s used.


Aggregate Binding

 


Aggregates get treated just like any other operator.


 


    protected override Expression VisitMethodCall(MethodCallExpression m) {


        if (m.Method.DeclaringType == typeof(Queryable) ||


            m.Method.DeclaringType == typeof(Enumerable)) {
            ...


            switch (m.Method.Name) {


                case "Count":


                case "Min":


                case "Max":


                case "Sum":


                case "Average":


                    if (m.Arguments.Count == 1) {


                        return this.BindAggregate(m.Arguments[0], m.Method, null);


                    }


                    else if (m.Arguments.Count == 2) {


                        LambdaExpression selector = (LambdaExpression)StripQuotes(m.Arguments[1]);


                        return this.BindAggregate(m.Arguments[0], m.Method, selector);


                    }


                    break;


            }


        }


        return base.VisitMethodCall(m);


    }


 


The actual binding has a few extra bits that help make the job a bit easier.


 


    Expression currentGroupElement;


 


    class GroupByInfo {


        internal string Alias { get; private set; }


        internal Expression Element { get; private set; }


        internal GroupByInfo(string alias, Expression element) {


            this.Alias = alias;


            this.Element = element;


        }


    }


 


    private AggregateType GetAggregateType(string methodName) {


        switch (methodName) {


            case "Count": return AggregateType.Count;


            case "Min": return AggregateType.Min;


            case "Max": return AggregateType.Max;


            case "Sum": return AggregateType.Sum;


            case "Average": return AggregateType.Average;


            default: throw new Exception(string.Format("Unknown aggregate type: {0}", methodName));


        }


    }


 


    private bool HasPredicateArg(AggregateType aggregateType) {


        return aggregateType == AggregateType.Count;


    }


 


    private Expression BindAggregate(Expression source, MethodInfo method, LambdaExpression argument, bool isRoot) {


        Type returnType = method.ReturnType;


        AggregateType aggType = this.GetAggregateType(method.Name);


        bool hasPredicateArg = this.HasPredicateArg(aggType);


 


        if (argument != null && hasPredicateArg) {


            // convert query.Count(predicate) into query.Where(predicate).Count()


            source = Expression.Call(typeof(Queryable), "Where", method.GetGenericArguments(), source, argument);


            argument = null;


        }


 


        ProjectionExpression projection = this.VisitSequence(source);


 


        Expression argExpr = null;


        if (argument != null) {


            this.map[argument.Parameters[0]] = projection.Projector;


            argExpr = this.Visit(argument.Body);


        }


        else if (!hasPredicateArg) {


            argExpr = projection.Projector;


        }


 


        string alias = this.GetNextAlias();


        var pc = this.ProjectColumns(projection.Projector, alias, projection.Source.Alias);


        Expression aggExpr = new AggregateExpression(returnType, aggType, argExpr);


        Type selectType = typeof(IEnumerable<>).MakeGenericType(returnType);


        SelectExpression select = new SelectExpression(selectType, alias, new ColumnDeclaration[] { new ColumnDeclaration("", aggExpr) }, projection.Source, null);


 


        if (isRoot) {


            ParameterExpression p = Expression.Parameter(selectType, "p");


            LambdaExpression gator = Expression.Lambda(Expression.Call(typeof(Enumerable), "Single", new Type[] { returnType }, p), p);


            return new ProjectionExpression(select, new ColumnExpression(returnType, alias, ""), gator);


        }


 


        SubqueryExpression subquery = new SubqueryExpression(returnType, select);


 


        // if we can find the corresponding group-info we can build a special AggregateSubquery node that will enable us to


        // optimize the aggregate expression later using AggregateRewriter


        GroupByInfo info;


        if (!hasPredicateArg && this.groupByMap.TryGetValue(projection, out info)) {


            // use the element expression from the group-by info to rebind the argument so the resulting expression is one that


            // would be legal to add to the columns in the select expression that has the corresponding group-by clause.


            if (argument != null) {


                this.map[argument.Parameters[0]] = info.Element;


                argExpr = this.Visit(argument.Body);


            }


            else {


                argExpr = info.Element;


            }


            aggExpr = new AggregateExpression(returnType, aggType, argExpr);


 


            // check for easy to optimize case.  If the projection that our aggregate is based on is really the 'group' argument from


            // the query.GroupBy(xxx, (key, group) => yyy) method then whatever expression we return here will automatically


            // become part of the select expression that has the group-by clause, so just return the simple aggregate expression.


            if (projection == this.currentGroupElement)


                return aggExpr;


 


            return new AggregateSubqueryExpression(info.Alias, aggExpr, subquery);


        }


 


        return subquery;


    }


 


Binding aggregates has its own slew of caveats that drive how this binding function is formed.  Aggregates can be specified with our without an argument. Normally, if an aggregate is not specified with an argument it is implied that the aggregate is operating over the element of the sequence. 


 


For example, it is legal to write:


 


    db.Orders.Select(o => o.OrderID).Max();


 


It has the same meaning as:


 


    db.Orders.Max(o => o.OrderID);


 


So if an aggregate invocation has no argument, I can simply pick up and use the projector expression coming out of the source sequence, right?  Well, not when the aggregate is Count. The LINQ Count aggregate takes a predicate as an argument not a value expression.  So I definitely don’t want to treat Count with an argument the same way I treat other aggregates.  For Count, I want to take that argument and turn it into a WHERE clause. I do that near the top of the BindAggregate function by tacking on a call to Queryable.Where right onto the source before I even translate it.


 


The next thing I do is actually create the AggregateExpression and stuff it into a column of a new SelectExpression. Whoa! Stop right there. Didn’t I want to avoid this?  Don’t I want this aggregate ending up alongside the group-by?  Yes, but not just yet.


 


What happens is that the source expression, once translated, already is a correlated sub-query!  How did that happen?  Take a moment to consider what the source of the aggregate really is.  Go ahead, I’ll wait.  Got it yet?   It’s the silly ‘g’.  Ugh.


 


Remember this query?


 


var query = db.Orders.GroupBy(o => o.CustomerID).Select(g => g.Max(o => o.OrderID));


 


Now do you see it?  The ‘g’ is the parameter to the Select operator.  Where does it come from? Yes, that’s right. It comes from the output of the GroupBy operator.  And what was that?  Yes, right again.  It was a sequence of Grouping<K,E> instances.  So a single ‘g’ is a single Grouping<K,E> instance.  Which itself is a collection of grouped items, which was formed using that basis query back in the BindGroupBy method, which was a self-join back to the original query with a join condition based on matching up grouping key expressions. Yes, that query, and yes, replicated here, in the context of an aggregate expression, it is indistinguishable from a correlated sub-query.  So I simply add my extra aggregate expression on top and, presto change-o, I am finished! 


 


Well, not really.  Skipping over this next bit about “isRoot” you can see that I create a SubqueryExpression.  I have not talked about this yet, but this is how I represent a true correlated sub-query. 


 


And here it is in all its majestic glory.


 


    internal class SubqueryExpression : Expression {


        SelectExpression select;


        internal SubqueryExpression(Type type, SelectExpression select)


            : base((ExpressionType)DbExpressionType.Subquery, type) {


            this.select = select;


        }


        internal SelectExpression Select {


            get { return this.select; }


        }


    }


 


Okay, that was a tad anti-climatic. Get over it.  It comes in handy later.


 


So what’s the rest of the method doing?  Ah, yes, you’ve seen it. This is where I look to see if I can tie the aggregate back to the group-by that it’s related to.  I do this by looking up the GroupByInfo based on the source projection; again that’s the ‘g’.  If the ‘g’ translates into the same expression that I created back in the BindGroupBy method then I know it’s the same one.  Back in that method I stored a memento of the group-by keyed by this very same expression, and now I’m pulling this information back up.


 


What you don’t see me doing here is actually going back and changing the SelectExpression that contains the group-by. Believe me; I really want to, but not just yet.  Instead, what I do now is drop a breadcrumb into the query tree so that I can later follow the trail in one sweeping tree rewriting operation. To do this, I have invented yet another expression node to act as this breadcrumb.


 


    internal class AggregateSubqueryExpression : Expression {


        string groupByAlias;


        Expression aggregateInGroupSelect;


        SubqueryExpression aggregateAsSubquery;


        internal AggregateSubqueryExpression(string groupByAlias, Expression aggregateInGroupSelect, SubqueryExpression aggregateAsSubquery)


            : base((ExpressionType)DbExpressionType.AggregateSubquery, aggregateAsSubquery.Type)


        {


            this.aggregateInGroupSelect = aggregateInGroupSelect;


            this.groupByAlias = groupByAlias;


            this.aggregateAsSubquery = aggregateAsSubquery;


        }


        internal string GroupByAlias { get { return this.groupByAlias; } }


        internal Expression AggregateInGroupSelect { get { return this.aggregateInGroupSelect; } }


        internal SubqueryExpression AggregateAsSubquery { get { return this.aggregateAsSubquery; } }


    }


 


What this node does is hold onto two possible futures. Either, the sweeping rewrite is going to move the aggregate expression back into the select with the group-by or its going to abandon all hope and fall back to the correlated sub-query approach.  That’s why this node holds onto two different expressions. One is the sub-query node I’ve already created.  The other is an expression that is fit and ready to become a citizen of the group.  I get this expression by simply rebinding the argument relative to the element expression I stuffed into the GroupByInfo when binding the GroupBy.  Now I stick both of the variations into a AggregateSubqueryExpression (reminding myself that unless I rewrite this the aggregate is going to be interpreted as a correlated subquery.)


 


What about ‘currentGroupElement’?  How does this factor in?  Recall that there was a form of GroupBy that took as an argument the result selector. This is not the form that C# converts query expressions into (but VB does when you use the Aggregate clause).  If the aggregate expression is being bound as part of the result selector then whatever expression I return from this method is going to end up in the same select expression with the GroupBy, no need to optimize later.  So if this is the case I simply have the method return the optimal aggregate expression and forget all about the sub-query nonsense. 


 


That just leaves us with ‘isRoot.’  Explaining isRoot is simple.  If the aggregate operator itself is the root of the tree, and in this case I mean with operator on top, or the ‘last’ operator, then it’s a very different kind of aggregate than normal. It is not associated with a group-by. It is a top-level aggregate. 


 


I get these when I type things like:


 


    db.Orders.Max(o => o.OrderID);


 


Now instead of returning a sequence of aggregates, I need only one for the whole sequence.  However, the query provider still gets back a sequence of rows from ADO and the ProjectionReader is still an IEnumerable of whatever the returned element type is.  In this case it is IEnumerable<int>.  Yet, I can clearly not return that IEnumerable as the result of the execution of the query. I need just a single int. So I need a way to tell the provider how to get the one true int out of the sequence.  That’s what I’m doing here.  I’ve added an extra lambda to the ProjectionExpression that tells the provider later how to aggregate the sequence back into a singleton. The ProjectionExpression already has a ‘projector’ that tells the provider how to turn DataReader rows into objects.  Now is also has an ‘aggregator’ that tells the provider how to turn the entire result set into a single value.  The aggregator I construct calls the Enumerable.Single method. 


 


That’s really how it all works, except for the real magic, the part where I wave a wand and polymorph the ugly tree into the beautiful one.  That’s the aggregate rewriter.


Aggregate Rewriting

 


Are you ready for it?  This is the part that does all the truly maligned and dishonest shuffling of bits.


 


    internal class AggregateRewriter : DbExpressionVisitor {


        ILookup<string, AggregateSubqueryExpression> lookup;


        Dictionary<AggregateSubqueryExpression, Expression> map;


 


        private AggregateRewriter(Expression expr) {


            this.map = new Dictionary<AggregateSubqueryExpression, Expression>();


            this.lookup = AggregateGatherer.Gather(expr).ToLookup(a => a.GroupByAlias);


        }


 


        internal static Expression Rewrite(Expression expr) {


            return new AggregateRewriter(expr).Visit(expr);


        }


 


        protected override Expression VisitSelect(SelectExpression select) {


            select = (SelectExpression)base.VisitSelect(select);


            if (lookup.Contains(select.Alias)) {


                List<ColumnDeclaration> aggColumns = new List<ColumnDeclaration>(select.Columns);


                foreach (AggregateSubqueryExpression ae in lookup[select.Alias]) {


                    string name = "agg" + aggColumns.Count;


                    ColumnDeclaration cd = new ColumnDeclaration(name, ae.AggregateInGroupSelect);


                    this.map.Add(ae, new ColumnExpression(ae.Type, ae.GroupByAlias, name));


                    aggColumns.Add(cd);


                }


                return new SelectExpression(select.Type, select.Alias, aggColumns, select.From, select.Where, select.OrderBy, select.GroupBy);


            }


            return select;


        }


 


        protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate) {


            Expression mapped;


            if (this.map.TryGetValue(aggregate, out mapped)) {


                return mapped;


            }


            return this.Visit(aggregate.AggregateAsSubquery);


        }


 


        class AggregateGatherer : DbExpressionVisitor {


            List<AggregateSubqueryExpression> aggregates = new List<AggregateSubqueryExpression>();


            private AggregateGatherer() {


            }


 


            internal static List<AggregateSubqueryExpression> Gather(Expression expression) {


                AggregateGatherer gatherer = new AggregateGatherer();


                gatherer.Visit(expression);


                return gatherer.aggregates;


            }


 


            protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate) {


                this.aggregates.Add(aggregate);


                return base.VisitAggregateSubquery(aggregate);


            }


        }


    }


 


That was it.  No, really.  That was the hard part.  Look closer, you’ll see where it actually does something.


 


The AggregateRewriter is actually two visitors; the primary rewriting visitor and a secondary gatherer that builds a collection of all the reachable aggregate subqueries.  See, I told you having this new node would come in handy.


 


What is actually going on is first all the aggregate expressions are gathered up and formed into a lookup table keyed off the group-by alias.  Then during the rewrite, when I get to the select expression with that alias I simply tack on all the aggregate expressions by inventing columns for them.  On the side I keep a mapping between all the original aggregate sub-queries and a column-expression that references the newly declared column.  A new select expression is created with the new set of columns, triggering a cascade of tree rebuilding (keeping everything nice and immutable.)  Then, when the aggregate sub-query is visited later, if it’s found in the map I simply replace it with the new column expression.


 


And now the aggregate expressions are in the right place.  Peace, harmony and all is well with the world.


 


I can see you are now impressed with my mad wizard-like skillz.


 


Let’s put it all together.  I add the aggregate rewriter to the pipeline of visitors we have in DbQueryProvider.Translate().


 


    private TranslateResult Translate(Expression expression) {


        ProjectionExpression projection = expression as ProjectionExpression;


        if (projection == null) {


            expression = Evaluator.PartialEval(expression, CanBeEvaluatedLocally);


            expression = QueryBinder.Bind(this, expression);


            expression = AggregateRewriter.Rewrite(expression);


            expression = OrderByRewriter.Rewrite(expression);


            expression = UnusedColumnRemover.Remove(expression);


            expression = RedundantSubqueryRemover.Remove(expression);


            projection = (ProjectionExpression)expression;


        }


        string commandText = QueryFormatter.Format(projection.Source);


        string[] columns = projection.Source.Columns.Select(c => c.Name).ToArray();


        LambdaExpression projector = ProjectionBuilder.Build(projection.Projector, projection.Source.Alias, columns);


        return new TranslateResult(commandText, projector, projection.Aggregator);


    }


 


Notice the new ‘aggregator’ being passed along in the result. The Execute method now makes use of it.



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


       


        IEnumerable sequence = (IEnumerable) Activator.CreateInstance(


            typeof(ProjectionReader<>).MakeGenericType(elementType),


            BindingFlags.Instance | BindingFlags.NonPublic, null,


            new object[] { reader, projector, this },


            null


            );


 


        if (query.Aggregator != null) {


            Delegate aggregator = query.Aggregator.Compile();


            AggregateReader aggReader = (AggregateReader) Activator.CreateInstance(


                typeof(AggregateReader<,>).MakeGenericType(elementType, query.Aggregator.Body.Type),


                BindingFlags.Instance | BindingFlags.NonPublic, null,


                new object[] { aggregator },


                null


                );


            return aggReader.Read(sequence);


        }


        else {


            return sequence;


        }


    }


Taking it for a Spin

 


Let’s give the provider a big soupy group-by ball of aggravation and see how it behaves.


 


    var query = from o in db.Orders


                group o by o.CustomerID into g


                select new {


                    Customer = g.Key,


                    Total = g.Sum(o => o.OrderID),


                    Min = g.Min(o => o.OrderID),


                    Avg = g.Average(o => o.OrderID)


                };


 


This query translates into:


 


SELECT t0.CustomerID, SUM(t0.OrderID) AS agg1, MIN(t0.OrderID) AS agg2, AVG(t0.OrderID) AS agg3


FROM Orders AS t0


GROUP BY t0.CustomerID


 


The results of execution are:


 


{ Customer = ALFKI, Total = 64835, Min = 10643, Avg = 10805 }


{ Customer = ANATR, Total = 42618, Min = 10308, Avg = 10654 }


{ Customer = ANTON, Total = 74195, Min = 10365, Avg = 10599 }


{ Customer = AROUT, Total = 139254, Min = 10355, Avg = 10711 }


...


{ Customer = WILMK, Total = 75650, Min = 10615, Avg = 10807 }


{ Customer = WOLZA, Total = 75595, Min = 10374, Avg = 10799 }


 


Don’t ask me why I think it is interesting to total up the OrderID’s.  I’m just weird.


 


That’s it.  Really.  This time I mean it.  I’m done.  If you want more take a look at the source code attached.  All the gory details are in there, along with a bazillion minor bug fixes I made since the last installment. 


 


DISCLAIMER:  The provided solution for GroupBy works well for many simple cases, such as when the aggregate expressions immediately follow the GroupBy operator. It is possible to write queries that produce the correlated sub-query form. This is normal. There is definitely still room for improvement.


 


Now, get out from under that shady tree and get back to writing code.  You know I am.

