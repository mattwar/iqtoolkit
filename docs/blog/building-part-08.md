# LINQ: Building an IQueryable Provider – Part VIII: OrderBy

Matt Warren - MSFT; October 9, 2007

---

This is the eighth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts you might want to shut off the Halo 3 and get crackin'.
Complete list of posts in the Building an IQueryable Provider series 
This post has a been a few weeks in coming. I realize that many of you have been anxiously awaiting the next installment. Your custom LINQ providers have been left alone collecting dust when they'd really rather be out on the town dazzling passers-by.


Implementing OrderBy

Today's topic is translating those order-by clauses.  Fortunately, there is only one way to do ordering, and that's using the LINQ ordering specific query operators. The bad news is that there are four different operators. 


When I write a query using the query syntax ordering looks simple.  It's just one clause.

    var query = from c in db.Customers

                orderby c.Country, c.City

                select c;

However, when that syntax is translated into LINQ there is more than one LINQ operator involved.

    var query = db.Customers.OrderBy(c => c.Country).ThenBy(c => c.City);

In fact, there is one ordering operator per ordering expression specified.  So LINQ providers translating to SQL will need to convert this pattern of individual operators back into a single clause.  The code to do this is going to be just a wee bit more involved than the previous operator translations, primarily because it needs to gather all these expressions up before it can do anything with them.  The prior operators had the luxury of being able to simply build a new outer select layer over the previous query accumulation.  They only ever had to consider their immediate arguments.  Ordering, instead, must delve deep into the otherness and understand more than just itself. 


The first thing I'm going to need is a way to represent the order-by clause.  The simplest thing to do is simply add this ordering stuff right into the SelectExpression I already have.  Yet, since each ordering expression can have a direction, ascending or descending, I'm going to need to remember that too.


So I'm going to add these new definitions:

    internal enum OrderType {

        Ascending,

        Descending

    }

    internal class OrderExpression {

        OrderType orderType;

        Expression expression;

        internal OrderExpression(OrderType orderType, Expression expression) {

            this.orderType = orderType;

            this.expression = expression;

        }

        internal OrderType OrderType {

            get { return this.orderType; }

        }

        internal Expression Expression {

            get { return this.expression; }

        }

    }



The new type 'OrderExpression' is not really an Expression node, since I'm not expecting to be able to stick one anywhere in the tree.  They will only every appear in a SelectExpression as part of its definition.  Therefore, SelectExpression needs to change a little bit.

    internal class SelectExpression : Expression {
        ...

        ReadOnlyCollection<OrderExpression> orderBy;

        internal SelectExpression(

            Type type, string alias, IEnumerable<ColumnDeclaration> columns,

            Expression from, Expression where, IEnumerable<OrderExpression> orderBy)

            : base((ExpressionType)DbExpressionType.Select, type) {
            ...

            this.orderBy = orderBy as ReadOnlyCollection<OrderExpression>;

            if (this.orderBy == null && orderBy != null) {

                this.orderBy = new List<OrderExpression>(orderBy).AsReadOnly();

            }

        }
        ...

        internal ReadOnlyCollection<OrderExpression> OrderBy {

            get { return this.orderBy; }

        }

    }



Of course, I need to update DbExpressionVisitor to know about this ordering stuff:

    internal class DbExpressionVisitor : ExpressionVisitor {
        ...

        protected virtual Expression VisitSelect(SelectExpression select) {

            Expression from = this.VisitSource(select.From);

            Expression where = this.Visit(select.Where);

            ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);

            ReadOnlyCollection<OrderExpression> orderBy = this.VisitOrderBy(select.OrderBy);

            if (from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy) {

                return new SelectExpression(select.Type, select.Alias, columns, from, where, orderBy);

            }

            return select;

        }
        ...
        protected ReadOnlyCollection<OrderExpression> VisitOrderBy(ReadOnlyCollection<OrderExpression> expressions) {

            if (expressions != null) {

                List<OrderExpression> alternate = null;

                for (int i = 0, n = expressions.Count; i < n; i++) {

                    OrderExpression expr = expressions[i];

                    Expression e = this.Visit(expr.Expression);

                    if (alternate == null && e != expr.Expression) {

                        alternate = expressions.Take(i).ToList();

                    }

                    if (alternate != null) {

                        alternate.Add(new OrderExpression(expr.OrderType, e));

                    }

                }

                if (alternate != null) {

                    return alternate.AsReadOnly();

                }

            }

            return expressions;

        }

    }

