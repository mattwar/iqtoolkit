# LINQ: Building an IQueryable Provider – Part V: Improving column binding

Matt Warren - MSFT; August 3, 2007

---

This is the fifth in a series of posts on how to build a LINQ IQueryable provider.  If you have not read the previous posts please take a look before proceeding, or if you are daring dig right in.


Complete list of posts in the Building an IQueryable Provider series 

Over the past four parts of this series I have constructed a working LINQ IQueryable provider that targets ADO and SQL and has so far been able to translate both Queryable.Where and Queryable.Select standard query operators. Yet, as big of an accomplishment that has been there are still a few gaping holes and I’m not talking about other missing operators like OrderBy and Join. I’m talking about huge conceptual gaffs that will bite anyone that strays from my oh-so-ideally crafted demo queries.


Fixing the Gaping Holes


Certainly, I can write a simple where/select pair and it works as advertised.  My select expression can be arbitrarily complex and it still chugs along.


var query = db.Customers.Where(c => c.City == city)


                        .Select(c => new {


                            Name = c.ContactName,


                            Location = c.City });



However, if just rearrange the order of Where and Select it all falls apart.


var query = db.Customers.Select(c => new {


                            Name = c.ContactName,


                            Location = c.City })
                        .Where(x => x.Location == city);

This handsome little query generates SQL that’s not exactly right.


 


SELECT * FROM (SELECT ContactName, City FROM (SELECT * FROM Customers) AS T) AS T WHERE (Location = 'London')

It also generates an exception when executed, “Invalid column name 'Location'.”  It seems my oversimplifying practice of treating member accesses as column references has backfired.  I naively assumed that the only member accesses in the sub-trees would match the names of the columns being generated. Yet, that’s obviously not true. So either I need to find a way to change the names of columns to match or I need to figure out some other way to deal with member accesses.


 


I suppose either is possible.  Yet, if I consider a slightly more complicated example, renaming the columns is not sufficient.  If the select expression produces a hierarchy of objects, then references to members can become a ‘multi-dot’ operation.


 


var query = db.Customers.Select(c => new {


                            Name = c.ContactName,


                            Location = new {
                                City = c.City,
                                Country = c.Country
                                }
                            })
                        .Where(x => x.Location.City == city);

Now, how am I going to translate this? The existing code does not even contain a concept of what this intermediate ‘Location’ object could be.  Luckily, I already realize what I need to do, yet it’s going to require a big change.  I need to step away from this notion that my provider is just translating query expressions into text. It’s translating query expressions into SQL. Text is only one possible manifestation of SQL and it’s not a very good one for programming logic to operate on. Of course, I’m going to need text eventually, but if I could first represent SQL as an abstraction, I could handle much more complicated translations.


 


Of course, the best data structure to operate on is a semantic SQL tree. So, ideally, I would have this entirely separate tree definition for SQL that I could translate LINQ query expressions into, but that would be a lot of work.  Luckily, the definition of this ideal SQL tree would overlap a lot with LINQ trees, so I’m going to cheat and simply teach LINQ expression trees about SQL. To do this, I’ll add some new expression node types.  It won’t matter if no other LINQ API understands them. I’ll just keep them to myself.


 


internal enum DbExpressionType {


    Table = 1000, // make sure these don't overlap with ExpressionType


    Column,


    Select,


    Projection


}


 


internal class TableExpression : Expression {


    string alias;


    string name;


    internal TableExpression(Type type, string alias, string name)


        : base((ExpressionType)DbExpressionType.Table, type) {


        this.alias = alias;


        this.name = name;


    }


    internal string Alias {


        get { return this.alias; }


    }


    internal string Name {


        get { return this.name; }


    }


}


 


internal class ColumnExpression : Expression {


    string alias;


    string name;


    int ordinal;


    internal ColumnExpression(Type type, string alias, string name, int ordinal)


        : base((ExpressionType)DbExpressionType.Column, type) {


        this.alias = alias;


        this.name = name;


        this.ordinal = ordinal;


    }


    internal string Alias {


        get { return this.alias; }


    }


