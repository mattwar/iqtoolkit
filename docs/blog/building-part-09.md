# LINQ: Building an IQueryable Provider – Part IX: Removing redundant subqueries

Matt Warren - MSFT; January 16, 2008

---

This is the nineth in a series of posts on how to build a LINQ IQueryable provider. If you have not read the previous posts here's a handy list of all the fun you've been missing.
Complete list of posts in the Building an IQueryable Provider series 
It's now officially a trend that additional installments to this series take longer and longer to be produced.  Blame the television writer's strike, I do.    
Cleaning up the Mess

I've been promising for a while to show you how I'm going to go about cleaning up the unnecessary layers of nested select expressions that my query translator has been accumulating. It's easy for a human brain to look at a query and realize that it could be written a lot simpler. However, its a lot easier for a computer program to just keep piling the layers on, after all the semantics are the same and the boon we get from keeping the program simple is nothing to sneer at.
It's easy to see the problem in a simple query with a where clause.

    from c in db.Customers

    where c.Country == "UK"

    select c;


This innocuous query turns into the following SQL:
SELECT t1.Country, t1.CustomerID, t1.ContactName, t1.Phone, t1.City
FROM (
  SELECT t0.Country, t0.CustomerID, t0.ContactName, t0.Phone, t0.City
  FROM Customers AS t0
) AS t1
WHERE (t1.Country = 'UK')
Why the extra SELECT?  It's easy to see why it happens when you know how the translation works and what the underlying LINQ query really is.
The LINQ query's method call syntax really looks like this: 

    db.Customers.Where(c => c.Country == "UK").Select(c => c);


It has two LINQ query operators, Where() and Select().  My translation engine in the SqlBinder class translates both of these method calls into two separate SelectExpression's.
Ideally, the SQL query would have looked like this:
SELECT t0.Country, t0.CustomerID, t0.ContactName, t0.Phone, t0.City
FROM Customers AS t0
WHERE (t0.Country = 'UK')


However, that's just the easy case.  It gets worse as more operators are added.  Did you think the translator was smart enough to merge multiple Where clauses together?  I certainly did not add any code for that.  It would be nice if the language compiler did it for me, but what about the case where additional Where() operators are added conditionally after the base query is already formed?

var query =

    from c in db.Customers

    where c.Country == "UK"

    select c;

...

query = from c in query

        where c.Phone == "555-5555"

        select c;


This becomes a triple layer monstrosity, which would only be good if it were a sandwich.


SELECT t2.CustomerID, t2.ContactName, t2.Phone, t2.City, t2.Country
FROM (
  SELECT t1.CustomerID, t1.ContactName, t1.Phone, t1.City, t1.Country
  FROM (
    SELECT t0.CustomerID, t0.ContactName, t0.Phone, t0.City, t0.Country
    FROM Customers AS t0
  ) AS t1
  WHERE (t1.Country = 'UK')
) AS t2
WHERE (t2.Phone = '555-5555')
And its not just the layering either.  What happens when when I try to project out a subset of the data? 

var query =

    from c in db.Customers

    where c.Country == "UK"

    select c.CustomerID;

This becomes the following:


SELECT t2.CustomerID
FROM (
  SELECT t1.CustomerID, t1.ContactName, t1.Phone, t1.City, t1.Country
  FROM (
    SELECT t0.CustomerID, t0.ContactName, t0.Phone, t0.City, t0.Country
    FROM Customers AS t0
  ) AS t1
  WHERE (t1.Country = 'UK')
) AS t2
Why oh why do the nested queries keep reselecting data that's never being used?  Hopefully the database engine is smart enough not to pipeline all that data when most of its never even referred to in the query or returned to the client.  Yet, wouldn't it be a whole lot nicer if the query translator could reduce this madness down to a simpler form that an actual human might write?  Then maybe someone would be able to make head or tails out of this one:

var query = from c in db.Customers

            join o in db.Orders on c.CustomerID equals o.CustomerID

            let m = c.Phone

            orderby c.City

            where c.Country == "UK"

            where m != "555-5555"

            select new { c.City, c.ContactName } into x

            where x.City == "London"

            select x;

I'm not even going to show you this one yet, as it might frighten you to the point of powering down the computer.


What I am going to do is show you what I've done about it, how I rolled up my sleeves and wrote some code that saves the day.  It turned out not to be too horribly difficult.  I had imagined working with the immutable expression tree would become more and more complex as the desired transformations became more and more interesting, given how complicated the order-by rewriter seemed to get.  However, I was pleasantly surprised to find out that this clean-up logic was in fact turning out to be actually sort of clean.


 


Removing Redudant Subqueries