And I have to fix up the other places in the code that I construct SelectExpressions, but that's relatively easy. 


Converting order-by clauses back to text is also not so bad.

    internal class QueryFormatter : DbExpressionVisitor {
        ...

        protected override Expression VisitSelect(SelectExpression select) {
            ...

            if (select.OrderBy != null && select.OrderBy.Count > 0) {

                this.AppendNewLine(Indentation.Same);

                sb.Append("ORDER BY ");

                for (int i = 0, n = select.OrderBy.Count; i < n; i++) {

                    OrderExpression exp = select.OrderBy[i];

                    if (i > 0) {

                        sb.Append(", ");

                    }

                    this.Visit(exp.Expression);

                    if (exp.OrderType != OrderType.Ascending) {

                        sb.Append(" DESC");

                    }

                }

            }
            ...

        }
        ...

    }


The heavy lifting comes in the QueryBinder where I need to build up the ordering clause out of these method calls.  What I decided to do was build up a list of these ordering expressions and evaluate them all at once.  Since ThenBy and ThenByDescending operators must follow other ordering operators, its easy to walk down the tree, throwing each of these in a collection until I reach the root of the order-by clause, a call to OrderBy or OrderByDescending.

    internal class QueryBinder : ExpressionVisitor {







        ...
        protected override Expression VisitMethodCall(MethodCallExpression m) {

            if (m.Method.DeclaringType == typeof(Queryable) ||

                m.Method.DeclaringType == typeof(Enumerable)) {
                ...

                switch (m.Method.Name) {

                    case "OrderBy":

                        return this.BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending);

                    case "OrderByDescending":

                        return this.BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending);

                    case "ThenBy":

                        return this.BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending);

                    case "ThenByDescending":

                        return this.BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending);

                }

            }
            ...

        }


        List<OrderExpression> thenBys;

        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType) {

            List<OrderExpression> myThenBys = this.thenBys;

            this.thenBys = null;

            ProjectionExpression projection = (ProjectionExpression)this.Visit(source);


            this.map[orderSelector.Parameters[0]] = projection.Projector;

            List<OrderExpression> orderings = new List<OrderExpression>();

            orderings.Add(new OrderExpression(orderType, this.Visit(orderSelector.Body)));


            if (myThenBys != null) {

                for (int i = myThenBys.Count - 1; i >= 0; i--) {

                    OrderExpression tb = myThenBys[i];

                    LambdaExpression lambda = (LambdaExpression)tb.Expression;

                    this.map[lambda.Parameters[0]] = projection.Projector;

                    orderings.Add(new OrderExpression(tb.OrderType, this.Visit(lambda.Body)));

                }

            }


            string alias = this.GetNextAlias();

            ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, projection.Source.Alias);

            return new ProjectionExpression(

                new SelectExpression(resultType, alias, pc.Columns, projection.Source, null, orderings.AsReadOnly()),

                pc.Projector

                );

        }


        protected virtual Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType) {

            if (this.thenBys == null) {

                this.thenBys = new List<OrderExpression>();

            }

            this.thenBys.Add(new OrderExpression(orderType, orderSelector));

            return this.Visit(source);

        }

        ...

    }



When a call to BindThenBy is made (used for ThenBy's and ThenByDescending's), the call's arguments are just appended to a growing list of then-by info. I re-use the OrderExpression to store the then-by's since its the same layout. Later, when BindOrderBy is called, the binding logic can then bind everything and build up single SelectExpression.  Note that when I do bind the then-by's, I iterate over the collection in reverse since the then-by's were collected backward.


Now, with all that in place, everything should be ready to go.


Giving it a whirl, I see that the query:

    var query = from c in db.Customers

                orderby c.Country, c.City

                select c;

Is translated into:


    SELECT t1.CustomerID, t1.ContactName, t1.Phone, t1.City, t1.Country
    FROM (
      SELECT t0.CustomerID, t0.ContactName, t0.Phone, t0.City, t0.Country
      FROM Customers AS t0
    ) AS t1
    ORDER BY t1.Country, t1.City


Which is exactly what I wanted. Wahoo!


 