    internal string Name {


        get { return this.name; }


    }


    internal int Ordinal {


        get { return this.ordinal; }


    }


}


 


internal class ColumnDeclaration {


    string name;


    Expression expression;


    internal ColumnDeclaration(string name, Expression expression) {


        this.name = name;


        this.expression = expression;


    }


    internal string Name {


        get { return this.name; }


    }


    internal Expression Expression {


        get { return this.expression; }


    }


}


 


internal class SelectExpression : Expression {


    string alias;


    ReadOnlyCollection<ColumnDeclaration> columns;


    Expression from;


    Expression where;


    internal SelectExpression(Type type, string alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression where)


        : base((ExpressionType)DbExpressionType.Select, type) {


        this.alias = alias;


        this.columns = columns as ReadOnlyCollection<ColumnDeclaration>;


        if (this.columns == null) {


            this.columns = new List<ColumnDeclaration>(columns).AsReadOnly();


        }


        this.from = from;


        this.where = where;


    }


    internal string Alias {


        get { return this.alias; }


    }


    internal ReadOnlyCollection<ColumnDeclaration> Columns {


        get { return this.columns; }


    }


    internal Expression From {


        get { return this.from; }


    }


    internal Expression Where {


        get { return this.where; }


    }


}


 


internal class ProjectionExpression : Expression {


    SelectExpression source;


    Expression projector;


    internal ProjectionExpression(SelectExpression source, Expression projector)


        : base((ExpressionType)DbExpressionType.Projection, projector.Type) {


        this.source = source;


        this.projector = projector;


    }


    internal SelectExpression Source {


        get { return this.source; }


    }


    internal Expression Projector {


        get { return this.projector; }


    }


}


 


The only bits of SQL I really need to add to LINQ expression trees are the concepts of a SQL Select query that produces one or more columns, a reference to a column, a reference to a table and a projection that reassembles objects out of column references.


 


I went ahead and defined my own DbExpressionType enum that ‘extends’ the base ExpressionType enum by picking a sufficiently large starting value to not collide with the other definitions.  If there was such a way as to derive from an enum I would have done that, but this will work as long as I am diligent.


 


Each of the new expression nodes follows the same pattern set by the LINQ expressions, being immutable, etc; except they are now modeling SQL concepts and not CLR concepts.  Notice, how the SelectExpression contains a collection of columns, and both a from and where expression.  That is because this expression node is meant to match what a legal SQL select statement would contain. 


 


The ProjectionExpression describes how to construct a result object out of the columns of a select expression.  If you think about it, this is almost exactly the same job that the projection expression held in Part IV, the one that was used to build the delegate for the ProjectionReader. Only this time, it’s possible to reason about projection in terms of the SQL query and not just as a function that assembles objects out of DataReaders.


 


Of course, now that I’ve got new nodes, I need a new visitor. The DbExpressionVisitor extends the ExpressionVisitor, adding the base visit pattern for the new nodes.


 


internal class DbExpressionVisitor : ExpressionVisitor {


    protected override Expression Visit(Expression exp) {


        if (exp == null) {


            return null;


        }


        switch ((DbExpressionType)exp.NodeType) {


            case DbExpressionType.Table:


                return this.VisitTable((TableExpression)exp);


            case DbExpressionType.Column:


                return this.VisitColumn((ColumnExpression)exp);


            case DbExpressionType.Select:


                return this.VisitSelect((SelectExpression)exp);


            case DbExpressionType.Projection:


                return this.VisitProjection((ProjectionExpression)exp);


            default:


                return base.Visit(exp);


        }


    }


    protected virtual Expression VisitTable(TableExpression table) {


        return table;


    }


    protected virtual Expression VisitColumn(ColumnExpression column) {


        return column;


    }


    protected virtual Expression VisitSelect(SelectExpression select) {


        Expression from = this.VisitSource(select.From);


        Expression where = this.Visit(select.Where);


        ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);


        if (from != select.From || where != select.Where || columns != select.Columns) {


            return new SelectExpression(select.Type, select.Alias, columns, from, where);


        }


