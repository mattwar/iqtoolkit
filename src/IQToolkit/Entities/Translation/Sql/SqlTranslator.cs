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
    /// Applies language specific rules to a <see cref="SqlExpression"/> query.
    /// </summary>
    public abstract class SqlTranslator : LanguageTranslator
    {
        public override QueryLanguage Language { get; }

        /// <summary>
        /// Construct a <see cref="LanguageTranslator"/>
        /// </summary>
        public SqlTranslator(QueryLanguage language)
        {
            this.Language = language;
        }

        /// <summary>
        /// Apply additional language related rewrites to the entire query.
        /// </summary>
        public override Expression ApplyLanguageRewrites(
            Expression expression,
            MappingTranslator mapper,
            PolicyTranslator police)
        {
            // pre-simplify to help 
            var simplified = expression.SimplifyQueries();

            // convert cross-apply and outer-apply joins into inner & left-outer-joins if possible
            var crossApplied = simplified.ConvertCrossApplyToInnerJoin(this);

            // convert cross joins into inner joins
            var crossJoined = crossApplied.ConvertCrossJoinToInnerJoin();

            return crossJoined;
        }

        public override Expression Parameterize(Expression expression)
        {
            return ClientParameterRewriter.Rewrite(this.Language, expression);
        }

        public override bool AllowsMultipleCommands => false;
        public override bool AllowSubqueryInSelectWithoutFrom => false;
        public override bool AllowDistinctInAggregates => false;

        public override Expression GetRowsAffectedExpression(Expression command)
        {
            return new ScalarFunctionCallExpression(typeof(int), "@@ROWCOUNT", null);
        }

        public override bool IsRowsAffectedExpressions(Expression expression)
        {
            return expression is ScalarFunctionCallExpression fex 
                && fex.Name == "@@ROWCOUNT";
        }

        public override Expression GetOuterJoinTest(Expression expression)
        {
            if (expression is SelectExpression select)
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
            }

            // fall back to introducing a constant
            return Expression.Constant(1, typeof(int?));
        }

        /// <summary>
        /// Adds an outer join test to a projection expression.
        /// </summary>
        public override Expression AddOuterJoinTest(Expression expression)
        {
            if (expression is ClientProjectionExpression proj)
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

            return expression;
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
        public override bool IsScalar(Type type)
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

        public override bool IsAggregate(MemberInfo member)
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

        public override bool AggregateArgumentIsPredicate(string aggregateName)
        {
            return aggregateName == "Count"
                || aggregateName == "LongCount";
        }

        /// <summary>
        /// Determines whether the given expression can be represented as a column in a select expressionss
        /// </summary>
        public override bool CanBeColumn(Expression expression)
        {
            return this.MustBeColumn(expression)
                || this.IsScalar(expression.Type);
        }

        /// <summary>
        /// Determines whether the given expression must be represented as a column in a SELECT column list
        /// </summary>
        public override bool MustBeColumn(Expression expression)
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