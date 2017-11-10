// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data
{
    using Common;

    /// <summary>
    /// A <see cref="QueryPolicy"/> for database entities.
    /// </summary>
    public class EntityPolicy : QueryPolicy
    {
        private readonly HashSet<MemberInfo> included = new HashSet<MemberInfo>();
        private readonly HashSet<MemberInfo> deferred = new HashSet<MemberInfo>();
        private readonly Dictionary<MemberInfo, List<LambdaExpression>> operations = new Dictionary<MemberInfo, List<LambdaExpression>>();

        /// <summary>
        /// Apply the transform function to any query (or sub query) with the element type of the function's parameter.
        /// </summary>
        /// <param name="fnApply">A lambda expression with one parameter that is a sequence of elements and
        /// returning a sequence of the same element type.</param>
        public void Apply(LambdaExpression fnApply)
        {
            if (fnApply == null)
                throw new ArgumentNullException("fnApply");
            if (fnApply.Parameters.Count != 1)
                throw new ArgumentException("Apply function has wrong number of arguments.");
            this.AddOperation(TypeHelper.GetElementType(fnApply.Parameters[0].Type).GetTypeInfo(), fnApply);
        }

        /// <summary>
        /// Apply the transform function to any query (or sub query) with element type <see cref="T:TEntity"/>.
        /// </summary>
        public void Apply<TEntity>(Expression<Func<IEnumerable<TEntity>, IEnumerable<TEntity>>> fnApply)
        {
            Apply((LambdaExpression)fnApply);
        }

        /// <summary>
        /// Include the association member's elements in any query that produces the containing entity type.
        /// </summary>
        public void Include(MemberInfo member)
        {
            Include(member, false);
        }

        /// <summary>
        /// Include the association member's elements in any query that produces the containing entity type.
        /// </summary>
        /// <param name="member">The member whose elements will be included.</param>
        /// <param name="deferLoad">If true, the member's elements will be defer loaded if possible.</param>
        public void Include(MemberInfo member, bool deferLoad)
        {
            this.included.Add(member);
            if (deferLoad)
                Defer(member);
        }

        /// <summary>
        /// Include the association member's elements in the output any query that procudes the containing entity type.
        /// Specified as a lambda expression of an element of the containing type referencing the member.
        /// </summary>
        /// <param name="fnMember">A lambda expression that takes a single parameter of the entity type and a 
        /// body that references an association member of the parameter.</param>
        public void IncludeWith(LambdaExpression fnMember)
        {
            IncludeWith(fnMember, false);
        }

        /// <summary>
        /// Include the association member's elements in the output of any query that procudes the containing entity type.
        /// Specified as a lambda expression of an element of the containing type referencing the member.
        /// </summary>
        /// <param name="fnMember">A lambda expression that takes a single parameter of the entity type and a 
        /// body that references an association member of the parameter.</param>
        /// <param name="deferLoad">If true the member's elements will be defer loaded if possible.</param>
        public void IncludeWith(LambdaExpression fnMember, bool deferLoad)
        {
            var rootMember = RootMemberFinder.Find(fnMember, fnMember.Parameters[0]);
            if (rootMember == null)
                throw new InvalidOperationException("Subquery does not originate with a member access");
            Include(rootMember.Member, deferLoad);
            if (rootMember != fnMember.Body)
            {
                AssociateWith(fnMember);
            }
        }

        /// <summary>
        /// Include the association member's elements in the output of any query that procudes the containing entity type.
        /// Specified as a lambda expression of an element of the containing type referencing the member.
        /// </summary>
        /// <param name="fnMember">A lambda expression that takes a single parameter of the entity type and a 
        /// body that references an association member of the parameter.</param>
        public void IncludeWith<TEntity>(Expression<Func<TEntity, object>> fnMember)
        {
            IncludeWith((LambdaExpression)fnMember, false);
        }

        /// <summary>
        /// Include the association member's elements in the output of any query that procudes the containing entity type.
        /// Specified as a lambda expression of an element of the containing type referencing the member.
        /// </summary>
        /// <param name="fnMember">A lambda expression that takes a single parameter of the entity type and a 
        /// body that references an association member of the parameter.</param>
        /// <param name="deferLoad">If true the member's elements will be defer loaded if possible.</param>
        public void IncludeWith<TEntity>(Expression<Func<TEntity, object>> fnMember, bool deferLoad)
        {
            IncludeWith((LambdaExpression)fnMember, deferLoad);
        }

        private void Defer(MemberInfo member)
        {
            Type mType = TypeHelper.GetMemberType(member);

            if (mType.GetTypeInfo().IsGenericType)
            {
                var gType = mType.GetGenericTypeDefinition();
                if (gType != typeof(IEnumerable<>)
                    && gType != typeof(IList<>)
                    && !typeof(IDeferLoadable).GetTypeInfo().IsAssignableFrom(mType.GetTypeInfo()))
                {
                    throw new InvalidOperationException(string.Format("The member '{0}' cannot be deferred due to its type.", member));
                }
            }

            this.deferred.Add(member);
        }

        /// <summary>
        /// Add a constraint or filter to an association member that is always applied whenever the member is
        /// referenced in a query, by specifing an operation on that member in a lambda expression.
        /// </summary>
        public void AssociateWith(LambdaExpression memberQuery)
        {
            var rootMember = RootMemberFinder.Find(memberQuery, memberQuery.Parameters[0]);

            if (rootMember == null)
                throw new InvalidOperationException("Subquery does not originate with a member access");

            if (rootMember != memberQuery.Body)
            {
                var memberParam = Expression.Parameter(rootMember.Type, "root");
                var newBody = ExpressionReplacer.Replace(memberQuery.Body, rootMember, memberParam);
                this.AddOperation(rootMember.Member, Expression.Lambda(newBody, memberParam));
            }
        }

        /// <summary>
        /// Add a constraint or filter to an association member that is always applied whenever the member is
        /// referenced in a query, by specifing an operation on that member in a lambda expression.
        /// </summary>
        public void AssociateWith<TEntity>(Expression<Func<TEntity, IEnumerable>> memberQuery)
        {
            AssociateWith((LambdaExpression)memberQuery);
        }

        private void AddOperation(MemberInfo member, LambdaExpression operation)
        {
            List<LambdaExpression> memberOps;

            if (!this.operations.TryGetValue(member, out memberOps))
            {
                memberOps = new List<LambdaExpression>();
                this.operations.Add(member, memberOps);
            }

            memberOps.Add(operation);
        }

        /// <summary>
        /// Finds the member that is first accessed from the lambda parameter.
        /// </summary>
        class RootMemberFinder : ExpressionVisitor
        {
            MemberExpression found;
            ParameterExpression parameter;

            private RootMemberFinder(ParameterExpression parameter)
            {
                this.parameter = parameter;
            }

            public static MemberExpression Find(Expression query, ParameterExpression parameter)
            {
                var finder = new RootMemberFinder(parameter);
                finder.Visit(query);
                return finder.found;
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Object != null)
                {
                    this.Visit(m.Object);
                }
                else if (m.Arguments.Count > 0)
                {
                    this.Visit(m.Arguments[0]);
                }

                return m;
            }

            protected override Expression VisitMemberAccess(MemberExpression m)
            {
                if (m.Expression == this.parameter)
                {
                    this.found = m;
                    return m;
                }
                else
                {
                    return base.VisitMemberAccess(m);
                }
            }
        }

        /// <summary>
        /// True if the association member <see cref="P:member"/>'s elements are included in the output of the query. 
        /// </summary>
        public override bool IsIncluded(MemberInfo member)
        {
            return this.included.Contains(member);
        }

        /// <summary>
        /// True if the association member <see cref="P:member"/>'s are defer loaded.
        /// </summary>
        public override bool IsDeferLoaded(MemberInfo member)
        {
            return this.deferred.Contains(member);
        }

        /// <summary>
        /// Create a <see cref="QueryPolice"/> that is used during query translation to
        /// enforce the policy.
        /// </summary>
        public override QueryPolice CreatePolice(QueryTranslator translator)
        {
            return new Police(this, translator);
        }

        private class Police : QueryPolice
        {
            private readonly EntityPolicy policy;

            public Police(EntityPolicy policy, QueryTranslator translator)
                : base(policy, translator)
            {
                this.policy = policy;
            }

            public override Expression ApplyPolicy(Expression expression, MemberInfo member)
            {
                List<LambdaExpression> ops;

                if (this.policy.operations.TryGetValue(member, out ops))
                {
                    var result = expression;

                    foreach (var fnOp in ops)
                    {
                        var pop = PartialEvaluator.Eval(fnOp, this.Translator.Mapper.Mapping.CanBeEvaluatedLocally);
                        result = this.Translator.Mapper.ApplyMapping(Expression.Invoke(pop, result));
                    }

                    var projection = (ProjectionExpression)result;
                    if (projection.Type != expression.Type)
                    {
                        var fnAgg = Aggregator.GetAggregator(expression.Type, projection.Type);
                        projection = new ProjectionExpression(projection.Select, projection.Projector, fnAgg);
                    }

                    return projection;
                }

                return expression;
            }
        }
    }
}