        return select;


    }


    protected virtual Expression VisitSource(Expression source) {


        return this.Visit(source);


    }


    protected virtual Expression VisitProjection(ProjectionExpression proj) {


        SelectExpression source = (SelectExpression)this.Visit(proj.Source);


        Expression projector = this.Visit(proj.Projector);


        if (source != proj.Source || projector != proj.Projector) {


            return new ProjectionExpression(source, projector);


        }


        return proj;


    }


    protected ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> columns) {


        List<ColumnDeclaration> alternate = null;


        for (int i = 0, n = columns.Count; i < n; i++) {


            ColumnDeclaration column = columns[i];


            Expression e = this.Visit(column.Expression);


            if (alternate == null && e != column.Expression) {


                alternate = columns.Take(i).ToList();


            }


            if (alternate != null) {


                alternate.Add(new ColumnDeclaration(column.Name, e));


            }


        }


        if (alternate != null) {


            return alternate.AsReadOnly();


        }


        return columns;


    }


}



That’s better.  Now I feel like I’m really headed somewhere!


 


The next step is to take a stick of dynamite and blow up the QueryTranslator. No more monolithic expression tree to string translator.  What I need are individual pieces that handle separate tasks;  one to bind the expression tree by figuring out what methods like Queryable.Select mean and another to convert the resulting tree into SQL text.  Hopefully, by concocting this LINQ/SQL hybrid tree I’ll be able to figure out the member access mess.


 


Here’s the code for the new QueryBinder class.


 


internal class QueryBinder : ExpressionVisitor {


    ColumnProjector columnProjector;


    Dictionary<ParameterExpression, Expression> map;


    int aliasCount;


 


    internal QueryBinder() {


        this.columnProjector = new ColumnProjector(this.CanBeColumn);


    }


 


    private bool CanBeColumn(Expression expression) {


        return expression.NodeType == (ExpressionType)DbExpressionType.Column;


    }


 


    internal Expression Bind(Expression expression) {


        this.map = new Dictionary<ParameterExpression, Expression>();


        return this.Visit(expression);


    }


 


    private static Expression StripQuotes(Expression e) {


        while (e.NodeType == ExpressionType.Quote) {


            e = ((UnaryExpression)e).Operand;


        }


        return e;


    }


 


    private string GetNextAlias() {


        return "t" + (aliasCount++);


    }


 