I first wanted to tackle how to get rid of redundant subqueries.  For example, an identity select does absolutely nothing, yet its adds a whole new layer.  What I needed was a way to remove select expressions that do nothing interesting.  Of course, the first thing I needed to do was decide what made an expression redundant.  Obviously, a select expression is redundant if it does not actually do anything at all, except for re-selecting columns from another select expression. Other select expressions would seem redundant if the only thing they do are operations that could have been combined with those of another select expression.


The next thing I needed to do was to figure out how to actually remove a select expression from an expression tree and end up with a tree that is actually legal.  This step had an ominous feel that I was going to end up devising something even more arcane than the order-by rewriter.  I was surprised that it turned out to be so simple.  Take a look.

    internal class SubqueryRemover : DbExpressionVisitor

    {

        HashSet<SelectExpression> selectsToRemove;

        Dictionary<string, Dictionary<string, Expression>> map;

        public Expression Remove(SelectExpression outerSelect, params SelectExpression[] selectsToRemove)

        {

            return Remove(outerSelect, (IEnumerable<SelectExpression>)selectsToRemove);

        }


        public Expression Remove(SelectExpression outerSelect, IEnumerable<SelectExpression> selectsToRemove)

        {

            this.selectsToRemove = new HashSet<SelectExpression>(selectsToRemove);

            this.map = selectsToRemove.ToDictionary(d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));

            return this.Visit(outerSelect);

        }


        protected override Expression VisitSelect(SelectExpression select)

        {

            if (this.selectsToRemove.Contains(select))

            {

                return this.Visit(select.From);

            }

            else

            {

                return base.VisitSelect(select);

            }

        }


        protected override Expression VisitColumn(ColumnExpression column)

        {

            Dictionary<string, Expression> nameMap;

            if (this.map.TryGetValue(column.Alias, out nameMap))

            {

                Expression expr;

                if (nameMap.TryGetValue(column.Name, out expr))

                {

                    return this.Visit(expr);

                }

                throw new Exception("Reference to undefined column");

            }

            return column;

        }

    }



That's it.  This is a nice little class that will rewrite an expression tree and remove one or more select expressions from it, automatically fixing up all references to columns that go away. Looking at the two visit methods, the code looks trivial. When I see a select that's one of ones to be removed, I simply throw it away by returning its 'from' expression. When I see a column expression that is referencing a column that is declared in a select expression that is removed, I substitute the expression used in the declaration for the reference.  As it turns out the most interesting piece of the whole visitor is figuring out the set of columns that are going away, and this is determined using a nice little LINQ query in the Remove method to construct a dictionary of dictionaries that tells me this.


Now all that's left is to write some code that actually figures out which subqueries are the redundant ones.


And here that is. 

   internal class RedundantSubqueryRemover : DbExpressionVisitor

    {

        internal Expression Remove(Expression expression)

        {

            return this.Visit(expression);

        }

        protected override Expression VisitSelect(SelectExpression select)

        {

            select = (SelectExpression)base.VisitSelect(select);


            // first remove all purely redundant subqueries

            List<SelectExpression> redundant = new RedundantSubqueryGatherer().Gather(select.From);

            if (redundant != null)

            {

                select = (SelectExpression)new SubqueryRemover().Remove(select, redundant);

            }


            // next attempt to merge subqueries


            // can only merge if subquery is a single select (not a join)

            SelectExpression fromSelect = select.From as SelectExpression;

            if (fromSelect != null)

            {

                // can only merge if subquery has simple-projection (no renames or complex expressions)

                if (HasSimpleProjection(fromSelect))

                {

                    // remove the redundant subquery

                    select = (SelectExpression)new SubqueryRemover().Remove(select, fromSelect);

                    // merge where expressions

                    Expression where = select.Where;

                    if (fromSelect.Where != null)

                    {

                        if (where != null)

                        {

                            where = Expression.And(fromSelect.Where, where);

                        }

                        else

                        {

                            where = fromSelect.Where;

                        }

                    }

                    if (where != select.Where)

                    {

                        return new SelectExpression(select.Type, select.Alias, select.Columns, select.From, where, select.OrderBy);

                    }

                }

            }


            return select;

        }


        private static bool IsRedudantSubquery(SelectExpression select)

        {

            return HasSimpleProjection(select)

                && select.Where == null

                && (select.OrderBy == null || select.OrderBy.Count == 0);

        }


        private static bool HasSimpleProjection(SelectExpression select)

        {

            foreach (ColumnDeclaration decl in select.Columns)

            {

                ColumnExpression col = decl.Expression as ColumnExpression;

                if (col == null || decl.Name != col.Name)

                {

                    // column name changed or column expression is more complex than reference to another column

                    return false;

                }

            }

            return true;

        }


        class RedundantSubqueryGatherer : DbExpressionVisitor

        {

            List<SelectExpression> redundant;


            internal List<SelectExpression> Gather(Expression source)

            {

                this.Visit(source);

                return this.redundant;

            }


            protected override Expression VisitSelect(SelectExpression select)

            {

                if (IsRedudantSubquery(select))

                {

                    if (this.redundant == null)

                    {

                        this.redundant = new List<SelectExpression>();

                    }

                    this.redundant.Add(select);

                }

                return select;

            }

        }

    }


