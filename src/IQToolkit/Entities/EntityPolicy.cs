// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities
{
    using Expressions;
    using Utils;

    /// <summary>
    /// A <see cref="QueryPolicy"/> for database entities.
    /// </summary>
    public class EntityPolicy : QueryPolicy
    {
        private readonly ImmutableDictionary<MemberInfo, IncludeFacts> _included;
        private readonly ImmutableDictionary<MemberInfo, ImmutableList<LambdaExpression>> _operations;

        private EntityPolicy(
            ImmutableDictionary<MemberInfo, IncludeFacts> included,
            ImmutableDictionary<MemberInfo, ImmutableList<LambdaExpression>> operations)
        {
            _included = included;
            _operations = operations;
        }

        private class IncludeFacts
        {
            public bool IsDeferred { get; }

            public IncludeFacts(bool isDeferred)
            {
                this.IsDeferred = isDeferred;
            }
        }

        /// <summary>
        /// The default <see cref="EntityPolicy"/>.
        /// </summary>
        public static readonly EntityPolicy Default =
            new EntityPolicy(
                ImmutableDictionary<MemberInfo, IncludeFacts>.Empty,
                ImmutableDictionary<MemberInfo, ImmutableList<LambdaExpression>>.Empty
                );

        /// <summary>
        /// Apply the transform function to any query (or sub query) with the element type of the function's parameter.
        /// </summary>
        /// <param name="fnFilter">A lambda expression with one parameter that is a sequence of elements and
        /// returning a sequence of the same element type.</param>
        public EntityPolicy Apply(LambdaExpression fnFilter)
        {
            if (fnFilter == null)
                throw new ArgumentNullException("fnApply");
            if (fnFilter.Parameters.Count != 1)
                throw new ArgumentException("Apply function has wrong number of arguments.");

            return this.AddOperation(
                TypeHelper.GetSequenceElementType(fnFilter.Parameters[0].Type), 
                fnFilter);
        }

        /// <summary>
        /// Apply the transform function to any query (or sub query) with element type <see cref="T:TEntity"/>.
        /// </summary>
        public EntityPolicy Apply<TEntity>(
            Expression<Func<IEnumerable<TEntity>, IEnumerable<TEntity>>> fnFilter)
        {
            return Apply((LambdaExpression)fnFilter);
        }

        /// <summary>
        /// Include the association member's elements in any query that produces the containing entity type.
        /// </summary>
        public EntityPolicy Include(MemberInfo member)
        {
            return Include(member, false);
        }

        /// <summary>
        /// Include the association member's elements in any query that produces the containing entity type.
        /// </summary>
        /// <param name="member">The member whose elements will be included.</param>
        /// <param name="deferLoad">If true, the member's elements will be defer loaded if possible.</param>
        public EntityPolicy Include(MemberInfo member, bool deferLoad)
        {
            return new EntityPolicy(
                _included.SetItem(member, new IncludeFacts(deferLoad)),
                _operations
                );
        }

        /// <summary>
        /// Include the association member's elements in the output any query that procudes the containing entity type.
        /// Specified as a lambda expression of an element of the containing type referencing the member.
        /// </summary>
        /// <param name="fnMember">A lambda expression that takes a single parameter of the entity type and a 
        /// body that references an association member of the parameter.</param>
        public EntityPolicy IncludeWith(LambdaExpression fnMember)
        {
            return IncludeWith(fnMember, false);
        }

        /// <summary>
        /// Include the association member's elements in the output of any query that procudes the containing entity type.
        /// Specified as a lambda expression of an element of the containing type referencing the member.
        /// </summary>
        /// <param name="fnMember">A lambda expression that takes a single parameter of the entity type and a 
        /// body that references an association member of the parameter.</param>
        /// <param name="deferLoad">If true the member's elements will be defer loaded if possible.</param>
        public EntityPolicy IncludeWith(LambdaExpression fnMember, bool deferLoad)
        {
            var rootMember = fnMember.Body.FindFirstDownOrDefault<MemberExpression>(
                mx => mx.Expression == fnMember.Parameters[0]
                );

            if (rootMember == null)
                throw new InvalidOperationException("Subquery does not originate with a member access");

            var included = Include(rootMember.Member, deferLoad);

            if (rootMember != fnMember.Body)
            {
                included = included.AssociateWith(fnMember);
            }

            return included;
        }

        /// <summary>
        /// Include the association member's elements in the output of any query that procudes the containing entity type.
        /// Specified as a lambda expression of an element of the containing type referencing the member.
        /// </summary>
        /// <param name="fnMember">A lambda expression that takes a single parameter of the entity type and a 
        /// body that references an association member of the parameter.</param>
        public EntityPolicy IncludeWith<TEntity>(Expression<Func<TEntity, object>> fnMember)
        {
            return IncludeWith((LambdaExpression)fnMember, false);
        }

        /// <summary>
        /// Include the association member's elements in the output of any query that procudes the containing entity type.
        /// Specified as a lambda expression of an element of the containing type referencing the member.
        /// </summary>
        /// <param name="fnMember">A lambda expression that takes a single parameter of the entity type and a 
        /// body that references an association member of the parameter.</param>
        /// <param name="deferLoad">If true the member's elements will be defer loaded if possible.</param>
        public EntityPolicy IncludeWith<TEntity>(Expression<Func<TEntity, object>> fnMember, bool deferLoad)
        {
            return IncludeWith((LambdaExpression)fnMember, deferLoad);
        }

        /// <summary>
        /// Add a constraint or filter to an association member that is always applied whenever the member is
        /// referenced in a query, by specifing an operation on that member in a lambda expression.
        /// </summary>
        public EntityPolicy AssociateWith(LambdaExpression memberQuery)
        {
            var rootMember = memberQuery.Body.FindFirstDownOrDefault<MemberExpression>(
                mx => mx.Expression == memberQuery.Parameters[0]
                );

            if (rootMember == null)
                throw new InvalidOperationException("Subquery does not originate with a member access");

            if (rootMember != memberQuery.Body)
            {
                var memberParam = Expression.Parameter(rootMember.Type, "root");
                var newBody = memberQuery.Body.Replace(rootMember, memberParam);
                return this.AddOperation(rootMember.Member, Expression.Lambda(newBody, memberParam));
            }

            return this;
        }

        /// <summary>
        /// Add a constraint or filter to an association member that is always applied whenever the member is
        /// referenced in a query, by specifing an operation on that member in a lambda expression.
        /// </summary>
        public EntityPolicy AssociateWith<TEntity>(Expression<Func<TEntity, IEnumerable>> memberQuery)
        {
            return AssociateWith((LambdaExpression)memberQuery);
        }

        /// <summary>
        /// Adds an operation for the member.
        /// </summary>
        private EntityPolicy AddOperation(MemberInfo member, LambdaExpression operation)
        {
            if (!_operations.TryGetValue(member, out var memberOps))
            {
               memberOps = ImmutableList<LambdaExpression>.Empty;
            }

            var newOps = memberOps.Add(operation);

            return new EntityPolicy(
                _included,
                _operations.SetItem(member, newOps)
                );
        }

        /// <summary>
        /// Gets the operations associated with the member.
        /// </summary>
        public IReadOnlyList<LambdaExpression> GetOperations(MemberInfo member)
        {
            if (_operations.TryGetValue(member, out var list))
            {
                return list.ToReadOnly();
            }
            else
            {
                return ReadOnlyList<LambdaExpression>.Empty;
            }
        }

        /// <summary>
        /// True if the member's elements are included in the output of the query. 
        /// </summary>
        public override bool IsIncluded(MemberInfo member)
        {
            return _included.ContainsKey(member);
        }

        /// <summary>
        /// True if the member's elements are defer loaded.
        /// </summary>
        public override bool IsDeferLoaded(MemberInfo member)
        {
            return _included.TryGetValue(member, out var info)
                && info.IsDeferred;
        }
    }
}