    private ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias) {


        return this.columnProjector.ProjectColumns(expression, newAlias, existingAlias);


    }


 


    protected override Expression VisitMethodCall(MethodCallExpression m) {


        if (m.Method.DeclaringType == typeof(Queryable) ||


            m.Method.DeclaringType == typeof(Enumerable)) {


            switch (m.Method.Name) {


                case "Where":


                    return this.BindWhere(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));


                case "Select":


                    return this.BindSelect(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));


            }


            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));


        }


        return base.VisitMethodCall(m);


    }


 


    private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate) {


        ProjectionExpression projection = (ProjectionExpression)this.Visit(source);


        this.map[predicate.Parameters[0]] = projection.Projector;


        Expression where = this.Visit(predicate.Body);


        string alias = this.GetNextAlias();


        ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, GetExistingAlias(projection.Source));


        return new ProjectionExpression(


            new SelectExpression(resultType, alias, pc.Columns, projection.Source, where),


            pc.Projector


            );


    }


 


    private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector) {


        ProjectionExpression projection = (ProjectionExpression)this.Visit(source);


        this.map[selector.Parameters[0]] = projection.Projector;


        Expression expression = this.Visit(selector.Body);


        string alias = this.GetNextAlias();


        ProjectedColumns pc = this.ProjectColumns(expression, alias, GetExistingAlias(projection.Source));


        return new ProjectionExpression(


            new SelectExpression(resultType, alias, pc.Columns, projection.Source, null),


            pc.Projector


            );


    }


 


    private static string GetExistingAlias(Expression source) {


        switch ((DbExpressionType)source.NodeType) {


            case DbExpressionType.Select:


                return ((SelectExpression)source).Alias;


            case DbExpressionType.Table:


                return ((TableExpression)source).Alias;


            default:


                throw new InvalidOperationException(string.Format("Invalid source node type '{0}'", source.NodeType));


        }


    }


 


    private bool IsTable(object value) {


        IQueryable q = value as IQueryable;


        return q != null && q.Expression.NodeType == ExpressionType.Constant;


    }


 


    private string GetTableName(object table) {


        IQueryable tableQuery = (IQueryable)table;


        Type rowType = tableQuery.ElementType;


        return rowType.Name;


    }


 


    private string GetColumnName(MemberInfo member) {


        return member.Name;


    }


 


    private Type GetColumnType(MemberInfo member) {


        FieldInfo fi = member as FieldInfo;


        if (fi != null) {


            return fi.FieldType;


        }


        PropertyInfo pi = (PropertyInfo)member;


        return pi.PropertyType;


    }


 


    private IEnumerable<MemberInfo> GetMappedMembers(Type rowType) {


        return rowType.GetFields().Cast<MemberInfo>();


    }


 


    private ProjectionExpression GetTableProjection(object value) {


        IQueryable table = (IQueryable)value;


        string tableAlias = this.GetNextAlias();


        string selectAlias = this.GetNextAlias();


        List<MemberBinding> bindings = new List<MemberBinding>();


        List<ColumnDeclaration> columns = new List<ColumnDeclaration>();


        foreach (MemberInfo mi in this.GetMappedMembers(table.ElementType)) {


            string columnName = this.GetColumnName(mi);


            Type columnType = this.GetColumnType(mi);


            int ordinal = columns.Count;


            bindings.Add(Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName, ordinal)));


            columns.Add(new ColumnDeclaration(columnName, new ColumnExpression(columnType, tableAlias, columnName, ordinal)));


        }


        Expression projector = Expression.MemberInit(Expression.New(table.ElementType), bindings);


        Type resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);


        return new ProjectionExpression(


            new SelectExpression(


                resultType,


                selectAlias,


                columns,


                new TableExpression(resultType, tableAlias, this.GetTableName(table)),


                null


                ),


            projector


            );


    }


 


    protected override Expression VisitConstant(ConstantExpression c) {


        if (this.IsTable(c.Value)) {


            return GetTableProjection(c.Value);


        }


        return c;


    }


 


    protected override Expression VisitParameter(ParameterExpression p) {


        Expression e;


        if (this.map.TryGetValue(p, out e)) {


            return e;


        }


        return p;


    }


 


    protected override Expression VisitMemberAccess(MemberExpression m) {


        Expression source = this.Visit(m.Expression);


        switch (source.NodeType) {


            case ExpressionType.MemberInit:


                MemberInitExpression min = (MemberInitExpression)source;


                for (int i = 0, n = min.Bindings.Count; i < n; i++) {


                    MemberAssignment assign = min.Bindings[i] as MemberAssignment;


                    if (assign != null && MembersMatch(assign.Member, m.Member)) {


                        return assign.Expression;


                    }


                }


                break;


            case ExpressionType.New:


                NewExpression nex = (NewExpression)source;


                if (nex.Members != null) {


                    for (int i = 0, n = nex.Members.Count; i < n; i++) {


                        if (MembersMatch(nex.Members[i], m.Member)) {


                            return nex.Arguments[i];


                        }


                    }


                }


                break;


        }


        if (source == m.Expression) {


            return m;


        }


        return MakeMemberAccess(source, m.Member);


    }


 


    private bool MembersMatch(MemberInfo a, MemberInfo b) {


        if (a == b) {


            return true;


        }


        if (a is MethodInfo && b is PropertyInfo) {


            return a == ((PropertyInfo)b).GetGetMethod();


        }


        else if (a is PropertyInfo && b is MethodInfo) {


            return ((PropertyInfo)a).GetGetMethod() == b;


        }


        return false;


    }


 


    private Expression MakeMemberAccess(Expression source, MemberInfo mi) {


        FieldInfo fi = mi as FieldInfo;


        if (fi != null) {


            return Expression.Field(source, fi);


        }


        PropertyInfo pi = (PropertyInfo)mi;


        return Expression.Property(source, pi);


    }


}