Unfortunately, that's not the end of the story.  You probably knew I was going to say that.  You were probably thinking, hey, it seems he's about wrapped up on the feature yet there is a whole bunch more text to read. He can't possibly be done. There must be something wrong. He's trying to trick me.  It's like one of those puzzle questions.  Arrg!  I hate puzzle questions.


Yes, there is something definitely wrong. In this example, ordering appeared to work fine. The translator translated the query, the server ate it up and presto, results ordered as requested. The problem lies in other potential examples. The truth is that LINQ is a bit more free with ordering than SQL. As its stands now, a slightly different example would generate SQL text that SQL databases would choke on.


LINQ allows me to put ordering expressions anywhere I please. SQL is very restrictive. There are a few exceptions, but mostly I'm constrained to having only a single order-by clause on the outer-most SQL Select statement. Just like in my example above. Yet what would happen if the order-by came earlier? What if I piled on some more LINQ operators that logically came after the ordering?


What if instead I had this query.

    var query = from c in db.Customers

                orderby c.City

                where c.Country == "UK"

                select c;


It's very similar to the one before, yet I now have a where clause after the orderby.  I can't do that with SQL.  Even if I could, what kind of SQL would my provider generate?

    SELECT t2.City, t2.Country, t2.CustomerID, t2.ContactName, t2.Phone
    FROM (
      SELECT t1.City, t1.Country, t1.CustomerID, t1.ContactName, t1.Phone
      FROM (
        SELECT t0.City, t0.Country, t0.CustomerID, t0.ContactName, t0.Phone
        FROM Customers AS t0
      ) AS t1
      ORDER BY t1.City
    ) AS t2
    WHERE (t2.Country = 'UK')
Egads!  That's definitely not going to fly. Aside from the fact that this query is getting out of control size-wise, the order-by is now nested and that should not happen. At least if I don't want the user getting exceptions anytime they deviate from the basic ho-hum query.


It even fails when I add a simple projection.

    var query = from c in db.Customers

                orderby c.City

                select new { c.Country, c.City, c.ContactName };


This translates with the same problem.


    SELECT t2.Country, t2.City, t2.ContactName
    FROM (
      SELECT t1.City, t1.Country, t1.ContactName, t1.CustomerID, t1.Phone
      FROM (
        SELECT t0.City, t0.Country, t0.ContactName, t0.CustomerID, t0.Phone
        FROM Customers AS t0
      ) AS t1
      ORDER BY t1.City
    ) AS t2
Clearly something needs to be done.  The question is, what?


<insert dramatic pause>


Of course, I already have a solution in case you were wondering. It pretty much entails fixing up the query tree to abide by SQL's rules about ordering.  This means lifting the order-by expressions out from where they don't belong and adding them back where they do. 


Which is not going to be easy.  The query tree, based on LINQ expression nodes is immutable, meaning it cannot be changed. That's not the hard part because fortunately the visitors were written to recognize change automatically and build me a new immutable tree.  The hard part is going to be making sure the table aliases all match up correctly and dealing with order-by's that reference columns that no longer exist in the output. 


Looks like the real heavy lifting has not even started yet.


 


Reordering for SQL's sake

So how did I do it?  I basically wrote another visitor that just deals with moving the order-by's around the tree. I've tried to simplify it as much as possible, but in the end its still a complicated piece of code.  I could have attempted to integrate this rewriting into the binder itself, but that would have only added as much complication to an already involved process.  It was better to separate it out so did not add confusion to the rest.