The RedundantSubqueryRemover is a bit more involved that the SubqueryRemover, but it basically has a simple algorithm.  When it examines a given select expression it tries to determine if one or more sub-select's are redundant. To determine this set it uses anothger visitor, the RedundantSubqueryGatherer, which builds a list of redundant subqueries that can be reached without recursing down into any 'from' expressions. This allows it to consider all the sub select expressions that are in scope to the parent select expression, seeing through any join expression nodes that may exist. Once I have this list, I just use the SubqueryRemover to remove them.


Following that, I look to see if any select expressions can be merged together.  The only interesting thing to consider at this time is whether a subquery would be considered redundant except for a where expression that can easily be combined into the outer select expression. If I find one of these, I go ahead and remove the subquery and add its where expression to the outer select expression.  Yes, I'm surprised too that it works out that easily.


 


Removing Unused Columns

The second part of clean up is to get rid of unused columns.  If I project into a smaller set of columns and don't even reference some of the others then why keep them in the query at all?  It might not matter to the semantics of the query and it might not even make the query faster, but it will certainly be easier on the eyes, and it might make it possible to discover more redundant subqueries that might have slipped by simply because the subquery computed a column that is later ignored.


Of course, this turns out to be a lot more complicated than the other two, so I saved it for last.

    internal class UnusedColumnRemover : DbExpressionVisitor

    {

        Dictionary<string, HashSet<string>> allColumnsUsed;

        internal Expression Remove(Expression expression)

        {

            this.allColumnsUsed = new Dictionary<string, HashSet<string>>();

            return this.Visit(expression);

        }


        protected override Expression VisitColumn(ColumnExpression column)

        {

            HashSet<string> columns;

            if (!this.allColumnsUsed.TryGetValue(column.Alias, out columns))

            {

                columns = new HashSet<string>();

                this.allColumnsUsed.Add(column.Alias, columns);

            }

            columns.Add(column.Name);

            return column;

        }


        protected override Expression VisitSelect(SelectExpression select)

        {

            // visit column projection first

            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;


            HashSet<string> columnsUsed;

            if (this.allColumnsUsed.TryGetValue(select.Alias, out columnsUsed))

            {

                List<ColumnDeclaration> alternate = null;

                for (int i = 0, n = select.Columns.Count; i < n; i++)

                {

                    ColumnDeclaration decl = select.Columns[i];

                    if (!columnsUsed.Contains(decl.Name))

                    {

                        decl = null;  // null means it gets omitted

                    }

                    else

                    {

                        Expression expr = this.Visit(decl.Expression);

                        if (expr != decl.Expression)

                        {

                            decl = new ColumnDeclaration(decl.Name, decl.Expression);

                        }

                    }

                    if (decl != select.Columns[i] && alternate == null)

                    {

                        alternate = new List<ColumnDeclaration>();

                        for (int j = 0; j < i; j++)

                        {

                            alternate.Add(select.Columns[j]);

                        }

                    }

                    if (decl != null && alternate != null)

                    {

                        alternate.Add(decl);

                    }

                }

                if (alternate != null)

                {

                    columns = alternate.AsReadOnly();

                }

            }


            ReadOnlyCollection<OrderExpression> orderbys = this.VisitOrderBy(select.OrderBy);

            Expression where = this.Visit(select.Where);

            Expression from = this.Visit(select.From);


            if (columns != select.Columns || orderbys != select.OrderBy || where != select.Where || from != select.From)

            {

                return new SelectExpression(select.Type, select.Alias, columns, from, where, orderbys);

            }


            return select;

        }


        protected override Expression VisitProjection(ProjectionExpression projection)

        {

            // visit mapping in reverse order

            Expression projector = this.Visit(projection.Projector);

            SelectExpression source = (SelectExpression)this.Visit(projection.Source);

            if (projector != projection.Projector || source != projection.Source)

            {

                return new ProjectionExpression(source, projector);

            }

            return projection;

        }


        protected override Expression VisitJoin(JoinExpression join)

        {

            // visit join in reverse order

            Expression condition = this.Visit(join.Condition);

            Expression right = this.VisitSource(join.Right);

            Expression left = this.VisitSource(join.Left);

            if (left != join.Left || right != join.Right || condition != join.Condition)

            {

                return new JoinExpression(join.Type, join.Join, left, right, condition);

            }

            return join;

        }

    }