One thing to notice is that there is a lot more going on here than in the QueryTranslator of yore. Translation for Where and Select have been factored out into separate methods.  They don’t produce text anymore, but instances of ProjectionExpression and SelectExpression.  The ColumnProjector looks like it is doing something much more complicated too.  I haven’t shown the code for that yet, but it has changed as well.  There’s all these little helper methods for understanding the meaning of a table or a column.  This is the starting of a factoring that may well end with some kind of mapping system, but I’ll leave that for the future.


 


A key method to examine is the GetTableProjection method that actually assembles a query (with both Select and Project expressions) that represent the default query for getting members out of the database table. No more “select *” here.  The default table projection only represents the columns that I defined in my domain class declarations.


 


Another thing to note is the change in the VisitMemberAccess method.  I no longer only consider trivial accesses off parameter nodes.  I actually attempt to resolve member access by looking up the meaning of a member and returning the sub-expression that a member translates into. 


 


This is how it works. When I translate a ‘table’ constant into a table projection (via GetTableProjection), I include a projector expression that describes how to construct an object out of the table’s columns.  When I get to a Select or Where method I add a mapping from the parameter expression declared by the LambdaExpression argument to the projector of the ‘previous’ portion of the query.  For the first Where or Select that’s just the projector from the table projection.  So, later when I see a parameter expression in VisitParameter I substitute the entire previous projector expression.  It’s okay to do this, the nodes are immutable and so I can include them many times in the tree.  Finally, when I get to the member access node I’ve already turned the parameter into its semantic equivalent. This expression is likely a new or member-init expression node, and so I merely do a lookup in this structure to find the expression that the member access should be replaced with.  Often, this simply finds a ColumnExpression node defined by the table projection.  It could, however, find another new or member-init expression from a previous Select operation that produced a hierarchy.  If it did, then a subsequent member access operation would look inside this expression, and so on.


 