Take a look.

    /// <summary>

    /// Move order-bys to the outermost select

    /// </summary>
    internal class OrderByRewriter : DbExpressionVisitor {

        IEnumerable<OrderExpression> gatheredOrderings;

        bool isOuterMostSelect;

        public OrderByRewriter() {

        }


        public Expression Rewrite(Expression expression) {

            this.isOuterMostSelect = true;

            return this.Visit(expression);

        }


        protected override Expression VisitSelect(SelectExpression select) {

            bool saveIsOuterMostSelect = this.isOuterMostSelect;

            try {

                this.isOuterMostSelect = false;

                select = (SelectExpression)base.VisitSelect(select);

                bool hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;

                if (hasOrderBy) {

                    this.PrependOrderings(select.OrderBy);

                }

                bool canHaveOrderBy = saveIsOuterMostSelect;

                bool canPassOnOrderings = !saveIsOuterMostSelect;

                IEnumerable<OrderExpression> orderings = (canHaveOrderBy) ? this.gatheredOrderings : null;

                ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;

                if (this.gatheredOrderings != null) {

                    if (canPassOnOrderings) {

                        HashSet<string> producedAliases = new AliasesProduced().Gather(select.From);

                        // reproject order expressions using this select's alias so the outer select will have properly formed expressions

                        BindResult project = this.RebindOrderings(this.gatheredOrderings, select.Alias, producedAliases, select.Columns);

                        this.gatheredOrderings = project.Orderings;

                        columns = project.Columns;

                    }

                    else {

                        this.gatheredOrderings = null;

                    }

                }

                if (orderings != select.OrderBy || columns != select.Columns) {

                    select = new SelectExpression(select.Type, select.Alias, columns, select.From, select.Where, orderings);

                }

                return select;

            }

            finally {

                this.isOuterMostSelect = saveIsOuterMostSelect;

            }

        }


        protected override Expression VisitJoin(JoinExpression join) {

            // make sure order by expressions lifted up from the left side are not lost

            // when visiting the right side

            Expression left = this.VisitSource(join.Left);

            IEnumerable<OrderExpression> leftOrders = this.gatheredOrderings;

            this.gatheredOrderings = null; // start on the right with a clean slate

            Expression right = this.VisitSource(join.Right);

            this.PrependOrderings(leftOrders);

            Expression condition = this.Visit(join.Condition);

            if (left != join.Left || right != join.Right || condition != join.Condition) {

                return new JoinExpression(join.Type, join.Join, left, right, condition);

            }

            return join;

        }


        /// <summary>

        /// Add a sequence of order expressions to an accumulated list, prepending so as

        /// to give precedence to the new expressions over any previous expressions

        /// </summary>

        /// <param name="newOrderings"></param>

        protected void PrependOrderings(IEnumerable<OrderExpression> newOrderings) {

            if (newOrderings != null) {

                if (this.gatheredOrderings == null) {

                    this.gatheredOrderings = newOrderings;

                }

                else {

                    List<OrderExpression> list = this.gatheredOrderings as List<OrderExpression>;

                    if (list == null) {

                        this.gatheredOrderings = list = new List<OrderExpression>(this.gatheredOrderings);

                    }

                    list.InsertRange(0, newOrderings);

                }

            }

        }


        protected class BindResult {

            ReadOnlyCollection<ColumnDeclaration> columns;

            ReadOnlyCollection<OrderExpression> orderings;

            public BindResult(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orderings) {

                this.columns = columns as ReadOnlyCollection<ColumnDeclaration>;

                if (this.columns == null) {

                    this.columns = new List<ColumnDeclaration>(columns).AsReadOnly();

                }

                this.orderings = orderings as ReadOnlyCollection<OrderExpression>;

                if (this.orderings == null) {

                    this.orderings = new List<OrderExpression>(orderings).AsReadOnly();

                }

            }

            public ReadOnlyCollection<ColumnDeclaration> Columns {

                get { return this.columns; }

            }

            public ReadOnlyCollection<OrderExpression> Orderings {

                get { return this.orderings; }

            }

        }


        /// <summary>

        /// Rebind order expressions to reference a new alias and add to column declarations if necessary

        /// </summary>

        protected virtual BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, string alias, HashSet<string> existingAliases, IEnumerable<ColumnDeclaration> existingColumns) {

            List<ColumnDeclaration> newColumns = null;

            List<OrderExpression> newOrderings = new List<OrderExpression>();

            foreach (OrderExpression ordering in orderings) {

                Expression expr = ordering.Expression;

                ColumnExpression column = expr as ColumnExpression;

                if (column == null || (existingAliases != null && existingAliases.Contains(column.Alias))) {

                    // check to see if a declared column already contains a similar expression

                    int iOrdinal = 0;

                    foreach (ColumnDeclaration decl in existingColumns) {

                        ColumnExpression declColumn = decl.Expression as ColumnExpression;

                        if (decl.Expression == ordering.Expression ||

                            (column != null && declColumn != null && column.Alias == declColumn.Alias && column.Name == declColumn.Name)) {

                            // found it, so make a reference to this column

                            expr = new ColumnExpression(column.Type, alias, decl.Name, iOrdinal);

                            break;

                        }

                        iOrdinal++;

                    }

                    // if not already projected, add a new column declaration for it

                    if (expr == ordering.Expression) {

                        if (newColumns == null) {

                            newColumns = new List<ColumnDeclaration>(existingColumns);

                            existingColumns = newColumns;

                        }

                        string colName = column != null ? column.Name : "c" + iOrdinal;

                        newColumns.Add(new ColumnDeclaration(colName, ordering.Expression));

                        expr = new ColumnExpression(expr.Type, alias, colName, iOrdinal);

                    }

                    newOrderings.Add(new OrderExpression(ordering.OrderType, expr));

                }

            }

            return new BindResult(existingColumns, newOrderings);

        }

    }

