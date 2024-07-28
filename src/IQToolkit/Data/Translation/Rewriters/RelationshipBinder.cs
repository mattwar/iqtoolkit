// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    /// <summary>
    /// Translates accesses to relationship members into projections or joins
    /// </summary>
    public class RelationshipBinder : DbExpressionRewriter
    {
        private readonly QueryMappingRewriter _mapper;
        private readonly QueryMapping _mapping;
        private readonly QueryLanguage _language;
        private Expression? _currentFrom;

        public RelationshipBinder(QueryMappingRewriter mappingRewriter)
        {
            _mapper = mappingRewriter;
            _mapping = mappingRewriter.Mapping;
            _language = mappingRewriter.Translator.LanguageRewriter.Language;
        }

        protected override Expression RewriteSelect(SelectExpression select)
        {
            // look for association references in SelectExpression clauses
            var saveCurrentFrom = _currentFrom;
            _currentFrom = this.RewriteN(select.From);

            try
            {
                var where = this.RewriteN(select.Where);
                var orderBy = this.VisitOrderExpressions(select.OrderBy);
                var groupBy = this.RewriteExpressionList(select.GroupBy);
                var skip = this.RewriteN(select.Skip);
                var take = this.RewriteN(select.Take);
                var columns = this.RewriteColumnDeclarations(select.Columns);

                return select.Update(select.Alias, _currentFrom, where, orderBy, groupBy, skip, take, select.IsDistinct, select.IsReverse, columns);
            }
            finally
            {
                _currentFrom = saveCurrentFrom;
            }
        }

        protected override Expression RewriteClientProjection(ClientProjectionExpression proj)
        {
            var select = (SelectExpression)this.Rewrite(proj.Select);

            // look for association references in projector
            var saveCurrentFrom = _currentFrom;
            _currentFrom = select;

            try
            {
                var projector = this.Rewrite(proj.Projector);

                if (_currentFrom != select)
                {
                    // remap projector onto new select that includes new from
                    var alias = new TableAlias();
                    var existingAliases = GetAliases(_currentFrom);
                    ProjectedColumns pc = ColumnProjector.ProjectColumns(_language, projector, null, alias, existingAliases);
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

        protected override Expression RewriteMemberAccess(MemberExpression memberAccess)
        {
            var source = this.Rewrite(memberAccess.Expression);

            if (FindEntityExpression(memberAccess.Expression) is { } entity 
                && _mapping.IsRelationship(entity.Entity, memberAccess.Member))
            {
                var projection = (ClientProjectionExpression)this.Rewrite(
                    _mapper.GetMemberExpression(source, entity.Entity, memberAccess.Member)
                    );

                if (_currentFrom != null && _mapping.IsSingletonRelationship(entity.Entity, memberAccess.Member))
                {
                    // convert singleton associations directly to OUTER APPLY
                    // by adding join to relavent FROM clause
                    // and placing an OuterJoinedExpression in the projection to remember the outer-join test-for-null condition
                    projection = _language.AddOuterJoinTest(projection);
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
                    return this.Rewrite(resolvedAccess);
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