Whew!  That’s a lot to take in.  Unfortunately, I am not done. There’s also this ColumnProjector class that is a whole lot different than before.  Take a look.


 


    internal sealed class ProjectedColumns {


        Expression projector;


        ReadOnlyCollection<ColumnDeclaration> columns;


        internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns) {


            this.projector = projector;


            this.columns = columns;


        }


        internal Expression Projector {


            get { return this.projector; }


        }


        internal ReadOnlyCollection<ColumnDeclaration> Columns {


            get { return this.columns; }


        }


    }


 


    internal class ColumnProjector : DbExpressionVisitor {


        Nominator nominator;


        Dictionary<ColumnExpression, ColumnExpression> map;


        List<ColumnDeclaration> columns;


        HashSet<string> columnNames;


        HashSet<Expression> candidates;


        string existingAlias;


        string newAlias;


        int iColumn;


 


        internal ColumnProjector(Func<Expression, bool> fnCanBeColumn) {


            this.nominator = new Nominator(fnCanBeColumn);


        }


 


        internal ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias) {


            this.map = new Dictionary<ColumnExpression, ColumnExpression>();


            this.columns = new List<ColumnDeclaration>();


            this.columnNames = new HashSet<string>();


            this.newAlias = newAlias;


            this.existingAlias = existingAlias;


            this.candidates = this.nominator.Nominate(expression);


            return new ProjectedColumns(this.Visit(expression), this.columns.AsReadOnly());


        }


 


        protected override Expression Visit(Expression expression) {


            if (this.candidates.Contains(expression)) {


                if (expression.NodeType == (ExpressionType)DbExpressionType.Column) {


                    ColumnExpression column = (ColumnExpression)expression;


                    ColumnExpression mapped;


                    if (this.map.TryGetValue(column, out mapped)) {


                        return mapped;


                    }


                    if (this.existingAlias == column.Alias) {


                        int ordinal = this.columns.Count;


                        string columnName = this.GetUniqueColumnName(column.Name);


                        this.columns.Add(new ColumnDeclaration(columnName, column));


                        mapped = new ColumnExpression(column.Type, this.newAlias, columnName, ordinal);


                        this.map[column] = mapped;


                        this.columnNames.Add(columnName);


                        return mapped;


                    }


                    // must be referring to outer scope


                    return column;


                }


                else {


                    string columnName = this.GetNextColumnName();


                    int ordinal = this.columns.Count;


                    this.columns.Add(new ColumnDeclaration(columnName, expression));


                    return new ColumnExpression(expression.Type, this.newAlias, columnName, ordinal);


                }


            }


            else {


                return base.Visit(expression);


            }


        }


 


        private bool IsColumnNameInUse(string name) {


            return this.columnNames.Contains(name);


        }


 


        private string GetUniqueColumnName(string name) {


            string baseName = name;


            int suffix = 1;


            while (this.IsColumnNameInUse(name)) {


                name = baseName + (suffix++);


            }


            return name;


        }


 


        private string GetNextColumnName() {


            return this.GetUniqueColumnName("c" + (iColumn++));


        }


 


        class Nominator : DbExpressionVisitor {


            Func<Expression, bool> fnCanBeColumn;


            bool isBlocked;


            HashSet<Expression> candidates;


 


            internal Nominator(Func<Expression, bool> fnCanBeColumn) {


                this.fnCanBeColumn = fnCanBeColumn;


            }


 


            internal HashSet<Expression> Nominate(Expression expression) {


                this.candidates = new HashSet<Expression>();


                this.isBlocked = false;


                this.Visit(expression);


                return this.candidates;


            }


 


            protected override Expression Visit(Expression expression) {


                if (expression != null) {


                    bool saveIsBlocked = this.isBlocked;


                    this.isBlocked = false;


                    base.Visit(expression);


                    if (!this.isBlocked) {


                        if (this.fnCanBeColumn(expression)) {


                            this.candidates.Add(expression);


                        }


                        else {


                            this.isBlocked = true;


                        }


                    }


                    this.isBlocked |= saveIsBlocked;


                }


                return expression;


            }


        }


    }

The ColumnProjector is no longer assembling text for the select command, nor is it rewriting the selector expression into a function that constructs an object from a DataReader.  However, it is doing something that is almost the same thing.  It is assembling a list of ColumnDeclaration objects that are going to be used to construct a SelectExpression node, and it is rewriting the selector expression into a projector expression that references the columns assembled in the list.


 


So how does it work?  I’ve probably over engineered this class for what I’m using it for right now, but it will come in handy in the future so I’ll leave it as is.  Before I get into how it works, let’s think about what it needs to do.


 


Given some selector expression, I really need to figure out which parts of the expression should correspond to column declarations of a SQL select statement.  These could be as simple as identifying the column references (ColumnExpression’s) left over in the tree after binding.  Of course, that would mean the expression ‘a + b’ would turn into two column declarations, one for ‘a’ and one for ‘b’, leaving the ‘+’ operation residing in the newly rebuilt projector expression.  That would certainly work, but wouldn’t it make sense to be able to put the entire ‘a + b’ expression into the column?  That way it would be computed by SQL server instead of during construction of the results.  If a Where operation, appearing after the Select operation, were to reference this expression it would have to evaluate it on the server anyway. Ignoring for the moment that I have not even allowed for ‘+’ operations to be translated, you can start to see that the problem of figuring out what should be in a column and what should be in the projection is a problem akin to the problem of figuring out how to pre-evaluate isolated sub-trees.


 


The Evaluator class did this work using a two pass system of first nominating nodes that could be evaluated locally and then a second pass to pick the first nominated nodes from the top down, thus achieving evaluation of ‘maximal’ sub-trees.  Figuring out what could or should be in a column declaration is just same kind of problem, only with a possibly different rule about what should be included or not.  I want to pluck off the maximal sub-trees of nodes that can legally be placed in columns.  Instead of evaluating these trees I just want to make them part of a SelectExpression and rewrite the remaining selector tree into a projector that references the new columns.


 


