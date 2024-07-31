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

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;

    /// <summary>
    /// Attempts to rewrite cross-apply and outer-apply joins as inner and left-outer joins
    /// </summary>
    public class CrossApplyToLeftJoinRewriter : SqlExpressionVisitor
    {
        private readonly QueryLanguage _language;

        public CrossApplyToLeftJoinRewriter(QueryLanguage language)
        {
            _language = language;
        }

        protected internal override Expression VisitJoin(JoinExpression join)
        {
            join = (JoinExpression)base.VisitJoin(join);

            if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
            {
                if (join.Right is TableExpression)
                {
                    return new JoinExpression(JoinType.CrossJoin, join.Left, join.Right, null);
                }
                else
                {
                    var select = join.Right as SelectExpression;
                    // Only consider rewriting cross apply if 
                    //   1) right side is a select
                    //   2) other than in the where clause in the right-side select, no left-side declared aliases are referenced
                    //   3) and has no behavior that would change semantics if the where clause is removed (like groups, aggregates, take, skip, etc).
                    // Note: it is best to attempt this after redundant subqueries have been removed.
                    if (select != null
                        && select.Take == null
                        && select.Skip == null
                        && !AggregateChecker.HasAggregates(select)
                        && (select.GroupBy == null || select.GroupBy.Count == 0))
                    {
                        var selectWithoutWhere = select.WithWhere(null);
                        var referencedAliases = ReferencedAliasGatherer.Gather(selectWithoutWhere);
                        var declaredAliases = DeclaredAliasGatherer.Gather(join.Left);
                        referencedAliases.IntersectWith(declaredAliases);
                        if (referencedAliases.Count == 0)
                        {
                            var where = select.Where;
                            select = selectWithoutWhere;
                            if (where != null)
                            {
                                var pc = ColumnProjector.ProjectColumns(_language, where, select.Columns, select.Alias, DeclaredAliasGatherer.Gather(select.From));
                                select = select.WithColumns(pc.Columns);
                                where = pc.Projector;
                            }

                            JoinType jt = (where == null) ? JoinType.CrossJoin : (join.JoinType == JoinType.CrossApply ? JoinType.InnerJoin : JoinType.LeftOuterJoin);
                            return new JoinExpression(jt, join.Left, select, where);
                        }
                    }
                }
            }

            return join;
        }

        private bool CanBeColumn(Expression expr)
        {
            return expr is ColumnExpression;
        }
    }
}