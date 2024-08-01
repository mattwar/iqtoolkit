// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Expressions.Sql;

    /// <summary>
    /// Adds relationship to query results depending on policy
    /// </summary>
    public class RelationshipIncluder : SqlExpressionVisitor
    {
        private readonly QueryLinguist _linguist;
        private readonly QueryMapper _mapper;
        private readonly QueryPolice _police;
        private ImmutableDictionary<MemberInfo, bool> _includeScope;

        public RelationshipIncluder(QueryLinguist linguist, QueryMapper mapper, QueryPolice police)
        {
            _linguist = linguist;
            _mapper = mapper;
            _police = police;
            _includeScope = ImmutableDictionary<MemberInfo, bool>.Empty;
        }

        public static Expression Include(
            Expression expression, 
            QueryLinguist linguist,
            QueryMapper mapper,
            QueryPolice police)
        {
            return new RelationshipIncluder(linguist, mapper, police).Visit(expression);
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
                if (_mapper.HasIncludedMembers(entity, _police.Policy))
                {
                    entity = _mapper.IncludeMembers(
                        entity,
                        m =>
                        {
                            if (_includeScope.ContainsKey(m))
                            {
                                return false;
                            }
                            
                            if (_police.Policy.IsIncluded(m))
                            {
                                _includeScope = _includeScope.SetItem(m, true);
                                return true;
                            }

                            return false;
                        },
                        _linguist,
                        _police
                        );
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