You will see as you examine the code there is one additional complication that did not exist in the Evaluator. I may need to invent names for these column declarations if they truly are based on more complex sub expressions.


 


Okay, now that I’ve built hybrid expression trees, bound them and gave them a nice spanking, I still need to get text out or the whole process is for naught.  So I took the text generating code from the QueryTranslator and built a new QueryFormatter class whose sole responsibility is to generate the text from an expression tree.  No more needing to actually build the tree at the same time.


 


internal class QueryFormatter : DbExpressionVisitor {


    StringBuilder sb;


    int indent = 2;


    int depth;


 


    internal QueryFormatter() {


    }


 


    internal string Format(Expression expression) {


        this.sb = new StringBuilder();


        this.Visit(expression);


        return this.sb.ToString();


    }


 


    protected enum Identation {


        Same,


        Inner,


        Outer


    }


 


    internal int IdentationWidth {


        get { return this.indent; }


        set { this.indent = value; }


    }


 


    private void AppendNewLine(Identation style) {


        sb.AppendLine();


        if (style == Identation.Inner) {


            this.depth++;


        }


        else if (style == Identation.Outer) {


            this.depth--;


            System.Diagnostics.Debug.Assert(this.depth >= 0);


        }


        for (int i = 0, n = this.depth * this.indent; i < n; i++) {


            sb.Append(" ");


        }


    }


 


