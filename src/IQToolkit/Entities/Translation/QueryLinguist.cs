// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Expressions.Sql;
    using Entities.Mapping;
    using Utils;

    /// <summary>
    /// Applies language specific rules to a query.
    /// </summary>
    public abstract class QueryLinguist
    {
        public QueryLanguage Language { get; }

        /// <summary>
        /// Construct a <see cref="QueryLinguist"/>
        /// </summary>
        public QueryLinguist(QueryLanguage language)
        {
            this.Language = language;
        }

        /// <summary>
        /// Apply additional language rewrites.
        /// </summary>
        public virtual Expression Apply(
            Expression expression,
            QueryMapper mapper,
            QueryPolice police)
        {
            // pre-simplify to help 
            var simplified = expression.SimplifyQueries();

            // convert cross-apply and outer-apply joins into inner & left-outer-joins if possible
            var crossApplied = simplified.ConvertCrossApplyToInnerJoin(this);

            // convert cross joins into inner joins
            var crossJoined = crossApplied.ConvertCrossJoinToInnerJoin();

            return crossJoined;
        }

        /// <summary>
        /// Format the <see cref="SqlExpression"/> as query language text.
        /// </summary>
        public abstract FormattedQuery Format(SqlExpression expression, QueryOptions? options = null);

        /// <summary>
        /// Get an expression that selects and entity's generated ID.
        /// </summary>
        public abstract Expression GetGeneratedIdExpression(MappedColumnMember member);

        /// <summary>
        /// Determine which sub-expressions must be parameters
        /// </summary>
        public virtual Expression Parameterize(
            Expression expression)
        {
            return ClientParameterRewriter.Rewrite(this.Language, expression);
        }

        /// <summary>
        /// True if the language allows multiple commands to be executed in one query.
        /// </summary>
        public virtual bool AllowsMultipleCommands
        {
            get { return false; }
        }

        /// <summary>
        /// True if it is legal to represent a subquery in a SELECT statement that has no FROM clause.
        /// </summary>
        public virtual bool AllowSubqueryInSelectWithoutFrom
        {
            get { return false; }
        }

        /// <summary>
        /// True if DISTINCT is allows in an aggregate expression.
        /// </summary>
        public virtual bool AllowDistinctInAggregates
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an expression that evaluates to the number of rows affected by the last command.
        /// </summary>
        public virtual Expression GetRowsAffectedExpression(Expression command)
        {
            return new ScalarFunctionCallExpression(typeof(int), "@@ROWCOUNT", null);
        }

        /// <summary>
        /// True if the expression is a rows-affected expression.
        /// </summary>
        public virtual bool IsRowsAffectedExpressions(Expression expression)
        {
            return expression is ScalarFunctionCallExpression fex && fex.Name == "@@ROWCOUNT";
        }

        /// <summary>
        /// Gets an expression that be used by a query to determines if an outer join had a successful match
        /// (as opposed to null columns when no match occurs).
        /// </summary>
        public virtual Expression GetOuterJoinTest(SelectExpression select)
        {
            // if the column is used in the join condition (equality test)
            // if it is null in the database then the join test won't match (null != null) so the row won't appear
            // we can safely use this existing column as our test to determine if the outer join produced a row

            // find a column that is used in equality test
            var aliases = DeclaredAliasGatherer.Gather(select.From);
            var joinColumns = JoinColumnGatherer.Gather(aliases, select);

            if (joinColumns.Count > 0)
            {
                // prefer one that is already in the projection list.
                foreach (var jc in joinColumns)
                {
                    foreach (var col in select.Columns)
                    {
                        if (jc.Equals(col.Expression))
                        {
                            return jc;
                        }
                    }
                }

                return joinColumns[0];
            }

            // fall back to introducing a constant
            return Expression.Constant(1, typeof(int?));
        }

        /// <summary>
        /// Adds an outer join test to a projection expression.. 
        /// </summary>
        public virtual ClientProjectionExpression AddOuterJoinTest(ClientProjectionExpression proj)
        {
            var test = this.GetOuterJoinTest(proj.Select);
            var select = proj.Select;
            ColumnExpression? testCol = null;

            // look to see if test expression exists in columns already
            foreach (var col in select.Columns)
            {
                if (test.Equals(col.Expression)
                    && this.Language.TypeSystem.GetQueryType(test.Type) is { } colType)
                {
                    testCol = new ColumnExpression(test.Type, colType, select.Alias, col.Name);
                    break;
                }
            }

            if (testCol == null)
            {
                // add expression to projection
                testCol = test as ColumnExpression;
                string colName = (testCol != null) ? testCol.Name : "Test";
                colName = proj.Select.Columns.GetAvailableColumnName(colName);
                var colType = this.Language.TypeSystem.GetQueryType(test.Type);
                select = select.AddColumn(new ColumnDeclaration(colName, test, colType));
                testCol = new ColumnExpression(test.Type, colType, select.Alias, colName);
            }

            var newProjector = new OuterJoinedExpression(testCol, proj.Projector);
            return new ClientProjectionExpression(select, newProjector, proj.Aggregator);
        }

        /// <summary>
        /// Gets all columns used by a join expression
        /// </summary>
        class JoinColumnGatherer
        {
            private readonly HashSet<TableAlias> _aliases;
            private readonly HashSet<ColumnExpression> _columns =
                new HashSet<ColumnExpression>();

            private JoinColumnGatherer(HashSet<TableAlias> aliases)
            {
                _aliases = aliases;
            }

            public static IReadOnlyList<ColumnExpression> Gather(HashSet<TableAlias> aliases, SelectExpression select)
            {
                if (select.Where != null)
                {
                    var gatherer = new JoinColumnGatherer(aliases);
                    gatherer.Gather(select.Where);
                    return gatherer._columns.ToReadOnly();
                }
                else
                {
                    return Array.Empty<ColumnExpression>();
                }
            }

            private void Gather(Expression expression)
            {
                if (expression is BinaryExpression b)
                {
                    switch (b.NodeType)
                    {
                        case ExpressionType.Equal:
                        case ExpressionType.NotEqual:
                            if (IsExternalColumn(b.Left) && GetColumn(b.Right) is { } rightColumn)
                            {
                                _columns.Add(rightColumn);
                            }
                            else if (IsExternalColumn(b.Right) && GetColumn(b.Left) is { } leftColumn)
                            {
                                _columns.Add(leftColumn);
                            }
                            break;
                        case ExpressionType.And:
                        case ExpressionType.AndAlso:
                            if (b.Type == typeof(bool) || b.Type == typeof(bool?))
                            {
                                this.Gather(b.Left);
                                this.Gather(b.Right);
                            }
                            break;
                    }
                }
            }

            private ColumnExpression? GetColumn(Expression exp)
            {
                while (exp is UnaryExpression ux && ux.NodeType == ExpressionType.Convert)
                {
                    exp = ux.Operand;
                }

                return exp as ColumnExpression;
            }

            private bool IsExternalColumn(Expression exp)
            {
                return GetColumn(exp) is { } col
                    && !_aliases.Contains(col.Alias);
            }
        }

        /// <summary>
        /// Determines whether the CLR type corresponds to a scalar data type in the query language
        /// </summary>
        public virtual bool IsScalar(Type type)
        {
            type = TypeHelper.GetNonNullableType(type);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                    return false;
                case TypeCode.Object:
                    return
                        type == typeof(DateTimeOffset) ||
                        type == typeof(TimeSpan) ||
                        type == typeof(Guid) ||
                        type == typeof(byte[]);
                default:
                    return true;
            }
        }

        public virtual bool IsAggregate(MemberInfo member)
        {
            var method = member as MethodInfo;
            if (method != null)
            {
                if (method.DeclaringType == typeof(Queryable)
                    || method.DeclaringType == typeof(Enumerable))
                {
                    switch (method.Name)
                    {
                        case "Count":
                        case "LongCount":
                        case "Sum":
                        case "Min":
                        case "Max":
                        case "Average":
                            return true;
                    }
                }
            }

            if (member is PropertyInfo property
                && property.Name == "Count"
                && typeof(IEnumerable).IsAssignableFrom(property.DeclaringType))
            {
                return true;
            }

            return false;
        }

        public virtual bool AggregateArgumentIsPredicate(string aggregateName)
        {
            return aggregateName == "Count"
                || aggregateName == "LongCount";
        }

        /// <summary>
        /// Determines whether the given expression can be represented as a column in a select expressionss
        /// </summary>
        public virtual bool CanBeColumn(Expression expression)
        {
            return this.MustBeColumn(expression)
                || this.IsScalar(expression.Type);
        }

        /// <summary>
        /// Determines whether the given expression must be represented as a column in a SELECT column list
        /// </summary>
        public virtual bool MustBeColumn(Expression expression)
        {
            switch (expression)
            {
                case ColumnExpression _:
                case SubqueryExpression _:
                case AggregateExpression _:
                    return true;
                case TaggedExpression tagged:
                    return MustBeColumn(tagged.Expression);
                default:
                    return false;
            }
        }
    }
}