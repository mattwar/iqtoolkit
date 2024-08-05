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
    public abstract class LanguageTranslator
    {
        public abstract QueryLanguage Language { get; }

        /// <summary>
        /// Apply additional language related rewrites to the entire query.
        /// </summary>
        public abstract Expression ApplyLanguageRewrites(
            Expression expression,
            MappingTranslator mapper,
            PolicyTranslator police);

        /// <summary>
        /// Format the <see cref="SqlExpression"/> as query language text.
        /// </summary>
        public abstract FormattedQuery Format(SqlExpression expression, QueryOptions? options = null);

        /// <summary>
        /// Determine which sub-expressions must be parameters
        /// </summary>
        public abstract Expression Parameterize(Expression expression);

        /// <summary>
        /// True if the language allows multiple commands to be executed in one query.
        /// </summary>
        public abstract bool AllowsMultipleCommands { get; }

        /// <summary>
        /// True if it is legal to represent a subquery in a SELECT statement that has no FROM clause.
        /// </summary>
        public abstract bool AllowSubqueryInSelectWithoutFrom { get; }

        /// <summary>
        /// True if DISTINCT is allows in an aggregate expression.
        /// </summary>
        public abstract bool AllowDistinctInAggregates { get; }

        /// <summary>
        /// Gets an expression that selects an entity's generated ID.
        /// </summary>
        public abstract Expression GetGeneratedIdExpression(MappedColumnMember member);

        /// <summary>
        /// Gets an expression that evaluates to the number of rows affected by the last command.
        /// </summary>
        public abstract Expression GetRowsAffectedExpression(Expression command);

        /// <summary>
        /// True if the expression is a rows-affected expression.
        /// </summary>
        public abstract bool IsRowsAffectedExpressions(Expression expression);

        /// <summary>
        /// Gets an expression that be used by a query to determines if an outer join had a successful match
        /// (as opposed to null columns when no match occurs).
        /// </summary>
        public abstract Expression GetOuterJoinTest(Expression select);

        /// <summary>
        /// Adds an outer join test to a projection expression.. 
        /// </summary>
        public abstract Expression AddOuterJoinTest(Expression proj);

        /// <summary>
        /// Determines whether the CLR type corresponds to a scalar data type in the query language
        /// </summary>
        public abstract bool IsScalar(Type type);

        /// <summary>
        /// Returns true if the language considers the method to be an aggregate.
        /// </summary>
        public abstract bool IsAggregate(MemberInfo member);

        /// <summary>
        /// Returns true if the aggregate has a predicate argument.
        /// </summary>
        public abstract bool AggregateArgumentIsPredicate(string aggregateName);

        /// <summary>
        /// Returns true if the expression can be projected into a column of the query result.
        /// </summary>
        public abstract bool CanBeColumn(Expression expression);

        /// <summary>
        /// Returns true if the expression must be projected into a column of the query result.
        /// </summary>
        public abstract bool MustBeColumn(Expression expression);
    }
}