    protected override Expression VisitMethodCall(MethodCallExpression m) {


        throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));


    }


 


    protected override Expression VisitUnary(UnaryExpression u) {


        switch (u.NodeType) {


            case ExpressionType.Not:


                sb.Append(" NOT ");


                this.Visit(u.Operand);


                break;


            default:


                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));


        }


        return u;


    }


 


    protected override Expression VisitBinary(BinaryExpression b) {


        sb.Append("(");


        this.Visit(b.Left);


        switch (b.NodeType) {


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


 


    protected override Expression VisitConstant(ConstantExpression c) {


        if (c.Value == null) {


            sb.Append("NULL");


        }


        else {


            switch (Type.GetTypeCode(c.Value.GetType())) {


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


 


    protected override Expression VisitColumn(ColumnExpression column) {


        if (!string.IsNullOrEmpty(column.Alias)) {


            sb.Append(column.Alias);


            sb.Append(".");


        }


        sb.Append(column.Name);


        return column;


    }


 


    protected override Expression VisitSelect(SelectExpression select) {


        sb.Append("SELECT ");


        for (int i = 0, n = select.Columns.Count; i < n; i++) {


            ColumnDeclaration column = select.Columns[i];


            if (i > 0) {


                sb.Append(", ");


            }


            ColumnExpression c = this.Visit(column.Expression) as ColumnExpression;


            if (c == null || c.Name != select.Columns[i].Name) {


                sb.Append(" AS ");


                sb.Append(column.Name);


            }


        }


        if (select.From != null) {


            this.AppendNewLine(Identation.Same);


            sb.Append("FROM ");


            this.VisitSource(select.From);


        }


        if (select.Where != null) {


            this.AppendNewLine(Identation.Same);


            sb.Append("WHERE ");


            this.Visit(select.Where);


        }


        return select;


    }


 


    protected override Expression VisitSource(Expression source) {


        switch ((DbExpressionType)source.NodeType) {


            case DbExpressionType.Table:


                TableExpression table = (TableExpression)source;


                sb.Append(table.Name);


                sb.Append(" AS ");


                sb.Append(table.Alias);


                break;


            case DbExpressionType.Select:


                SelectExpression select = (SelectExpression)source;


                sb.Append("(");


                this.AppendNewLine(Identation.Inner);


                this.Visit(select);


                this.AppendNewLine(Identation.Outer);


                sb.Append(")");


                sb.Append(" AS ");


                sb.Append(select.Alias);


                break;


            default:


                throw new InvalidOperationException("Select source is not valid type");


        }


        return source;


    }


}


 


In addition to adding logic to write out the new SelectExpression node, I’ve advanced the formatting logic to include (gasp) new-lines and indentation.  Now isn’t that special? 


 


Of course, I also have to end up with a LambdaExpression that builds the result objects. I used to get that out of the ColumnProjector class, but now it’s generating these semantic SQL projections, not at all what I need to do the real heavy-lifting of making actual objects.  So I need to transform it again. I built a little class called ProjectionBuilder to do that.


 


internal class ProjectionBuilder : DbExpressionVisitor {


    ParameterExpression row;


    private static MethodInfo miGetValue;


 


    internal ProjectionBuilder() {


        if (miGetValue == null) {


            miGetValue = typeof(ProjectionRow).GetMethod("GetValue");


        }


    }


 


    internal LambdaExpression Build(Expression expression) {


        this.row = Expression.Parameter(typeof(ProjectionRow), "row");


        Expression body = this.Visit(expression);


        return Expression.Lambda(body, this.row);


    }


 


    protected override Expression VisitColumn(ColumnExpression column) {


        return Expression.Convert(Expression.Call(this.row, miGetValue, Expression.Constant(column.Ordinal)), column.Type);


    }


}


 


This class simply does what the ColumnProjector used to do, only it now benefits from the better binding logic introduced by the QueryBinder, so it empirically knows which nodes to translate into data reading expressions.


 


Luckily, I don’t have to rewrite the ProjectionReader. It still works the same as before. What I do get to do is get rid of the ObjectReader, since I now always have a projector expression.  I build one in the QueryBinder automatically every time I see a table.


 


That just leaves the putting it all together step.  Here’s the new rewrite to DbQueryProvider.


 


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


        Delegate projector = result.Projector.Compile();


 


        DbCommand cmd = this.connection.CreateCommand();


        cmd.CommandText = result.CommandText;


        DbDataReader reader = cmd.ExecuteReader();


 


        Type elementType = TypeSystem.GetElementType(expression.Type);


        return Activator.CreateInstance(


            typeof(ProjectionReader<>).MakeGenericType(elementType),


            BindingFlags.Instance | BindingFlags.NonPublic, null,


            new object[] { reader, projector },


            null


            );


    }


 


    internal class TranslateResult {


        internal string CommandText;


        internal LambdaExpression Projector;


    }


 


    private TranslateResult Translate(Expression expression) {


        expression = Evaluator.PartialEval(expression);


        ProjectionExpression proj = (ProjectionExpression)new QueryBinder().Bind(expression);


        string commandText = new QueryFormatter().Format(proj.Source);


        LambdaExpression projector = new ProjectionBuilder().Build(proj.Projector);


        return new TranslateResult { CommandText = commandText, Projector = projector };


    }


}


 


It’s not too different from before.  The Translate method has multiple steps, invoking the additional visitors, and the Execute method no longer has to build ObjectReader if the projector does not exist.  It always exists.


 


So, now if I write the following query:


 


var query = db.Customers.Select(c => new {


                            Name = c.ContactName,


                            Location = new {
                                City = c.City,
                                Country = c.Country
                                }
                            })
                        .Where(x => x.Location.City == city);



It runs successfully producing the following output:


 


Query:


SELECT t2.ContactName, t2.City, t2.Country


FROM (


  SELECT t1.ContactName, t1.City, t1.Country


  FROM (


    SELECT t0.ContactName, t0.City, t0.Country, t0.CustomerID, t0.Phone


    FROM Customers AS t0


  ) AS t1


) AS t2


WHERE (t2.City = 'London')


 


{ Name = Thomas Hardy, Location = { City = London, Country = UK } }


{ Name = Victoria Ashworth, Location = { City = London, Country = UK } }


{ Name = Elizabeth Brown, Location = { City = London, Country = UK } }


{ Name = Ann Devon, Location = { City = London, Country = UK } }


{ Name = Simon Crowther, Location = { City = London, Country = UK } }


{ Name = Hari Kumar, Location = { City = London, Country = UK } }



 


A better looking query, a better looking result, and it works no matter how many Select’s or Where’s I add, no matter how complex I make each projection.


 


 


 


At least I’ll let you think that until I point out the next gaping hole. J


 


Until next time!

