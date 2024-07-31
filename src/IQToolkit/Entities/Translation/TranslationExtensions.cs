using System.Diagnostics;
using System.Linq.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using SqlExpressions;
    using System.Collections;
    using System.Collections.Generic;

    public static class TranslationExtensions
    {
        /// <summary>
        /// Add included relationships to entity expressions.
        /// </summary>
        public static Expression AddIncludedRelationships(this Expression expression, QueryPolicy policy, QueryMappingRewriter mappingRewriter)
        {
            var rewritten = RelationshipIncluder.Include(expression, policy, mappingRewriter);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");

            return rewritten != expression
                ? rewritten.SimplifyQueries()
                : expression;
        }

        /// <summary>
        /// Converts comparisions of entities into comparisons of the primary key values.
        /// </summary>
        public static Expression ConvertEntityComparisons(this Expression expression, EntityMapping mapping)
        {
            var rewritten = new EntityComparisonRewriter(mapping).Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Converts CROSS APPLY and OUTER APPLY joins to INNER and LEFT OUTER joins.
        /// </summary>
        public static Expression ConvertCrossApplyToInnerJoin(this Expression expression, QueryLanguage language)
        {
            // simplify before attempting to convert cross applies.
            var rewritten = new CrossApplyToLeftJoinRewriter(language).Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Converts cross joins FROM A,B to inner joins FROM A INNER JOIN B
        /// </summary>
        public static Expression ConvertCrossJoinToInnerJoin(this Expression expression)
        {
            var rewritten = new CrossJoinIsolator().Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");

            return (rewritten != expression)
                ? rewritten.SimplifyQueries()
                : rewritten;
        }

        /// <summary>
        /// rewrites nested projections into client-side joins
        /// </summary>
        public static Expression ConvertNestedProjectionsToClientJoins(this Expression expression, QueryPolicy policy, QueryLanguage language)
        {
            var rewritten = new ClientProjectionToClientJoinRewriter(policy, language).Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");

            return rewritten != expression
                ? rewritten.SimplifyQueries()
                : expression;
        }

        /// <summary>
        /// Converts LINQ <see cref="System.Linq.Queryable"/> query operators to DbExpressions.
        /// </summary>
        public static Expression ConvertQueryOperatorsToDbExpressions(
            this Expression expression,
            QueryLanguage language,
            QueryMappingRewriter mapper, 
            bool isQueryFragment = false)
        {
            var rewriter = new LinqToDbExpressionRewriter(language, mapper, expression);
            var rewritten = rewriter.Visit(expression);
            Debug.Assert(isQueryFragment || rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Converts accesses to relationship members into projections or joins
        /// </summary>
        public static Expression ConvertRelationshipAccesses(this Expression expression, QueryMappingRewriter mapper)
        {
            var rewritten = new RelationshipBinder(mapper).Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");

            return rewritten != expression
                ? rewritten.SimplifyQueries()
                : expression;
        }

        /// <summary>
        /// Converts nested singleton projection into server-side joins
        /// </summary>
        public static Expression ConvertSingletonProjections(
            this Expression expression, QueryLanguage language, EntityMapping mapping)
        {
            var rewritten = new SingletonProjectionRewriter(language).Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");

            if (rewritten != expression)
            {
                var simplified = rewritten.SimplifyQueries();
                var ecconverted = simplified.ConvertEntityComparisons(mapping);
                return ecconverted;
            }
            else
            {
                return expression;
            }
        }

        /// <summary>
        /// Rewrites SKIP s + TAKE n clause to nested queries using TOP
        /// </summary>
        public static Expression ConvertSkipTakeToTop(this Expression expression, QueryLanguage language)
        {
            var rewritten = new SkipTakeToTopRewriter(language).Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Duplicates the expression by making a copy with new table aliases.
        /// </summary>
        public static TExpression Duplicate<TExpression>(this TExpression expression)
            where TExpression : Expression
        {
            return (TExpression)QueryDuplicator.Duplicate(expression);
        }

        /// <summary>
        /// True if the column does not reference aliases that are not declared within the expression
        /// or are listed as a valid alias.
        /// </summary>
        public static bool IsSelfContained(this Expression expression, IEnumerable<TableAlias>? validAliases = null)
        {
            return SelfContainedReferencer.IsSelfContained(expression, validAliases);
        }

        /// <summary>
        /// Isolate cross joins from other joins pushing them down into nested subqueries.
        /// </summary>
        public static Expression IsolateCrossJoins(this Expression expression)
        {
            var rewritten = new CrossJoinIsolator().Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Moves order-bys in nested subqueries to the outer-most select if possible
        /// </summary>
        public static Expression MoveOrderByToOuterSelect(this Expression expression, QueryLanguage language)
        {
            var rewritten = new MoveOrderByToOuterMostSelectRewriter(language).Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Remaps all references to one or more aliases to a new alias.
        /// </summary>
        public static TExpression RemapTableAliases<TExpression>(this TExpression expression, TableAlias newAlias, IEnumerable<TableAlias> oldAliases)
            where TExpression : Expression
        {
            return (TExpression)new TableAliasRemapper(oldAliases, newAlias).Visit(expression);
        }

        /// <summary>
        /// Remaps all references to one or more aliases to a new alias.
        /// </summary>
        public static TExpression RemapTableAliases<TExpression>(this TExpression expression, TableAlias newAlias, params TableAlias[] oldAliases)
            where TExpression : Expression
        {
            return (TExpression)new TableAliasRemapper(oldAliases, newAlias).Visit(expression);
        }

        /// <summary>
        /// Removes duplicate column declarations that refer to the same underlying column
        /// </summary>
        public static Expression RemoveRedundantColumns(this Expression expression)
        {
            var rewritten = new RedundantColumnRemover().Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Removes joins expressions that are identical to joins that already exist
        /// </summary>
        public static Expression RemoveRedundantJoins(this Expression expression)
        {
            var rewritten = new RedundantJoinRemover().Visit(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Removes select expressions that don't add any additional semantic value
        /// </summary>
        public static Expression RemoveRedundantSubqueries(this Expression expression)
        {
            var rewritten = RedundantSubqueryRemover.RemoveRedudantSuqueries(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Merges select expressions with their immediate nested select if possible.
        /// </summary>
        public static Expression MergeSubqueries(this Expression expression)
        {
            var rewritten = SubqueryMerger.Merge(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Removes columns in <see cref="SelectExpression"/> that are not referenced
        /// or required.
        /// </summary>
        public static Expression RemoveUnusedColumns(this Expression expression)
        {
            var rewritten = UnusedColumnRemover.RemoveUnusedColumns(expression);
            Debug.Assert(rewritten.HasValidReferences(), "Invalid References or Declarations");
            return rewritten;
        }

        /// <summary>
        /// Simplifies queries by removing unused columns and redundant columns, subqueries, and joins.
        /// </summary>
        public static Expression SimplifyQueries(this Expression expression)
        {
            var e1 = expression.RemoveUnusedColumns();
            var e2 = e1.RemoveRedundantColumns();
            var e3 = e2.RemoveRedundantSubqueries();
            var e4 = e3.MergeSubqueries();
            var e5 = e4.RemoveRedundantJoins();
            return e5;
        }

        /// <summary>
        /// Returns true if the query has only valid references or declarations.
        /// </summary>
        public static bool HasValidReferences(this Expression query, SelectExpression? outerSelect = null)
        {
            return ValidReferenceChecker.HasValidReferences(query, outerSelect);
        }
    }
}