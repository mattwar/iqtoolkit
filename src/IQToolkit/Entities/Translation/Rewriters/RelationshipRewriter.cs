// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;
    using Utils;

    /// <summary>
    /// Translates accesses to relationship members into projections or joins
    /// </summary>
    public class RelationshipRewriter : SqlExpressionVisitor
    {
        private readonly QueryLinguist _linguist;
        private readonly QueryMapper _mapper;
        private readonly QueryPolice _police;
        private readonly EntityMapping _mapping;
        private Expression? _currentFrom;

        public RelationshipRewriter(
            QueryLinguist linguist,
            QueryMapper mapper,
            QueryPolice police)
        {
            _linguist = linguist;
            _mapper = mapper;
            _police = police;
            _mapping = mapper.Mapping;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            // look for association references in SelectExpression clauses
            var saveCurrentFrom = _currentFrom;
            _currentFrom = this.Visit(select.From!);

            try
            {
                var where = this.Visit(select.Where);
                var orderBy = select.OrderBy.Rewrite(o => o.Accept(this));
                var groupBy = select.GroupBy.Rewrite(this);
                var skip = this.Visit(select.Skip);
                var take = this.Visit(select.Take);
                var columns = select.Columns.Rewrite(d => d.Accept(this));

                return select.Update(select.Alias, _currentFrom, where, orderBy, groupBy, skip, take, select.IsDistinct, select.IsReverse, columns);
            }
            finally
            {
                _currentFrom = saveCurrentFrom;
            }
        }

        protected internal override Expression VisitClientProjection(ClientProjectionExpression proj)
        {
            var select = (SelectExpression)this.Visit(proj.Select);

            // look for association references in projector
            var saveCurrentFrom = _currentFrom;
            _currentFrom = select;

            try
            {
                var projector = this.Visit(proj.Projector);

                if (_currentFrom != select)
                {
                    // remap projector onto new select that includes new from
                    var alias = new TableAlias();
                    var existingAliases = GetAliases(_currentFrom);
                    var pc = ColumnProjector.ProjectColumns(_linguist, projector, null, alias, existingAliases);
                    projector = pc.Projector;
                    select = new SelectExpression(alias, pc.Columns, _currentFrom, null);
                }

                return proj.Update(select, projector, proj.Aggregator);
            }
            finally
            {
                _currentFrom = saveCurrentFrom;
            }
        }

        private static List<TableAlias> GetAliases(Expression expr)
        {
            var aliases = new List<TableAlias>();
            GetAliases(expr);
            return aliases;

            void GetAliases(Expression e)
            {
                switch (e)
                {
                    case JoinExpression j:
                        GetAliases(j.Left);
                        GetAliases(j.Right);
                        break;
                    case AliasedExpression a:
                        aliases.Add(a.Alias);
                        break;
                }
            }
        }

        protected override Expression VisitMember(MemberExpression memberAccess)
        {
            var source = this.Visit(memberAccess.Expression);

            if (FindEntityExpression(memberAccess.Expression) is { } entity 
                && _mapping.IsRelationship(entity.Entity, memberAccess.Member))
            {
                var projection = (ClientProjectionExpression)this.Visit(
                    _mapper.GetMemberExpression(source, entity.Entity, memberAccess.Member, _linguist, _police)
                    );

                if (_currentFrom != null && _mapping.IsSingletonRelationship(entity.Entity, memberAccess.Member))
                {
                    // convert singleton associations directly to OUTER APPLY
                    // by adding join to relavent FROM clause
                    // and placing an OuterJoinedExpression in the projection to remember the outer-join test-for-null condition
                    projection = _linguist.AddOuterJoinTest(projection);
                    var newFrom = new JoinExpression(JoinType.OuterApply, _currentFrom, projection.Select, null);
                    _currentFrom = newFrom;
                    return projection.Projector;
                }

                return projection;
            }
            else
            {
                if (!source.TryResolveMemberAccess(memberAccess.Member, out var resolvedAccess))
                {
                    return memberAccess;
                }

                if (resolvedAccess is ClientProjectionExpression)
                {
                    // rewrite nested projections too
                    return this.Visit(resolvedAccess);
                }

                return resolvedAccess;
            }
        }

        private static EntityExpression? FindEntityExpression(Expression exp)
        {
            // see through the outer-joined-expression to find the entity expression
            if (exp is OuterJoinedExpression oj)
                exp = oj.Expression;

            return exp as EntityExpression;
        }
    }
}
