// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using SqlExpressions;

    /// <summary>
    /// rewrites nested client projections into client-side joins
    /// </summary>
    public class ClientProjectionToClientJoinRewriter : DbExpressionVisitor
    {
        private readonly QueryPolicy _policy;
        private readonly QueryLanguage _language;
        private bool _isTopLevel = true;
        private SelectExpression? _currentSelect;
        private MemberInfo? _currentMember;
        private bool _canJoinOnClient = true;

        public ClientProjectionToClientJoinRewriter(QueryPolicy policy, QueryLanguage language)
        {
            _policy = policy;
            _language = language;
        }

        private readonly Dictionary<Expression, MemberInfo> _matchingMembers =
            new Dictionary<Expression, MemberInfo>();

        public override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null!;

            if (_matchingMembers.TryGetValue(exp, out var mm))
            {
                var prevMM = _currentMember;
                _currentMember = mm;
                var result = base.Visit(exp);
                _currentMember = prevMM;
                return result;
            }
            else
            {
                return base.Visit(exp);
            }
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            _matchingMembers[assignment.Expression] = assignment.Member;
            return base.VisitMemberAssignment(assignment);
        }

        protected override Expression VisitNew(NewExpression original)
        {
            if (original.Members?.Count > 0 
                && original.Members.Count == original.Arguments.Count)
            {
                for (int i = 0; i < original.Arguments.Count; i++)
                {
                    _matchingMembers[original.Arguments[i]] = original.Members[i];
                }
            }

            return base.VisitNew(original);
        }

        protected internal override Expression VisitClientProjection(ClientProjectionExpression proj)
        {
            var previousSelect = _currentSelect;
            _currentSelect = proj.Select;
            try
            {
                if (!_isTopLevel)
                {
                    if (this.CanJoinOnClient(_currentSelect) && previousSelect != null)
                    {
                        // make a query that combines all the constraints from the outer queries into a single select
                        var newOuterSelect = previousSelect.Duplicate();

                        // remap any references to the outer select to the new alias;
                        var newInnerSelect = proj.Select.RemapTableAliases(newOuterSelect.Alias, previousSelect.Alias);
                        
                        // add outer-join test
                        var newInnerProjection = _language.AddOuterJoinTest(new ClientProjectionExpression(newInnerSelect, proj.Projector));
                        newInnerSelect = newInnerProjection.Select;

                        var newProjector = newInnerProjection.Projector;

                        var newAlias = new TableAlias();
                        var pc = ColumnProjector.ProjectColumns(_language, newProjector, null, newAlias, newOuterSelect.Alias, newInnerSelect.Alias);

                        var join = new JoinExpression(JoinType.OuterApply, newOuterSelect, newInnerSelect, null);
                        var joinedSelect = new SelectExpression(newAlias, pc.Columns, join, null, null, null, proj.IsSingleton, null, null, false);

                        // apply client-join treatment recursively
                        _currentSelect = joinedSelect;
                        newProjector = this.Visit(pc.Projector);

                        // compute keys (this only works if join condition was a single column comparison)
                        var outerKeys = new List<Expression>();
                        var innerKeys = new List<Expression>();
                        if (newInnerSelect.Where != null 
                            && this.GetEquiJoinKeyExpressions(newInnerSelect.Where, newOuterSelect.Alias, outerKeys, innerKeys))
                        {
                            // outerKey needs to refer to the outer-scope's alias
                            var outerKey = outerKeys.Select(k => TableAliasRemapper.Map(k, previousSelect.Alias, newOuterSelect.Alias));
                            
                            // innerKey needs to refer to the new alias for the select with the new join
                            var innerKey = innerKeys.Select(k => TableAliasRemapper.Map(k, joinedSelect.Alias, ((ColumnExpression)k).Alias));
                            var newProjection = new ClientProjectionExpression(joinedSelect, newProjector, proj.Aggregator);

                            return new ClientJoinExpression(newProjection, outerKey, innerKey);
                        }
                    }
                    else
                    {
                        bool previousJoin = _canJoinOnClient;
                        _canJoinOnClient = false;
                        var result = base.VisitClientProjection(proj);
                        _canJoinOnClient = previousJoin;
                        return result;
                    }
                }
                else
                {
                    _isTopLevel = false;
                }

                return base.VisitClientProjection(proj);
            }
            finally 
            {
                _currentSelect = previousSelect;
            }
        }

        private bool CanJoinOnClient(SelectExpression select)
        {
            // can add singleton (1:0,1) join if no grouping/aggregates or distinct
            return 
                _canJoinOnClient 
                && _currentMember != null 
                && !_policy.IsDeferLoaded(_currentMember)
                && !select.IsDistinct
                && (select.GroupBy.Count == 0)
                && !AggregateChecker.HasAggregates(select);
        }

        private bool GetEquiJoinKeyExpressions(
            Expression predicate, 
            TableAlias outerAlias, List<Expression> outerExpressions, List<Expression> innerExpressions)
        {
            if (predicate.NodeType == ExpressionType.Equal)
            {
                var b = (BinaryExpression)predicate;
                var leftCol = this.GetColumnExpression(b.Left);
                var rightCol = this.GetColumnExpression(b.Right);
                if (leftCol != null && rightCol != null)
                {
                    if (leftCol.Alias == outerAlias)
                    {
                        outerExpressions.Add(b.Left);
                        innerExpressions.Add(b.Right);
                        return true;
                    }
                    else if (rightCol.Alias == outerAlias)
                    {
                        innerExpressions.Add(b.Left);
                        outerExpressions.Add(b.Right);
                        return true;
                    }
                }
            }

            bool hadKey = false;
            var parts = predicate.Split(ExpressionType.And, ExpressionType.AndAlso);
            if (parts.Length > 1)
            {
                foreach (var part in parts)
                {
                    bool hasOuterAliasReference = ReferencedAliasGatherer.Gather(part).Contains(outerAlias);
                    if (hasOuterAliasReference)
                    {
                        if (!GetEquiJoinKeyExpressions(part, outerAlias, outerExpressions, innerExpressions))
                            return false;
                        hadKey = true;
                    }
                }
            }

            return hadKey;
        }

        private ColumnExpression? GetColumnExpression(Expression expression)
        {
            // ignore converions 
            while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression as ColumnExpression;
        }

        protected internal override Expression VisitScalarSubquery(ScalarSubqueryExpression scalar)
        {
            return scalar;
        }

        protected internal override Expression VisitExistsSubquery(ExistsSubqueryExpression exists)
        {
            return exists;
        }

        protected internal override Expression VisitInSubquery(InSubqueryExpression @in)
        {
            return @in;
        }

        protected internal override Expression VisitInsertCommand(InsertCommand insert)
        {
            _isTopLevel = true;
            return base.VisitInsertCommand(insert);
        }

        protected internal override Expression VisitUpdateCommand(UpdateCommand update)
        {
            _isTopLevel = true;
            return base.VisitUpdateCommand(update);
        }

        protected internal override Expression VisitDeleteCommand(DeleteCommand delete)
        {
            _isTopLevel = true;
            return base.VisitDeleteCommand(delete);
        }

        protected internal override Expression VisitIfCommand(IfCommand ifx)
        {
            _isTopLevel = true;
            return base.VisitIfCommand(ifx);
        }

        protected internal override Expression VisitDeclarationCommand(DeclarationCommand decl)
        {
            _isTopLevel = true;
            return base.VisitDeclarationCommand(decl);
        }

        protected internal override Expression VisitBlockCommand(BlockCommand block)
        {
            _isTopLevel = true;
            return base.VisitBlockCommand(block);
        }
    }
}