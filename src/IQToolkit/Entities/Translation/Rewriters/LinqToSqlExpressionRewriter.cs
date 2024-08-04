// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;
    using Mapping;
    using Utils;

    /// <summary>
    /// Rewrites LINQ operators to <see cref="SqlExpression"/> nodes.
    /// </summary>
    public class LinqToSqlExpressionRewriter : SqlExpressionVisitor
    {
        private readonly QueryLinguist _linguist;
        private readonly QueryMapper _mapper;
        private readonly QueryPolice _police;
        private readonly Dictionary<ParameterExpression, Expression> _map;
        private readonly Dictionary<Expression, GroupByInfo> _groupByMap;
        private Expression _root;
        private IEntityTable? _batchUpd;

        public LinqToSqlExpressionRewriter(
            QueryLinguist linguist,
            QueryMapper mapper,
            QueryPolice police,
            Expression root)
        {
            _mapper = mapper;
            _linguist = linguist;
            _police = police;
            _map = new Dictionary<ParameterExpression, Expression>();
            _groupByMap = new Dictionary<Expression, GroupByInfo>();
            _root = root;
        }

        private static LambdaExpression? GetLambda(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }

            if (e.NodeType == ExpressionType.Constant)
            {
                return ((ConstantExpression)e).Value as LambdaExpression;
            }

            return e as LambdaExpression;
        }

        internal TableAlias GetNextAlias()
        {
            return new TableAlias();
        }

        private ProjectedColumns ProjectColumns(Expression expression, TableAlias newAlias, params TableAlias[] existingAliases)
        {
            return ColumnProjector.ProjectColumns(_linguist, expression, null, newAlias, existingAliases);
        }

        public override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null!;

            var result = base.Visit(exp);

            // bindings that expect projections should have called VisitSequence, the rest will probably get annoyed if
            // the projection does not have the expected type.
            Type expectedType = exp.Type;
            if (result is ClientProjectionExpression projection)
            {
                if (_aggregateSubqueries.Count > 0)
                {
                    var itemsToRemove = _aggregateSubqueries.Where(asub =>
                        projection.FindFirstDownOrDefault<TaggedExpression>(ags =>
                            ags.Id == asub.SubqueryId) != null)
                        .ToReadOnly();

                    if (itemsToRemove.Count > 0)
                    {
                        // attempt to move aggregates into the source projection
                        projection = (ClientProjectionExpression)new AggregateRewriter(_linguist.Language, _aggregateSubqueries).Visit(projection);
                        _aggregateSubqueries = _aggregateSubqueries.RemoveAll(x => itemsToRemove.Contains(x));
                    }
                }

                if (projection.Aggregator == null
                    && !expectedType.IsAssignableFrom(projection.Type))
                {
                    var aggregator = Aggregator.GetAggregator(expectedType, projection.Type);
                    if (aggregator != null)
                    {
                        result = projection = new ClientProjectionExpression(projection.Select, projection.Projector, aggregator);
                    }
                }

                result = projection;
            }

            return result;
        }

        private static bool IsLinqOperator(MethodInfo method) =>
            method.DeclaringType == typeof(Queryable) 
            || method.DeclaringType == typeof(Enumerable);

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (IsLinqOperator(m.Method))
            {
                switch (m.Method.Name)
                {
                    case "Where":
                        return this.BindWhere(m.Type, m.Arguments[0], GetLambda(m.Arguments[1])!);
                    case "Select":
                        return this.BindSelect(m.Type, m.Arguments[0], GetLambda(m.Arguments[1])!);
                    case "SelectMany":
                        if (m.Arguments.Count == 2)
                        {
                            return this.BindSelectMany(m.Type, m.Arguments[0], GetLambda(m.Arguments[1])!, null);
                        }
                        else if (m.Arguments.Count == 3)
                        {
                            return this.BindSelectMany(m.Type, m.Arguments[0], GetLambda(m.Arguments[1])!, GetLambda(m.Arguments[2])!);
                        }
                        break;
                    case "Join":
                        return this.BindJoin(m.Type, m.Arguments[0], m.Arguments[1], GetLambda(m.Arguments[2])!, GetLambda(m.Arguments[3])!, GetLambda(m.Arguments[4])!);
                    case "GroupJoin":
                        if (m.Arguments.Count == 5)
                        {
                            return this.BindGroupJoin(m.Method, m.Arguments[0], m.Arguments[1], GetLambda(m.Arguments[2])!, GetLambda(m.Arguments[3])!, GetLambda(m.Arguments[4])!);
                        }
                        break;
                    case "OrderBy":
                        return this.BindOrderBy(m.Type, m.Arguments[0], GetLambda(m.Arguments[1])!, OrderType.Ascending);
                    case "OrderByDescending":
                        return this.BindOrderBy(m.Type, m.Arguments[0], GetLambda(m.Arguments[1])!, OrderType.Descending);
                    case "ThenBy":
                        return this.BindThenBy(m.Arguments[0], GetLambda(m.Arguments[1])!, OrderType.Ascending);
                    case "ThenByDescending":
                        return this.BindThenBy(m.Arguments[0], GetLambda(m.Arguments[1])!, OrderType.Descending);
                    case "GroupBy":
                        if (m.Arguments.Count == 2)
                        {
                            return this.BindGroupBy(m.Arguments[0], GetLambda(m.Arguments[1])!, null, null);
                        }
                        else if (m.Arguments.Count == 3)
                        {
                            var lambda1 = GetLambda(m.Arguments[1])!;
                            var lambda2 = GetLambda(m.Arguments[2])!;
                            if (lambda2.Parameters.Count == 1)
                            {
                                // second lambda is element selector
                                return this.BindGroupBy(m.Arguments[0], lambda1, lambda2, null);
                            }
                            else if (lambda2.Parameters.Count == 2)
                            {
                                // second lambda is result selector
                                return this.BindGroupBy(m.Arguments[0], lambda1, null, lambda2);
                            }
                        }
                        else if (m.Arguments.Count == 4)
                        {
                            return this.BindGroupBy(m.Arguments[0], GetLambda(m.Arguments[1])!, GetLambda(m.Arguments[2]), GetLambda(m.Arguments[3]));
                        }
                        break;
                    case "Distinct":
                        if (m.Arguments.Count == 1)
                        {
                            return this.BindDistinct(m.Arguments[0]);
                        }
                        break;
                    case "Skip":
                        if (m.Arguments.Count == 2)
                        {
                            return this.BindSkip(m.Arguments[0], m.Arguments[1]);
                        }
                        break;
                    case "Take":
                        if (m.Arguments.Count == 2)
                        {
                            return this.BindTake(m.Arguments[0], m.Arguments[1]);
                        }
                        break;
                    case "First":
                    case "FirstOrDefault":
                    case "Single":
                    case "SingleOrDefault":
                    case "Last":
                    case "LastOrDefault":
                        if (m.Arguments.Count == 1)
                        {
                            return this.BindFirst(m.Arguments[0], null, m.Method.Name, m == _root);
                        }
                        else if (m.Arguments.Count == 2)
                        {
                            return this.BindFirst(m.Arguments[0], GetLambda(m.Arguments[1])!, m.Method.Name, m == _root);
                        }
                        break;
                    case "Any":
                        if (m.Arguments.Count == 1)
                        {
                            return this.BindAnyAll(m.Arguments[0], m.Method, null, m == _root);
                        }
                        else if (m.Arguments.Count == 2)
                        {
                            return this.BindAnyAll(m.Arguments[0], m.Method, GetLambda(m.Arguments[1])!, m == _root);
                        }
                        break;
                    case "All":
                        if (m.Arguments.Count == 2)
                        {
                            return this.BindAnyAll(m.Arguments[0], m.Method, GetLambda(m.Arguments[1]), m == _root);
                        }
                        break;
                    case "Contains":
                        if (m.Arguments.Count == 2)
                        {
                            return this.BindContains(m.Arguments[0], m.Arguments[1], m == _root);
                        }
                        break;
                    case "Cast":
                        if (m.Arguments.Count == 1)
                        {
                            return this.BindCast(m.Arguments[0], m.Method.GetGenericArguments()[0]);
                        }
                        break;
                    case "Reverse":
                        return this.BindReverse(m.Arguments[0]);
                    case "Intersect":
                    case "Except":
                        if (m.Arguments.Count == 2)
                        {
                            return this.BindIntersect(m.Arguments[0], m.Arguments[1], m.Method.Name == "Except");
                        }
                        break;
                }
            }
            else if (typeof(Updatable).IsAssignableFrom(m.Method.DeclaringType))
            {
                IEntityTable upd = _batchUpd != null
                    ? _batchUpd
                    : (IEntityTable)((ConstantExpression)m.Arguments[0]).Value;

                switch (m.Method.Name)
                {
                    case "Insert":
                        return this.BindInsert(
                            upd,
                            m.Arguments[1],
                            m.Arguments.Count > 2 ? GetLambda(m.Arguments[2]) : null
                            );
                    case "Update":
                        return this.BindUpdate(
                            upd,
                            m.Arguments[1],
                            m.Arguments.Count > 2 ? GetLambda(m.Arguments[2]) : null,
                            m.Arguments.Count > 3 ? GetLambda(m.Arguments[3]) : null
                            );
                    case "InsertOrUpdate":
                        return this.BindInsertOrUpdate(
                            upd,
                            m.Arguments[1],
                            m.Arguments.Count > 2 ? GetLambda(m.Arguments[2]) : null,
                            m.Arguments.Count > 3 ? GetLambda(m.Arguments[3]) : null
                            );
                    case "Delete":
                        if (m.Arguments.Count == 2 && GetLambda(m.Arguments[1]) != null)
                        {
                            return this.BindDelete(upd, null, GetLambda(m.Arguments[1]));
                        }
                        return this.BindDelete(
                            upd,
                            m.Arguments[1],
                            m.Arguments.Count > 2 ? GetLambda(m.Arguments[2]) : null
                            );
                    case "Batch":
                        return this.BindBatch(
                            upd,
                            m.Arguments[1],
                            GetLambda(m.Arguments[2])!,
                            m.Arguments.Count > 3 ? m.Arguments[3] : Expression.Constant(50),
                            m.Arguments.Count > 4 ? m.Arguments[4] : Expression.Constant(false)
                            );
                }
            }
                
            if (_linguist.IsAggregate(m.Method))
            {
                return this.BindAggregate(
                    m.Arguments[0],
                    m.Method.Name,
                    m.Method.ReturnType,
                    m.Arguments.Count > 1 ? GetLambda(m.Arguments[1]) : null,
                    m == _root
                    );
            }

            return base.VisitMethodCall(m);
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if ((u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked)
                && u == _root)
            {
                _root = u.Operand;
            }
            return base.VisitUnary(u);
        }

        private ClientProjectionExpression RewriteSequence(
            Expression source)
        {
            // sure to call base.Visit in order to skip my override
            var sequence = this.ConvertToSequence(base.Visit(source));
            return sequence;
        }

        private ClientProjectionExpression ConvertToSequence(
            Expression expr)
        {
            switch (expr)
            {
                case ClientProjectionExpression project:
                    return project;
                case NewExpression nex:
                    if (expr.Type.IsGenericType && expr.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
                    {
                        return (ClientProjectionExpression)nex.Arguments[1];
                    }
                    goto default;
                case MemberExpression mx:
                    var bound = this.BindRelationshipProperty(mx);
                    if (bound.NodeType != ExpressionType.MemberAccess)
                        return this.ConvertToSequence(bound);
                    goto default;
                default:
                    if (this.GetNewExpression(expr) is { } n)
                    {
                        return ConvertToSequence(n);
                    }

                    throw new Exception(string.Format("The expression of type '{0}' is not a sequence", expr.Type));
            }
        }

        private Expression BindRelationshipProperty(
            MemberExpression mex)
        {
            if (mex.Expression is EntityExpression ex 
                && ex.Entity.RelationshipMembers.TryGetMemberByName(mex.Member.Name, out var rm))
            {
                return _mapper.GetMemberExpression(mex.Expression, rm, _linguist, _police);
            }

            return mex;
        }

        private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            var projection = this.RewriteSequence(source);
            
            _map[predicate.Parameters[0]] = projection.Projector;
            var where = this.Visit(predicate.Body);

            var alias = this.GetNextAlias();
            var pc = this.ProjectColumns(projection.Projector, alias, projection.Select.Alias);
            var final = new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, projection.Select, where),
                pc.Projector
                );

            return final;
        }

        private Expression BindReverse(Expression source)
        {
            ClientProjectionExpression projection = this.RewriteSequence(source);
            var alias = this.GetNextAlias();
            ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, projection.Select.Alias);
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, projection.Select, null).WithIsReverse(true),
                pc.Projector
                );
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            var sourceProjection = this.RewriteSequence(source);

            _map[selector.Parameters[0]] = sourceProjection.Projector;
            var selectorBody = this.Visit(selector.Body);

            var alias = this.GetNextAlias();
            var pc = this.ProjectColumns(selectorBody, alias, sourceProjection.Select.Alias);
            var result = new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, sourceProjection.Select, null),
                pc.Projector
                );

            return result;
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression? resultSelector)
        {
            ClientProjectionExpression projection = this.RewriteSequence(source);
            _map[collectionSelector.Parameters[0]] = projection.Projector;

            Expression collection = collectionSelector.Body;

            // check for DefaultIfEmpty
            bool defaultIfEmpty = false;
            var mcs = collection as MethodCallExpression;
            if (mcs != null && mcs.Method.Name == "DefaultIfEmpty" && mcs.Arguments.Count == 1 &&
                (mcs.Method.DeclaringType == typeof(Queryable) || mcs.Method.DeclaringType == typeof(Enumerable)))
            {
                collection = mcs.Arguments[0];
                defaultIfEmpty = true;
            }

            var collectionProjection = this.RewriteSequence(collection);
            bool isTable = collectionProjection.Select.From is TableExpression;
            JoinType joinType = isTable ? JoinType.CrossJoin : defaultIfEmpty ? JoinType.OuterApply : JoinType.CrossApply;
            if (joinType == JoinType.OuterApply)
            {
                collectionProjection = _linguist.AddOuterJoinTest(collectionProjection);
            }
            JoinExpression join = new JoinExpression(joinType, projection.Select, collectionProjection.Select, null);

            var alias = this.GetNextAlias();
            ProjectedColumns pc;
            if (resultSelector == null)
            {
                pc = this.ProjectColumns(collectionProjection.Projector, alias, projection.Select.Alias, collectionProjection.Select.Alias);
            }
            else
            {
                _map[resultSelector.Parameters[0]] = projection.Projector;
                _map[resultSelector.Parameters[1]] = collectionProjection.Projector;
                Expression result = this.Visit(resultSelector.Body);
                pc = this.ProjectColumns(result, alias, projection.Select.Alias, collectionProjection.Select.Alias);
            }
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, join, null),
                pc.Projector
                );
        }

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            var outerProjection = this.RewriteSequence(outerSource);
            var innerProjection = this.RewriteSequence(innerSource);
            _map[outerKey.Parameters[0]] = outerProjection.Projector;
            var outerKeyExpr = this.Visit(outerKey.Body);
            _map[innerKey.Parameters[0]] = innerProjection.Projector;
            var innerKeyExpr = this.Visit(innerKey.Body);
            _map[resultSelector.Parameters[0]] = outerProjection.Projector;
            _map[resultSelector.Parameters[1]] = innerProjection.Projector;
            var resultExpr = this.Visit(resultSelector.Body);
            var join = new JoinExpression(JoinType.InnerJoin, outerProjection.Select, innerProjection.Select, outerKeyExpr.Equal(innerKeyExpr));
            var alias = this.GetNextAlias();
            var pc = this.ProjectColumns(resultExpr, alias, outerProjection.Select.Alias, innerProjection.Select.Alias);
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, join, null),
                pc.Projector
                );
        }

        protected virtual Expression BindIntersect(Expression outerSource, Expression innerSource, bool negate)
        {
            // SELECT * FROM outer WHERE EXISTS(SELECT * FROM inner WHERE inner = outer))
            ClientProjectionExpression outerProjection = this.RewriteSequence(outerSource);
            ClientProjectionExpression innerProjection = this.RewriteSequence(innerSource);

            Expression exists = new ExistsSubqueryExpression(
                new SelectExpression(new TableAlias(), null, innerProjection.Select, innerProjection.Projector.Equal(outerProjection.Projector))
                );
            if (negate)
                exists = Expression.Not(exists);
            var alias = this.GetNextAlias();
            ProjectedColumns pc = this.ProjectColumns(outerProjection.Projector, alias, outerProjection.Select.Alias);
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, outerProjection.Select, exists),
                pc.Projector, outerProjection.Aggregator
                );
        }

        protected virtual Expression BindGroupJoin(MethodInfo groupJoinMethod, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            // A database will treat this no differently than a SelectMany w/ result selector, so just use that translation instead
            Type[] args = groupJoinMethod.GetGenericArguments();

            ClientProjectionExpression outerProjection = this.RewriteSequence(outerSource);

            _map[outerKey.Parameters[0]] = outerProjection.Projector;
            var predicateLambda = Expression.Lambda(innerKey.Body.Equal(outerKey.Body), innerKey.Parameters[0]);
            var callToWhere = Expression.Call(typeof(Enumerable), "Where", new Type[] { args[1] }, innerSource, predicateLambda);
            Expression group = this.Visit(callToWhere);

            _map[resultSelector.Parameters[0]] = outerProjection.Projector;
            _map[resultSelector.Parameters[1]] = group;
            Expression resultExpr = this.Visit(resultSelector.Body);

            var alias = this.GetNextAlias();
            ProjectedColumns pc = this.ProjectColumns(resultExpr, alias, outerProjection.Select.Alias);
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, outerProjection.Select, null),
                pc.Projector
                );
        }

        private List<OrderExpression>? _thenBys;

        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            var myThenBys = _thenBys;
            _thenBys = null;
            var projection = this.RewriteSequence(source);

            _map[orderSelector.Parameters[0]] = projection.Projector;
            var orderings = new List<OrderExpression>();
            orderings.Add(new OrderExpression(orderType, this.Visit(orderSelector.Body)));

            if (myThenBys != null)
            {
                for (int i = myThenBys.Count - 1; i >= 0; i--)
                {
                    var tb = myThenBys[i];
                    var lambda = (LambdaExpression)tb.Expression;
                    _map[lambda.Parameters[0]] = projection.Projector;
                    orderings.Add(new OrderExpression(tb.OrderType, this.Visit(lambda.Body)));
                }
            }

            var alias = this.GetNextAlias();
            var pc = this.ProjectColumns(projection.Projector, alias, projection.Select.Alias);
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, projection.Select, null, orderings.AsReadOnly(), null),
                pc.Projector
                );
        }

        protected virtual Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            if (_thenBys == null)
            {
                _thenBys = new List<OrderExpression>();
            }

            _thenBys.Add(new OrderExpression(orderType, orderSelector));

            return this.Visit(source);
        }

        protected virtual Expression BindGroupBy(Expression source, LambdaExpression keySelector, LambdaExpression? elementSelector, LambdaExpression? resultSelector)
        {
            var projection = this.RewriteSequence(source);

            _map[keySelector.Parameters[0]] = projection.Projector;
            var keyExpr = this.Visit(keySelector.Body);

            var elemExpr = projection.Projector;
            if (elementSelector != null)
            {
                _map[elementSelector.Parameters[0]] = projection.Projector;
                elemExpr = this.Visit(elementSelector.Body);
            }

            // Use ProjectColumns to get group-by expressions from key expression
            var keyProjection = this.ProjectColumns(keyExpr, projection.Select.Alias, projection.Select.Alias);
            var groupExprs = keyProjection.Columns.Select(c => c.Expression).ToArray();

            // make duplicate of source query as basis of element subquery by visiting the source again
            var duplicateSource = source;
            var subqueryBasis = this.RewriteSequence(duplicateSource);

            // recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate)
            _map[keySelector.Parameters[0]] = subqueryBasis.Projector;
            var subqueryKey = this.Visit(keySelector.Body);

            // use same projection trick to get group-by expressions based on subquery
            var subqueryKeyPC = this.ProjectColumns(subqueryKey, subqueryBasis.Select.Alias, subqueryBasis.Select.Alias);
            var subqueryGroupExprs = subqueryKeyPC.Columns.Select(c => c.Expression).ToArray();
            var subqueryCorrelation = this.BuildPredicateWithNullsEqual(subqueryGroupExprs, groupExprs);

            // compute element based on duplicated subquery
            var subqueryElemExpr = subqueryBasis.Projector;
            if (elementSelector != null)
            {
                _map[elementSelector.Parameters[0]] = subqueryBasis.Projector;
                subqueryElemExpr = this.Visit(elementSelector.Body);
            }

            // build subquery that projects the desired element
            var elementAlias = this.GetNextAlias();
            var elementPC = this.ProjectColumns(subqueryElemExpr, elementAlias, subqueryBasis.Select.Alias);
            var elementSubquery =
                new ClientProjectionExpression(
                    new SelectExpression(elementAlias, elementPC.Columns, subqueryBasis.Select, subqueryCorrelation),
                    elementPC.Projector
                    );

            var alias = this.GetNextAlias();

            // make it possible to tie aggregates back to this group-by
            var info = new GroupByInfo(alias, elemExpr);
            _groupByMap.Add(elementSubquery, info);

            Expression resultExpr;
            if (resultSelector != null)
            {
                var saveGroupElement = _currentGroupElement;
                _currentGroupElement = elementSubquery;
                // compute result expression based on key & element-subquery
                _map[resultSelector.Parameters[0]] = keyProjection.Projector;
                _map[resultSelector.Parameters[1]] = elementSubquery;
                resultExpr = this.Visit(resultSelector.Body);
                _currentGroupElement = saveGroupElement;
            }
            else
            {
                // no result selector was specified, so result must be IGrouping<K,E>
                var constructor = typeof(Grouping<,>)
                    .MakeGenericType(keyExpr.Type, subqueryElemExpr.Type)
                    .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .First();

                resultExpr = Expression.Convert(
                    Expression.New(constructor, new[] { keyExpr, elementSubquery }),
                    typeof(IGrouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type));
            }

            var pc = this.ProjectColumns(resultExpr, alias, projection.Select.Alias);
            var text = pc.Projector.ToDebugText();

            // make it possible to tie aggregates back to this group-by
            if (this.GetNewExpression(pc.Projector) is { } newResult
                && newResult.Type.IsGenericType 
                && newResult.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
            {
                var projectedElementSubquery = newResult.Arguments[1];
                _groupByMap[projectedElementSubquery] = info;
            }

            var final = new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, projection.Select, null, null, groupExprs),
                pc.Projector
                );

            return final;
        }

        private NewExpression? GetNewExpression(Expression expression)
        {
            // ignore converions 
            while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression as NewExpression;
        }

        private Expression BuildPredicateWithNullsEqual(IEnumerable<Expression> source1, IEnumerable<Expression> source2)
        {
            IEnumerator<Expression> en1 = source1.GetEnumerator();
            IEnumerator<Expression> en2 = source2.GetEnumerator();
            Expression? result = null;

            while (en1.MoveNext() && en2.MoveNext())
            {
                Expression compare =
                    Expression.Or(
                        new IsNullExpression(en1.Current).And(new IsNullExpression(en2.Current)),
                        en1.Current.Equal(en2.Current)
                        );
                result = (result == null) ? compare : result.And(compare);
            }

            return result!;
        }

        private Expression? _currentGroupElement;

        class GroupByInfo
        {
            internal TableAlias Alias { get; private set; }
            internal Expression Element { get; private set; }
            internal GroupByInfo(TableAlias alias, Expression element)
            {
                this.Alias = alias;
                this.Element = element;
            }
        }

        class AggregateSubqueryInfo
        {
            /// <summary>
            /// Alias of select that has the corresponding group-by
            /// </summary>
            public TableAlias Alias { get; }

            /// <summary>
            /// The aggregate as it should appear in the select with the group by
            /// </summary>
            public AggregateExpression Aggregate { get; }

            /// <summary>
            /// The id of the <see cref="TaggedExpression"/> that can be moved.
            /// </summary>
            public int SubqueryId { get; }

            public AggregateSubqueryInfo(
                TableAlias groupByAlias,
                AggregateExpression aggregate,
                int subqueryId)
            {
                this.Alias = groupByAlias;
                this.Aggregate = aggregate;
                this.SubqueryId = subqueryId;
            }
        }

        private ImmutableList<AggregateSubqueryInfo> _aggregateSubqueries =
            ImmutableList<AggregateSubqueryInfo>.Empty;

        private Expression BindAggregate(Expression source, string aggName, Type returnType, LambdaExpression? argument, bool isRoot)
        {
            var hasPredicateArg = _linguist.AggregateArgumentIsPredicate(aggName);
            var isDistinct = false;
            var argumentWasPredicate = false;
            var useAlternateArg = false;

            // check for distinct
            if (source is MethodCallExpression mcs
                && !hasPredicateArg
                && argument == null)
            {
                if (mcs.Method.Name == "Distinct" && mcs.Arguments.Count == 1 &&
                    (mcs.Method.DeclaringType == typeof(Queryable) || mcs.Method.DeclaringType == typeof(Enumerable))
                    && _linguist.AllowDistinctInAggregates)
                {
                    source = mcs.Arguments[0];
                    isDistinct = true;
                }
            }

            if (argument != null && hasPredicateArg)
            {
                // convert query.Count(predicate) into query.Where(predicate).Count()
                source = Expression.Call(typeof(Queryable), "Where", new[] { TypeHelper.GetSequenceElementType(source.Type) }, source, argument);
                argument = null;
                argumentWasPredicate = true;
            }

            var projection = this.RewriteSequence(source);

            Expression? argExpr = null;
            if (argument != null)
            {
                _map[argument.Parameters[0]] = projection.Projector;
                argExpr = this.Visit(argument.Body);
            }
            else if (!hasPredicateArg || useAlternateArg)
            {
                argExpr = projection.Projector;
            }

            var alias = this.GetNextAlias();
            var pc = this.ProjectColumns(projection.Projector, alias, projection.Select.Alias);
            var aggExpr = new AggregateExpression(returnType, aggName, argExpr, isDistinct);
            var colType = _linguist.Language.TypeSystem.GetQueryType(returnType);
            var colName = "_" + aggName.ToLower();
            var select = new SelectExpression(alias, new ColumnDeclaration[] { new ColumnDeclaration(colName, aggExpr, colType) }, projection.Select, null);

            if (isRoot)
            {
                var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(aggExpr.Type), "p");
                var gator = Expression.Lambda(Expression.Call(typeof(Enumerable), "Single", new Type[] { returnType }, p), p);
                return new ClientProjectionExpression(select, new ColumnExpression(returnType, _linguist.Language.TypeSystem.GetQueryType(returnType), alias, colName), gator);
            }

            var subquery = new ScalarSubqueryExpression(returnType, select);

            // if we can find the corresponding group-info we can build a special AggregateSubquery node that will enable us to 
            // optimize the aggregate expression later using AggregateRewriter
            if (!argumentWasPredicate && _groupByMap.TryGetValue(projection, out var groupInfo))
            {
                // use the element expression from the group-by info to rebind the argument so the resulting expression is one that 
                // would be legal to add to the columns in the select expression that has the corresponding group-by clause.
                if (argument != null)
                {
                    _map[argument.Parameters[0]] = groupInfo.Element;
                    argExpr = this.Visit(argument.Body);
                }
                else if (!hasPredicateArg || useAlternateArg)
                {
                    argExpr = groupInfo.Element;
                }

                aggExpr = new AggregateExpression(returnType, aggName, argExpr, isDistinct);

                // check for easy to optimize case.  If the projection that our aggregate is based on is really the 'group' argument from
                // the query.GroupBy(xxx, (key, group) => yyy) method then whatever expression we return here will automatically
                // become part of the select expression that has the group-by clause, so just return the simple aggregate expression.
                if (projection == _currentGroupElement)
                    return aggExpr;

                // otherwise, return the full scalar subquery
                // wrapped in TaggedExpression so we can find it and possibly move it to the appropriate select later.
                var aggSubquery = new TaggedExpression(subquery);

                _aggregateSubqueries = _aggregateSubqueries.Add(
                    new AggregateSubqueryInfo(groupInfo.Alias, aggExpr, aggSubquery.Id));

                return aggSubquery;
            }

            // if we can't find the corresponding group-by info, just return the scalar subquery
            return subquery;
        }

        private Expression BindDistinct(Expression source)
        {
            ClientProjectionExpression projection = this.RewriteSequence(source);
            SelectExpression select = projection.Select;
            var alias = this.GetNextAlias();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(_linguist, ProjectionAffinity.Server, projection.Projector, null, alias, projection.Select.Alias);
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, projection.Select)
                    .WithIsDistinct(true),
                pc.Projector
                );
        }

        private Expression BindTake(Expression source, Expression take)
        {
            ClientProjectionExpression projection = this.RewriteSequence(source);
            take = this.Visit(take);
            SelectExpression select = projection.Select;
            var alias = this.GetNextAlias();
            ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, projection.Select.Alias);
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, projection.Select)
                    .WithTake(take),
                pc.Projector
                );
        }

        private Expression BindSkip(Expression source, Expression skip)
        {
            ClientProjectionExpression projection = this.RewriteSequence(source);
            skip = this.Visit(skip);
            SelectExpression select = projection.Select;
            var alias = this.GetNextAlias();
            ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, projection.Select.Alias);
            return new ClientProjectionExpression(
                new SelectExpression(alias, pc.Columns, projection.Select)
                    .WithSkip(skip),
                pc.Projector
                );
        }

        private Expression BindCast(Expression source, Type targetElementType)
        {
            ClientProjectionExpression projection = this.RewriteSequence(source);
            Type elementType = this.GetTrueUnderlyingType(projection.Projector);
            if (!targetElementType.IsAssignableFrom(elementType))
            {
                throw new InvalidOperationException(string.Format("Cannot cast elements from type '{0}' to type '{1}'", elementType, targetElementType));
            }
            return projection;
        }

        private Type GetTrueUnderlyingType(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Convert)
            {
                expression = ((UnaryExpression)expression).Operand;
            }
            return expression.Type;
        }

        private Expression BindFirst(Expression source, LambdaExpression? predicate, string kind, bool isRoot)
        {
            var projection = this.RewriteSequence(source);
            Expression? where = null;

            if (predicate != null)
            {
                _map[predicate.Parameters[0]] = projection.Projector;
                where = this.Visit(predicate.Body);
            }

            var isFirst = kind.StartsWith("First");
            var isLast = kind.StartsWith("Last");
            var take = (isFirst || isLast) ? Expression.Constant(1) : null;

            if (take != null || where != null)
            {
                var alias = this.GetNextAlias();
                var pc = this.ProjectColumns(projection.Projector, alias, projection.Select.Alias);
                projection = new ClientProjectionExpression(
                    new SelectExpression(alias, pc.Columns, projection.Select, where, null, null, false, null, take, isLast),
                    pc.Projector
                    );
            }

            if (isRoot)
            {
                var elementType = projection.Projector.Type;
                var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "p");
                var gator = Expression.Lambda(Expression.Call(typeof(Enumerable), kind, new Type[] { elementType }, p), p);
                return new ClientProjectionExpression(projection.Select, projection.Projector, gator);
            }

            return projection;
        }

        private Expression BindAnyAll(Expression source, MethodInfo method, LambdaExpression? predicate, bool isRoot)
        {
            bool isAll = method.Name == "All";

            if (source is ConstantExpression constSource
                && !IsQuery(constSource)
                && predicate != null)
            {
                System.Diagnostics.Debug.Assert(!isRoot);
                Expression? where = null;

                foreach (object value in (IEnumerable)constSource.Value)
                {
                    var expr = Expression.Invoke(predicate, Expression.Constant(value, predicate.Parameters[0].Type));

                    if (where == null)
                    {
                        where = expr;
                    }
                    else if (isAll)
                    {
                        where = where.And(expr);
                    }
                    else
                    {
                        where = where.Or(expr);
                    }
                }

                return this.Visit(where!);
            }
            else
            {
                if (isAll && predicate != null)
                {
                    predicate = Expression.Lambda(Expression.Not(predicate.Body), predicate.Parameters.ToArray());
                }

                if (predicate != null)
                {
                    source = Expression.Call(typeof(Enumerable), "Where", method.GetGenericArguments(), source, predicate);
                }

                var projection = this.RewriteSequence(source);
                Expression? result = new ExistsSubqueryExpression(projection.Select);

                if (isAll)
                {
                    result = Expression.Not(result);
                }
                else if (isRoot)
                {
                    if (_linguist.AllowSubqueryInSelectWithoutFrom)
                    {
                        return GetSingletonSequence(result, "SingleOrDefault");
                    }
                    else
                    {
                        // use count aggregate instead of exists
                        var colType = _linguist.Language.TypeSystem.GetQueryType(typeof(int));
                        var newSelect = projection.Select.WithColumns(
                            new[] { new ColumnDeclaration("value", new AggregateExpression(typeof(int), "Count", null, false), colType) }
                            );
                        var colx = new ColumnExpression(typeof(int), colType, newSelect.Alias, "value");
                        var exp = isAll
                            ? colx.Equal(Expression.Constant(0))
                            : colx.GreaterThan(Expression.Constant(0));
                        return new ClientProjectionExpression(
                            newSelect, exp, Aggregator.GetAggregator(typeof(bool), typeof(IEnumerable<bool>))
                            );
                    }
                }

                return result;
            }
        }

        private Expression BindContains(Expression source, Expression match, bool isRoot)
        {
            if (source is ConstantExpression constSource
                && !IsQuery(constSource))
            {
                System.Diagnostics.Debug.Assert(!isRoot);
                List<Expression> values = new List<Expression>();

                foreach (object value in (IEnumerable)constSource.Value)
                {
                    values.Add(Expression.Constant(Convert.ChangeType(value, match.Type), match.Type));
                }

                match = this.Visit(match);

                return new InValuesExpression(match, values);
            }
            else if (isRoot && !_linguist.AllowSubqueryInSelectWithoutFrom)
            {
                var p = Expression.Parameter(TypeHelper.GetSequenceElementType(source.Type), "x");
                var predicate = Expression.Lambda(p.Equal(match), p);
                var exp = Expression.Call(typeof(Queryable), "Any", new Type[] { p.Type }, source, predicate);
                _root = exp;
                return this.Visit(exp);
            }
            else
            {
                ClientProjectionExpression projection = this.RewriteSequence(source);
                match = this.Visit(match);
                Expression result = new InSubqueryExpression(match, projection.Select);

                if (isRoot)
                {
                    return this.GetSingletonSequence(result, "SingleOrDefault");
                }

                return result;
            }
        }

        private Expression GetSingletonSequence(Expression expr, string aggregator)
        {
            var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(expr.Type), "p");
            LambdaExpression? gator = null;
            if (aggregator != null)
            {
                gator = Expression.Lambda(Expression.Call(typeof(Enumerable), aggregator, new Type[] { expr.Type }, p), p);
            }
            var alias = this.GetNextAlias();
            var colType = _linguist.Language.TypeSystem.GetQueryType(expr.Type);
            var select = new SelectExpression(alias, new[] { new ColumnDeclaration("value", expr, colType) }, null, null);
            return new ClientProjectionExpression(select, new ColumnExpression(expr.Type, colType, alias, "value"), gator);
        }

        private Expression BindInsert(IEntityTable upd, Expression instance, LambdaExpression? selector)
        {
            MappedEntity entity = _mapper.Mapping.GetEntity(instance.Type, upd.EntityId);
            return this.Visit(_mapper.GetInsertExpression(entity, instance, selector, _linguist, _police));
        }

        private Expression BindUpdate(IEntityTable upd, Expression instance, LambdaExpression? updateCheck, LambdaExpression? resultSelector)
        {
            MappedEntity entity = _mapper.Mapping.GetEntity(instance.Type, upd.EntityId);
            return this.Visit(_mapper.GetUpdateExpression(entity, instance, updateCheck, resultSelector, null, _linguist, _police));
        }

        private Expression BindInsertOrUpdate(IEntityTable upd, Expression instance, LambdaExpression? updateCheck, LambdaExpression? resultSelector)
        {
            MappedEntity entity = _mapper.Mapping.GetEntity(instance.Type, upd.EntityId);
            return this.Visit(_mapper.GetInsertOrUpdateExpression(entity, instance, updateCheck, resultSelector, _linguist, _police));
        }

        private Expression BindDelete(IEntityTable upd, Expression? instance, LambdaExpression? deleteCheck)
        {
            MappedEntity entity = _mapper.Mapping.GetEntity(upd);
            return this.Visit(_mapper.GetDeleteExpression(entity, instance, deleteCheck, _linguist, _police));
        }

        private Expression BindBatch(IEntityTable upd, Expression instances, LambdaExpression operation, Expression batchSize, Expression stream)
        {
            var save = _batchUpd;
            _batchUpd = upd;
            var op = (LambdaExpression)this.Visit(operation);
            _batchUpd = save;
            var items = this.Visit(instances);
            var size = this.Visit(batchSize);
            var str = this.Visit(stream);
            return new BatchExpression(items, op, size, str);
        }

        private bool IsQuery(Expression expression)
        {
            Type elementType = TypeHelper.GetSequenceElementType(expression.Type);
            return elementType != null && typeof(IQueryable<>).MakeGenericType(elementType).IsAssignableFrom(expression.Type);
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (this.IsQuery(c))
            {
                if (c.Value is IQueryable query)
                {
                    if (c.Value is IEntityTable table)
                    {
                        var entity = table is IHaveMappingEntity me
                            ? me.Entity 
                            : _mapper.Mapping.GetEntity(table.ElementType, table.EntityId);

                        return this.RewriteSequence(_mapper.GetQueryExpression(entity, _linguist, _police));
                    }
                    else if (query.Expression.NodeType == ExpressionType.Constant)
                    {
                        // assume this is also a table via some other implementation of IQueryable
                        var entity = _mapper.Mapping.GetEntity(query.ElementType);
                        return this.RewriteSequence(_mapper.GetQueryExpression(entity, _linguist, _police));
                    }
                    else
                    {
                        var pev = PartialEvaluator.Eval(query.Expression, _linguist.Language.CanBeEvaluatedLocally);
                        return this.Visit(pev);
                    }
                }
            }
            return c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            Expression e;
            if (_map.TryGetValue(p, out e))
            {
                return e;
            }
            return p;
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            if (iv.Expression is LambdaExpression lambda)
            {
                for (int i = 0, n = lambda.Parameters.Count; i < n; i++)
                {
                    _map[lambda.Parameters[i]] = iv.Arguments[i];
                }
                return this.Visit(lambda.Body);
            }
            return base.VisitInvocation(iv);
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression.NodeType == ExpressionType.Parameter
                && !_map.ContainsKey((ParameterExpression)m.Expression)
                && this.IsQuery(m))
            {
                return this.RewriteSequence(_mapper.GetQueryExpression(_mapper.Mapping.GetEntity(m.Member), _linguist, _police));
            }

            var source = this.Visit(m.Expression);

            if (_linguist.IsAggregate(m.Member) && IsRemoteQuery(source))
            {
                return this.BindAggregate(m.Expression, m.Member.Name, TypeHelper.GetMemberType(m.Member), null, m == _root);
            }

            return source.TryResolveMemberAccess(m.Member, out var resolvedAccess)
                ? resolvedAccess
                : m.Update(source, m.Member);
        }

        private bool IsRemoteQuery(Expression expression)
        {
            if (expression is SqlExpression)
                return true;

            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return IsRemoteQuery(((MemberExpression)expression).Expression);
                case ExpressionType.Call:
                    MethodCallExpression mc = (MethodCallExpression)expression;
                    if (mc.Object != null)
                        return IsRemoteQuery(mc.Object);
                    else if (mc.Arguments.Count > 0)
                        return IsRemoteQuery(mc.Arguments[0]);
                    break;
            }

            return false;
        }

        /// <summary>
        /// Moves aggregate subquery expressions into the same <see cref="SelectExpression"/> that has the group-by clause
        /// </summary>
        private class AggregateRewriter : SqlExpressionVisitor
        {
            private readonly QueryLanguage _language;
            private readonly ILookup<TableAlias, AggregateSubqueryInfo> _aliasToAggregateInfoMap;
            private ImmutableDictionary<int, ColumnExpression> _subqueryIdToColumnMap;

            public AggregateRewriter(
                QueryLanguage language,
                IReadOnlyList<AggregateSubqueryInfo> aggregateSubqueries)
            {
                _language = language;
                _subqueryIdToColumnMap = ImmutableDictionary<int, ColumnExpression>.Empty;
                _aliasToAggregateInfoMap = aggregateSubqueries.ToLookup(a => a.Alias);
            }

            protected internal override Expression VisitSelect(SelectExpression select)
            {
                select = (SelectExpression)base.VisitSelect(select);

                if (_aliasToAggregateInfoMap.Contains(select.Alias))
                {
                    var newColumns = select.Columns.ToList();

                    foreach (var info in _aliasToAggregateInfoMap[select.Alias])
                    {
                        var name = newColumns.GetAvailableColumnName("_" + info.Aggregate.AggregateName.ToLower());
                        var colType = _language.TypeSystem.GetQueryType(info.Aggregate.Type);
                        var cd = new ColumnDeclaration(name, info.Aggregate, colType);
                        _subqueryIdToColumnMap = _subqueryIdToColumnMap.SetItem(info.SubqueryId, new ColumnExpression(info.Aggregate.Type, colType, info.Alias, name));
                        newColumns.Add(cd);
                    }

                    return select.WithColumns(newColumns);
                }
                else if (_subqueryIdToColumnMap.Count > 0)
                {
                    // remap any remapped aggregates
                    // so the column bubbles up to where it needs to be.
                    var newColumns = select.Columns.ToList();

                    foreach (var kvp in _subqueryIdToColumnMap)
                    {
                        var col = kvp.Value;
                        if (IsFromAlias(select.From, col.Alias))
                        {
                            var name = newColumns.GetAvailableColumnName(col.Name);
                            var newCol = new ColumnDeclaration(name, col, col.QueryType);
                            _subqueryIdToColumnMap = _subqueryIdToColumnMap.SetItem(kvp.Key, new ColumnExpression(col.Type, col.QueryType, select.Alias, name));
                            newColumns.Add(newCol);
                        }
                    }

                    return select.WithColumns(newColumns);
                }

                return select;
            }

            private static bool IsFromAlias(Expression? from, TableAlias alias) =>
                from is AliasedExpression aliased ? aliased.Alias == alias
                : from is JoinExpression join ? IsFromAlias(join.Left, alias) || IsFromAlias(join.Right, alias)
                : false;

            protected internal override Expression VisitTagged(TaggedExpression original)
            {
                // its this a scalar subquery that should be mapped?
                if (_subqueryIdToColumnMap.TryGetValue(original.Id, out var column))
                {
                    return column;
                }

                return base.VisitTagged(original);
            }

            protected internal override Expression VisitClientProjection(ClientProjectionExpression original)
            {
                var oldMap = _subqueryIdToColumnMap;
                _subqueryIdToColumnMap = ImmutableDictionary<int, ColumnExpression>.Empty;
                var result = base.VisitClientProjection(original);
                _subqueryIdToColumnMap = oldMap;
                return result;
            }
        }
    }
}
