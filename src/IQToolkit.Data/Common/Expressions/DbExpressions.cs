// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Extended node types for custom expressions
    /// </summary>
    public enum DbExpressionType
    {
        Table               = 1000, // make sure these don't overlap with ExpressionType
        ClientJoin          = 1001,
        Column              = 1002,
        Select              = 1003,
        Projection          = 1004,
        Entity              = 1005,
        Join                = 1006,
        Aggregate           = 1007,
        Scalar              = 1008,
        Exists              = 1009,
        In                  = 1010,
        Grouping            = 1011,
        AggregateSubquery   = 1012,
        IsNull              = 1013,
        Between             = 1014,
        RowCount            = 1015,
        NamedValue          = 1016,
        OuterJoined         = 1017,
        Insert              = 1018,
        Update              = 1019,
        Delete              = 1020,
        Batch               = 1021,
        Function            = 1022,
        Block               = 1023,
        If                  = 1024,
        Declaration         = 1025,
        Variable            = 1026
    }

    public static class DbExpressionTypeExtensions
    {
        public static bool IsDbExpression(this ExpressionType et)
        {
            return ((int)et) >= 1000;
        }

        public static string GetNodeTypeName(this Expression e)
        {
            if (e is DbExpression d)
            {
                return d.ExpressionType.ToString();
            }
            else
            {
                return e.NodeType.ToString();
            }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebugText}")]
    public abstract class DbExpression : Expression
    {
        private readonly DbExpressionType expressionType;
        private readonly Type type;

        protected DbExpression(DbExpressionType eType, Type type)
        {
            this.expressionType = eType;
            this.type = type;
        }

        public DbExpressionType ExpressionType
        {
            get { return this.expressionType; }
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)(int)this.expressionType; }
        }

        public override Type Type
        {
            get { return this.type; }
        }

        private string DebugText
        {
            get { return $"{this.GetType().Name}: {this.GetNodeTypeName()} := {this.ToString()}"; }
        }

        public override string ToString()
        {
            return DbExpressionWriter.WriteToString(this);
        }
    }

    public abstract class AliasedExpression : DbExpression
    {
        public TableAlias Alias { get; }

        protected AliasedExpression(DbExpressionType nodeType, Type type, TableAlias alias)
            : base(nodeType, type)
        {
            this.Alias = alias;
        }
    }

    /// <summary>
    /// A custom expression node that represents a table reference in a SQL query
    /// </summary>
    public class TableExpression : AliasedExpression
    {
        public MappingEntity Entity { get; }
        public string Name { get; }

        public TableExpression(TableAlias alias, MappingEntity entity, string name)
            : base(DbExpressionType.Table, typeof(void), alias)
        {
            this.Entity = entity;
            this.Name = name;
        }

        public override string ToString()
        {
            return "T(" + this.Name + ")";
        }
    }

    public class EntityExpression : DbExpression
    {
        public MappingEntity Entity { get; }
        public Expression Expression { get; }

        public EntityExpression(MappingEntity entity, Expression expression)
            : base(DbExpressionType.Entity, expression.Type)
        {
            this.Entity = entity;
            this.Expression = expression;
        }
    }

    /// <summary>
    /// A custom expression node that represents a reference to a column in a SQL query
    /// </summary>
    public class ColumnExpression : DbExpression, IEquatable<ColumnExpression>
    {
        public TableAlias Alias { get; }
        public string Name { get; }
        public QueryType QueryType { get; }


        public ColumnExpression(Type type, QueryType queryType, TableAlias alias, string name)
            : base(DbExpressionType.Column, type)
        {
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            if (name == null)
                throw new ArgumentNullException("name");
            this.Alias = alias;
            this.Name = name;
            this.QueryType = queryType;
        }

        public override string ToString()
        {
            return this.Alias.ToString() + ".C(" + this.Name + ")";
        }

        public override int GetHashCode()
        {
            return this.Alias.GetHashCode() + this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ColumnExpression);
        }

        public bool Equals(ColumnExpression other)
        {
            return other != null
                && ((object)this) == (object)other
                 || (this.Alias == other.Alias && this.Name == other.Name);
        }
    }

    public class TableAlias
    {
        public TableAlias()
        {
        }

        public override string ToString()
        {
            return "A:" + this.GetHashCode();
        }
    }

    /// <summary>
    /// A declaration of a column in a SQL SELECT expression
    /// </summary>
    public class ColumnDeclaration
    {
        public string Name { get; }
        public Expression Expression { get; }
        public QueryType QueryType { get; }

        public ColumnDeclaration(string name, Expression expression, QueryType queryType)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (expression == null)
                throw new ArgumentNullException("expression");
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            this.Name = name;
            this.Expression = expression;
            this.QueryType = queryType;
        }
    }

    /// <summary>
    /// An SQL OrderBy order type 
    /// </summary>
    public enum OrderType
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// A pairing of an expression and an order type for use in a SQL Order By clause
    /// </summary>
    public class OrderExpression
    {
        public OrderType OrderType { get; }
        public Expression Expression { get; }

        public OrderExpression(OrderType orderType, Expression expression)
        {
            this.OrderType = orderType;
            this.Expression = expression;
        }
    }

    /// <summary>
    /// A custom expression node used to represent a SQL SELECT expression
    /// </summary>
    public class SelectExpression : AliasedExpression
    {
        public ReadOnlyCollection<ColumnDeclaration> Columns { get; }
        public Expression From { get; }
        public Expression Where { get; }
        public ReadOnlyCollection<OrderExpression> OrderBy { get; }
        public ReadOnlyCollection<Expression> GroupBy { get; }
        public bool IsDistinct { get; }
        public Expression Skip { get; }
        public Expression Take { get; }
        public bool IsReverse { get; }

        public SelectExpression(
            TableAlias alias,
            IEnumerable<ColumnDeclaration> columns,
            Expression from,
            Expression where,
            IEnumerable<OrderExpression> orderBy,
            IEnumerable<Expression> groupBy,
            bool isDistinct,
            Expression skip,
            Expression take,
            bool isReverse
            )
            : base(DbExpressionType.Select, typeof(void), alias)
        {
            this.Columns = columns.ToReadOnly();
            this.IsDistinct = isDistinct;
            this.From = from;
            this.Where = where;
            this.OrderBy = orderBy.ToReadOnly();
            this.GroupBy = groupBy.ToReadOnly();
            this.Take = take;
            this.Skip = skip;
            this.IsReverse = isReverse;
        }

        public SelectExpression(
            TableAlias alias,
            IEnumerable<ColumnDeclaration> columns,
            Expression from,
            Expression where,
            IEnumerable<OrderExpression> orderBy,
            IEnumerable<Expression> groupBy
            )
            : this(alias, columns, from, where, orderBy, groupBy, false, null, null, false)
        {
        }

        public SelectExpression(
            TableAlias alias, IEnumerable<ColumnDeclaration> columns,
            Expression from, Expression where
            )
            : this(alias, columns, from, where, null, null)
        {
        }

        public string QueryText => SqlFormatter.Format(this, true);
    }

    /// <summary>
    /// A kind of SQL join
    /// </summary>
    public enum JoinType
    {
        CrossJoin,
        InnerJoin,
        CrossApply,
        OuterApply,
        LeftOuter,
        SingletonLeftOuter
    }

    /// <summary>
    /// A custom expression node representing a SQL join clause
    /// </summary>
    public class JoinExpression : DbExpression
    {
        public JoinType Join { get; }
        public Expression Left { get; }
        public Expression Right { get; }
        public new Expression Condition { get; }

        public JoinExpression(JoinType joinType, Expression left, Expression right, Expression condition)
            : base(DbExpressionType.Join, typeof(void))
        {
            this.Join = joinType;
            this.Left = left;
            this.Right = right;
            this.Condition = condition;
        }
    }

    public class OuterJoinedExpression : DbExpression
    {
        public Expression Test { get; }
        public Expression Expression { get; }

        public OuterJoinedExpression(Expression test, Expression expression)
            : base(DbExpressionType.OuterJoined, expression.Type)
        {
            this.Test = test;
            this.Expression = expression;
        }
    }

    public abstract class SubqueryExpression : DbExpression
    {
        public SelectExpression Select { get; }

        protected SubqueryExpression(DbExpressionType eType, Type type, SelectExpression select)
            : base(eType, type)
        {
            System.Diagnostics.Debug.Assert(eType == DbExpressionType.Scalar || eType == DbExpressionType.Exists || eType == DbExpressionType.In);
            this.Select = select;
        }
    }

    public class ScalarExpression : SubqueryExpression
    {
        public ScalarExpression(Type type, SelectExpression select)
            : base(DbExpressionType.Scalar, type, select)
        {
        }
    }

    public class ExistsExpression : SubqueryExpression
    {
        public ExistsExpression(SelectExpression select)
            : base(DbExpressionType.Exists, typeof(bool), select)
        {
        }
    }

    public class InExpression : SubqueryExpression
    {
        // either select expression or values are assigned
        public Expression Expression { get; }
        public ReadOnlyCollection<Expression> Values { get; }

        public InExpression(Expression expression, SelectExpression select)
            : base(DbExpressionType.In, typeof(bool), select)
        {
            this.Expression = expression;
        }

        public InExpression(Expression expression, IEnumerable<Expression> values)
            : base(DbExpressionType.In, typeof(bool), null)
        {
            this.Expression = expression;
            this.Values = values.ToReadOnly();
        }
    }

    public class AggregateExpression : DbExpression
    {
        public string AggregateName { get; }
        public Expression Argument { get; }
        public bool IsDistinct { get; }

        public AggregateExpression(Type type, string aggregateName, Expression argument, bool isDistinct)
            : base(DbExpressionType.Aggregate, type)
        {
            this.AggregateName = aggregateName;
            this.Argument = argument;
            this.IsDistinct = isDistinct;
        }
    }

    public class AggregateSubqueryExpression : DbExpression
    {
        public TableAlias GroupByAlias { get; }
        public Expression AggregateInGroupSelect { get; }
        public ScalarExpression AggregateAsSubquery { get; }

        public AggregateSubqueryExpression(TableAlias groupByAlias, Expression aggregateInGroupSelect, ScalarExpression aggregateAsSubquery)
            : base(DbExpressionType.AggregateSubquery, aggregateAsSubquery.Type)
        {
            this.AggregateInGroupSelect = aggregateInGroupSelect;
            this.GroupByAlias = groupByAlias;
            this.AggregateAsSubquery = aggregateAsSubquery;
        }
    }

    /// <summary>
    /// Allows is-null tests against value-types like int and float
    /// </summary>
    public class IsNullExpression : DbExpression
    {
        public Expression Expression { get; }

        public IsNullExpression(Expression expression)
            : base(DbExpressionType.IsNull, typeof(bool))
        {
            this.Expression = expression;
        }
    }

    public class BetweenExpression : DbExpression
    {
        public Expression Expression { get; }
        public Expression Lower { get; }
        public Expression Upper { get; }

        public BetweenExpression(Expression expression, Expression lower, Expression upper)
            : base(DbExpressionType.Between, expression.Type)
        {
            this.Expression = expression;
            this.Lower = lower;
            this.Upper = upper;
        }
    }

    public class RowNumberExpression : DbExpression
    {
        public ReadOnlyCollection<OrderExpression> OrderBy { get; }

        public RowNumberExpression(IEnumerable<OrderExpression> orderBy)
            : base(DbExpressionType.RowCount, typeof(int))
        {
            this.OrderBy = orderBy.ToReadOnly();
        }
    }

    public class NamedValueExpression : DbExpression
    {
        public string Name { get; }
        public QueryType QueryType { get; }
        public Expression Value { get; }

        public NamedValueExpression(string name, QueryType queryType, Expression value)
            : base(DbExpressionType.NamedValue, value.Type)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            //if (queryType == null)
            //    throw new ArgumentNullException("queryType");
            if (value == null)
                throw new ArgumentNullException("value");
            this.Name = name;
            this.QueryType = queryType;
            this.Value = value;
        }
    }

    /// <summary>
    /// A custom expression representing the construction of one or more result objects from a 
    /// SQL select expression
    /// </summary>
    public class ProjectionExpression : DbExpression
    {
        public SelectExpression Select { get; }
        public Expression Projector { get; }
        public LambdaExpression Aggregator { get; }

        public ProjectionExpression(SelectExpression source, Expression projector)
            : this(source, projector, null)
        {
        }

        public ProjectionExpression(SelectExpression source, Expression projector, LambdaExpression aggregator)
            : base(DbExpressionType.Projection, aggregator != null ? aggregator.Body.Type : typeof(IEnumerable<>).MakeGenericType(projector.Type))
        {
            this.Select = source;
            this.Projector = projector;
            this.Aggregator = aggregator;
        }

        public bool IsSingleton => this.Aggregator?.Body.Type == this.Projector.Type;
        public override string ToString() => DbExpressionWriter.WriteToString(this);
        public string QueryText => SqlFormatter.Format(this.Select, true);
    }

    public class ClientJoinExpression : DbExpression
    {
        public ReadOnlyCollection<Expression> OuterKey { get; }
        public ReadOnlyCollection<Expression> InnerKey { get; }
        public ProjectionExpression Projection { get; }

        public ClientJoinExpression(ProjectionExpression projection, IEnumerable<Expression> outerKey, IEnumerable<Expression> innerKey)
            : base(DbExpressionType.ClientJoin, projection.Type)
        {
            this.OuterKey = outerKey.ToReadOnly();
            this.InnerKey = innerKey.ToReadOnly();
            this.Projection = projection;
        }
    }

    public class BatchExpression : Expression
    {
        public override Type Type { get; }
        public Expression Input { get; }
        public LambdaExpression Operation { get; }
        public Expression BatchSize { get; }
        public Expression Stream { get; }

        public BatchExpression(Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
        {
            this.Input = input;
            this.Operation = operation;
            this.BatchSize = batchSize;
            this.Stream = stream;
            this.Type = typeof(IEnumerable<>).MakeGenericType(operation.Body.Type);
        }

        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Batch;
    }

    public class FunctionExpression : DbExpression
    {
        public string Name { get; }
        public ReadOnlyCollection<Expression> Arguments { get; }

        public FunctionExpression(Type type, string name, IEnumerable<Expression> arguments)
            : base(DbExpressionType.Function, type)
        {
            this.Name = name;
            this.Arguments = arguments.ToReadOnly();
        }
    }

    public abstract class CommandExpression : DbExpression
    {
        protected CommandExpression(DbExpressionType eType, Type type)
            : base(eType, type)
        {
        }
    }

    public class InsertCommand : CommandExpression
    {
        public TableExpression Table { get; }
        public ReadOnlyCollection<ColumnAssignment> Assignments { get; }

        public InsertCommand(TableExpression table, IEnumerable<ColumnAssignment> assignments)
            : base(DbExpressionType.Insert, typeof(int))
        {
            this.Table = table;
            this.Assignments = assignments.ToReadOnly();
        }
    }

    public class ColumnAssignment
    {
        public ColumnExpression Column { get; }
        public Expression Expression { get; }

        public ColumnAssignment(ColumnExpression column, Expression expression)
        {
            this.Column = column;
            this.Expression = expression;
        }
    }

    public class UpdateCommand : CommandExpression
    {
        public TableExpression Table { get; }
        public Expression Where { get; }
        public ReadOnlyCollection<ColumnAssignment> Assignments { get; }

        public UpdateCommand(TableExpression table, Expression where, IEnumerable<ColumnAssignment> assignments)
            : base(DbExpressionType.Update, typeof(int))
        {
            this.Table = table;
            this.Where = where;
            this.Assignments = assignments.ToReadOnly();
        }
    }

    public class DeleteCommand : CommandExpression
    {
        public TableExpression Table { get; }
        public Expression Where { get; }

        public DeleteCommand(TableExpression table, Expression where)
            : base(DbExpressionType.Delete, typeof(int))
        {
            this.Table = table;
            this.Where = where;
        }
    }

    public class IFCommand : CommandExpression
    {
        public Expression Check { get; }
        public Expression IfTrue { get; }
        public Expression IfFalse { get; }

        public IFCommand(Expression check, Expression ifTrue, Expression ifFalse)
            : base(DbExpressionType.If, ifTrue.Type)
        {
            this.Check = check;
            this.IfTrue = ifTrue;
            this.IfFalse = ifFalse;
        }
    }

    public class BlockCommand : CommandExpression
    {
        public ReadOnlyCollection<Expression> Commands { get; }

        public BlockCommand(IList<Expression> commands)
            : base(DbExpressionType.Block, commands[commands.Count-1].Type)
        {
            this.Commands = commands.ToReadOnly();
        }

        public BlockCommand(params Expression[] commands) 
            : this((IList<Expression>)commands)
        {
        }
    }

    public class DeclarationCommand : CommandExpression
    {
        public ReadOnlyCollection<VariableDeclaration> Variables { get; }
        public SelectExpression Source { get; }

        public DeclarationCommand(IEnumerable<VariableDeclaration> variables, SelectExpression source)
            : base(DbExpressionType.Declaration, typeof(void))
        {
            this.Variables = variables.ToReadOnly();
            this.Source = source;
        }
    }

    public class VariableDeclaration
    {
        public string Name { get; }
        public QueryType QueryType { get; }
        public Expression Expression { get; }

        public VariableDeclaration(string name, QueryType queryType, Expression expression)
        {
            this.Name = name;
            this.QueryType = queryType;
            this.Expression = expression;
        }
    }

    public class VariableExpression : Expression
    {
        public string Name { get; }
        public override Type Type { get; }
        public QueryType QueryType { get; }

        public VariableExpression(string name, Type type, QueryType queryType)
        {
            this.Name = name;
            this.Type = type;
            this.QueryType = queryType;
        }

        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Variable;
    }
}