The reason is turns out to be so much more complicated is that in order to figure out what columns are used I have to examine the places where they are used before I get to the place where they are defined because that's where I need to remove them, which is exactly opposite of how the visitors normally flow through the expression tree, so instead of simply override a few pieces of a visitor I have to re-specify visit methods I could have otherwise ignored.


When I get to a select expression I have to examine the column declarations, where expression and order-by expressions before I recurse down into the from expression, because all of these expression may reference columns declared in the sub queries.  The first thing I do is grovel through the set of column declarations and remove the ones that are not referenced by the layer above. The top level select expression's column expression are, of course, reference by the projection expression. Most of the code in VisitSelect is just the work of reassembling this list of declarations. The rest is pretty much boiler plate from the base DbExpressionVisitor, except in a reverse order.


 


Putting It to the Test

The last thing to do is to wire these new clean-up visitors into the query translation process.  The appropriate place to do this is where all the other top level visitors are plugged in, the DbQueryProvider class.

    public class DbQueryProvider : QueryProvider {
        ...

        private TranslateResult Translate(Expression expression) {

            ProjectionExpression projection = expression as ProjectionExpression;

            if (projection == null) {

                expression = Evaluator.PartialEval(expression, CanBeEvaluatedLocally);

                expression = new QueryBinder(this).Bind(expression);

                expression = new OrderByRewriter().Rewrite(expression);

                expression = new UnusedColumnRemover().Remove(expression);

                expression = new RedundantSubqueryRemover().Remove(expression);

                projection = (ProjectionExpression)expression;

            }

            string commandText = new QueryFormatter().Format(projection.Source);

            LambdaExpression projector = new ProjectionBuilder().Build(projection.Projector, projection.Source.Alias);

            return new TranslateResult { CommandText = commandText, Projector = projector };

        }
        ...

    }


I just added them in right after the OrderByRewriter, allowing unused columns to be removed first in case doing so helps the redundant subquery removal.


Now all I have to do is try it out. 


How about that huge scary query?  How bad could it be?

var query = from c in db.Customers

            join o in db.Orders on c.CustomerID equals o.CustomerID

            let m = c.Phone

            orderby c.City

            where c.Country == "UK"

            where m != "555-5555"

            select new { c.City, c.ContactName } into x

            where x.City == "London"

            select x;


Without the new additions the translation is horrific.


SELECT t9.City, t9.ContactName
FROM (
  SELECT t8.City, t8.ContactName
  FROM (
    SELECT t7.CustomerID, t7.ContactName, t7.Phone, t7.City, t7.Country, t7.OrderID, t7.CustomerID1, t7.OrderDate
    FROM (
      SELECT t6.CustomerID, t6.ContactName, t6.Phone, t6.City, t6.Country, t6.OrderID, t6.CustomerID1, t6.OrderDate
      FROM (
        SELECT t5.CustomerID, t5.ContactName, t5.Phone, t5.City, t5.Country, t5.OrderID, t5.CustomerID1, t5.OrderDate
        FROM (
          SELECT t4.CustomerID, t4.ContactName, t4.Phone, t4.City, t4.Country, t4.OrderID, t4.CustomerID1, t4.OrderDate
          FROM (
            SELECT t1.CustomerID, t1.ContactName, t1.Phone, t1.City, t1.Country, t3.OrderID, t3.CustomerID AS CustomerID1, t3.OrderDate
            FROM (
              SELECT t0.CustomerID, t0.ContactName, t0.Phone, t0.City, t0.Country
              FROM Customers AS t0
            ) AS t1
            INNER JOIN (
              SELECT t2.OrderID, t2.CustomerID, t2.OrderDate
              FROM Orders AS t2
            ) AS t3
              ON (t1.CustomerID = t3.CustomerID)
          ) AS t4
        ) AS t5
      ) AS t6
      WHERE (t6.Country = 'UK')
    ) AS t7
    WHERE (t7.Phone <> '555-5555')
  ) AS t8
) AS t9
WHERE (t9.City = 'London')
ORDER BY t9.City
When I gather the courage to look I start to worry about exceeding some maximum query size sending this to the server.  Clearly, if the table had a more realistic number of columns the size would explode comparatively.


Now take a look at the query produced when the new clean-up code is enabled.


SELECT t0.City, t0.ContactName
FROM Customers AS t0
INNER JOIN Orders AS t2
  ON (t0.CustomerID = t2.CustomerID)
WHERE (((t0.Country = 'UK') AND (t0.Phone <> '555-5555')) AND (t0.City = 'London'))
ORDER BY t0.City
How sweet is that?  It's actually easier to read than the original LINQ query.


Sometimes I do amaze even myself.