That's a whole lot of something! ?? 


The main visitation algorithm works like this.  As the visitor walks back up the tree it maintains a growing collection of order-by expressions.  This is almost the opposite of what the binder was doing, as it collected then-by expressions as it walked down the tree.  If both an outer level and inner level select node have order-by expressions, neither of the expressions are lost. The outer level ones simply take precedence by appearing before the inner ones.  This happens in VisitSelect by calling the PrependOrderings function that adds the current order-by's as a block to the head of the growing list.


Next I decide if the current select node can even have order-by expressions in it. Currently, this is a simple test of whether the select node is the outermost select node or not. This question would be more interesting if I had TSQL's TOP clause available. I also determine whether this select node can pass on ordering information. Again, currently, this has to do with the nodes outermost levelness.  If I had DISTINCT available, I would have shut off the order-by propagation.  The reason comes clear when I take into consideration what comes next. The rebind.


After determining that this node must pass on its order-by's to an outer node, the order-by expressions themselves must be rewritten to appear as expressions that reference this select's table alias since these expressions are currently built to refer to aliases from an even deeper nesting.  In addition, if any of these columns that are referenced in the order-by expressions are not available in this select node's column projection, then I need to add them to the projection so they are accessible from the outer select.  This whole ball of wax is called rebinding, and is cordoned off into its own function RebindOrderings.


Now to get back to a prior point, if a given select node had also been distinct, it would have been incorrect to allow order-by expressions to introduce new columns into the projection.  This would have changed the evaluation of the distinct operation as it would have been based on additional columns.  No bother really right now since there is no distinct, but I might add it next week so its worth it to be thinking ahead.  This is the actual reason that LINQ to SQL does not maintain ordering through a distinct or union operation.


 


So putting it all together, I just need to modify DbQueryProvider to call this new visitor.

    public class DbQueryProvider : QueryProvider {
        ...

        private TranslateResult Translate(Expression expression) {

            ProjectionExpression projection = expression as ProjectionExpression;

            if (projection == null) {

                expression = Evaluator.PartialEval(expression, CanBeEvaluatedLocally);

                expression = new QueryBinder(this).Bind(expression);
                expression = new OrderByRewriter().Rewrite(expression);
                projection = (ProjectionExpression)expression;

            }

            string commandText = new QueryFormatter().Format(projection.Source);

            LambdaExpression projector = new ProjectionBuilder().Build(projection.Projector, projection.Source.Alias);

            return new TranslateResult { CommandText = commandText, Projector = projector };

        }
        ...

    } 

Now, if I run this otherwise complicated query

    var query = from c in db.Customers

                orderby c.City

                where c.Country == "UK"

                select new { c.City, c.ContactName };

I get this translation in SQL


    SELECT t3.City, t3.ContactName
    FROM (
      SELECT t2.City, t2.Country, t2.ContactName, t2.CustomerID, t2.Phone
      FROM (
        SELECT t1.City, t1.Country, t1.ContactName, t1.CustomerID, t1.Phone
        FROM (
          SELECT t0.City, t0.Country, t0.ContactName, t0.CustomerID, t0.Phone
          FROM Customers AS t0
        ) AS t1
      ) AS t2
      WHERE (t2.Country = 'UK')
    ) AS t3
    ORDER BY t3.City
Which is far cry better than what it was generating before. 


If I run it to completion, I get the following output:


{ City = Cowes, ContactName = Helen Bennett }
{ City = London, ContactName = Simon Crowther }
{ City = London, ContactName = Hari Kumar }
{ City = London, ContactName = Thomas Hardy }
{ City = London, ContactName = Victoria Ashworth }
{ City = London, ContactName = Elizabeth Brown }
{ City = London, ContactName = Ann Devon }
 


There!  Now that's ordering, or at least a good start. 


Of course, it would still look better if I could reduce some of the unnecessary layers of sub-queries.  Maybe next time. ??

