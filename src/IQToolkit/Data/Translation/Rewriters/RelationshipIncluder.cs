// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    /// <summary>
    /// Adds relationship to query results depending on policy
    /// </summary>
    public class RelationshipIncluder : DbExpressionVisitor
    {
        private readonly QueryMappingRewriter _mapper;
        private readonly QueryPolicy _policy;
        private ImmutableDictionary<MemberInfo, bool> _includeScope;

        public RelationshipIncluder(QueryPolicy policy, QueryMappingRewriter mappingRewriter)
        {
            _mapper = mappingRewriter;
            _policy = policy;
            _includeScope = ImmutableDictionary<MemberInfo, bool>.Empty;
        }

        public static Expression Include(Expression expression, QueryPolicy policy, QueryMappingRewriter mappingRewriter)
        {
            return new RelationshipIncluder(policy, mappingRewriter).Visit(expression);
        }

        protected internal override Expression VisitClientProjection(ClientProjectionExpression proj)
        {
            var projector = this.Visit(proj.Projector);
            return proj.Update(proj.Select, projector, proj.Aggregator);
        }

        protected internal override Expression VisitEntity(EntityExpression entity)
        {
            var oldScope = _includeScope;
            try
            {
                if (_mapper.HasIncludedMembers(entity))
                {
                    entity = _mapper.IncludeMembers(
                        entity,
                        m =>
                        {
                            if (_includeScope.ContainsKey(m))
                            {
                                return false;
                            }
                            if (_policy.IsIncluded(m))
                            {
                                _includeScope = _includeScope.SetItem(m, true);
                                return true;
                            }

                            return false;
                        });
                }

                return base.VisitEntity(entity);
            }
            finally
            {
                _includeScope = oldScope;
            }
        }
    }
}
