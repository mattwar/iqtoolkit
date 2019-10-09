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

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Translates accesses to relationship members into projections or joins
    /// </summary>
    public class RelationshipBinder : DbExpressionVisitor
    {
        private readonly QueryMapper mapper;
        private readonly QueryMapping mapping;
        private readonly QueryLanguage language;
        Expression currentFrom;

        private RelationshipBinder(QueryMapper mapper)
        {
            this.mapper = mapper;
            this.mapping = mapper.Mapping;
            this.language = mapper.Translator.Linguist.Language;
        }

        public static Expression Bind(QueryMapper mapper, Expression expression)
        {
            return new RelationshipBinder(mapper).Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            // look for association references in SelectExpression clauses
            Expression saveCurrentFrom = this.currentFrom;
            this.currentFrom = this.VisitSource(select.From);

            try
            {
                var where = this.Visit(select.Where);
                var orderBy = this.VisitOrderBy(select.OrderBy);
                var groupBy = this.VisitExpressionList(select.GroupBy);
                var skip = this.Visit(select.Skip);
                var take = this.Visit(select.Take);
                var columns = this.VisitColumnDeclarations(select.Columns);

                return UpdateSelect(select, this.currentFrom, where, orderBy, groupBy, skip, take, select.IsDistinct, select.IsReverse, columns);
            }
            finally
            {
                this.currentFrom = saveCurrentFrom;
            }
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            var select = (SelectExpression)this.Visit(proj.Select);

            // look for association references in projector
            Expression saveCurrentFrom = this.currentFrom;
            this.currentFrom = select;

            try
            {
                var projector = this.Visit(proj.Projector);

                if (this.currentFrom != select)
                {
                    // remap projector onto new select that includes new from
                    var alias = new TableAlias();
                    var existingAliases = GetAliases(this.currentFrom);
                    ProjectedColumns pc = ColumnProjector.ProjectColumns(this.language, projector, null, alias, existingAliases);
                    projector = pc.Projector;
                    select = new SelectExpression(alias, pc.Columns, this.currentFrom, null);
                }

                return UpdateProjection(proj, select, projector, proj.Aggregator);
            }
            finally
            {
                this.currentFrom = saveCurrentFrom;
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

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            Expression source = this.Visit(m.Expression);
            EntityExpression ex = GetEntityExpression(source);

            if (ex != null && this.mapping.IsRelationship(ex.Entity, m.Member))
            {
                ProjectionExpression projection = (ProjectionExpression)this.Visit(this.mapper.GetMemberExpression(source, ex.Entity, m.Member));

                if (this.currentFrom != null && this.mapping.IsSingletonRelationship(ex.Entity, m.Member))
                {
                    // convert singleton associations directly to OUTER APPLY
                    // by adding join to relavent FROM clause
                    // and placing an OuterJoinedExpression in the projection to remember the outer-join test-for-null condition
                    projection = this.language.AddOuterJoinTest(projection);
                    Expression newFrom = new JoinExpression(JoinType.OuterApply, this.currentFrom, projection.Select, null);
                    this.currentFrom = newFrom;
                    return projection.Projector;
                }

                return projection;
            }
            else
            {
                Expression result = QueryBinder.BindMember(source, m.Member);
                MemberExpression mex = result as MemberExpression;

                if (mex != null && mex.Member == m.Member && mex.Expression == m.Expression)
                {
                    return m;
                }

                return result;
            }
        }

        private static EntityExpression GetEntityExpression(Expression exp)
        {
            // see through the outer-joined-expression to find the entity expression
            if (exp is OuterJoinedExpression oj)
                exp = oj.Expression;

            return exp as EntityExpression;
        }
    }
}
