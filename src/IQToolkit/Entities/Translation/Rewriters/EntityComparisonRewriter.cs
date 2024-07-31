// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using IQToolkit.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using SqlExpressions;

    /// <summary>
    /// Rewrites comparisions of entities into comparisons of the primary key values.
    /// </summary>
    public class EntityComparisonRewriter : SqlExpressionVisitor
    {
        private readonly EntityMapping _mapping;

        public EntityComparisonRewriter(EntityMapping mapping)
        {
            _mapping = mapping;
        }

        protected override Expression VisitMember(MemberExpression original)
        {
            var modified = (MemberExpression)base.VisitMember(original);

            if (modified.Expression.TryResolveMemberAccess(modified.Member, out var resolvedAccess))
            {
                return resolvedAccess;
            }
            else
            {
                return modified;
            }
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            switch (b.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    Expression result = this.MakeComparison(b);
                    if (result == b)
                        goto default;
                    return this.Visit(result);
                default:
                    return base.VisitBinary(b);
            }
        }

        protected Expression SkipConvert(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Convert)
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression;
        }

        protected Expression MakeComparison(BinaryExpression bop)
        {
            var e1 = this.SkipConvert(bop.Left);
            var e2 = this.SkipConvert(bop.Right);

            var oj1 = e1 as OuterJoinedExpression;
            var oj2 = e2 as OuterJoinedExpression;

            var entity1 = oj1 != null ? oj1.Expression as EntityExpression : e1 as EntityExpression;
            var entity2 = oj2 != null ? oj2.Expression as EntityExpression : e2 as EntityExpression;

            bool negate = bop.NodeType == ExpressionType.NotEqual;

            // check for outer-joined entity comparing against null. These are special because outer joins have 
            // a test expression specifically desgined to be tested against null to determine if the joined side exists.
            if (oj1 != null && e2.NodeType == ExpressionType.Constant && ((ConstantExpression)e2).Value == null)
            {
                return MakeIsNull(oj1.Test, negate);
            }
            else if (oj2 != null && e1.NodeType == ExpressionType.Constant && ((ConstantExpression)e1).Value == null)
            {
                return MakeIsNull(oj2.Test, negate);
            }

            // if either side is an entity construction expression then compare using its primary key members
            if (entity1 != null)
            {
                return this.MakePredicate(e1, e2, _mapping.GetPrimaryKeyMembers(entity1.Entity), negate);
            }
            else if (entity2 != null)
            {
                return this.MakePredicate(e1, e2, _mapping.GetPrimaryKeyMembers(entity2.Entity), negate);
            }

            // check for comparison of user constructed type projections
            var dm1 = this.GetDefinedMembers(e1);
            var dm2 = this.GetDefinedMembers(e2);

            if (dm1 == null && dm2 == null)
            {
                // neither are constructed types
                return bop;
            }

            if (dm1 != null && dm2 != null)
            {
                // both are constructed types, so they'd better have the same members declared
                var names1 = new HashSet<string>(dm1.Select(m => m.Name));
                var names2 = new HashSet<string>(dm2.Select(m => m.Name));
                if (names1.IsSubsetOf(names2) && names2.IsSubsetOf(names1)) 
                {
                    return MakePredicate(e1, e2, dm1, negate);
                }
            }
            else if (dm1 != null)
            {
                return MakePredicate(e1, e2, dm1, negate);
            }
            else if (dm2 != null)
            {
                return MakePredicate(e1, e2, dm2, negate);
            }

            throw new InvalidOperationException("Cannot compare two constructed types with different sets of members assigned.");
        }

        protected Expression MakeIsNull(Expression expression, bool negate)
        {
            Expression isnull = new IsNullExpression(expression);
            return negate ? Expression.Not(isnull) : isnull;
        }

        protected Expression MakePredicate(Expression e1, Expression e2, IEnumerable<MemberInfo> members, bool negate)
        {
            var pred = members
                .Select(m => e1.ResolveMemberAccess(m).Equal(e2.ResolveMemberAccess(m)))
                .Combine(ExpressionType.And);

            if (negate)
                pred = Expression.Not(pred);

            return pred!;
        }

        private IEnumerable<MemberInfo>? GetDefinedMembers(Expression expr)
        {
            if (expr is MemberInitExpression mini)
            {
                var members = mini.Bindings.Select(b => FixMember(b.Member));

                if (mini.NewExpression.Members != null)
                {
                    members.Concat(mini.NewExpression.Members.Select(m => FixMember(m)));
                }

                return members;
            }
            else if (expr is NewExpression nex
                && nex.Members != null)
            {
                return nex.Members.Select(m => FixMember(m));
            }
            else
            {
                return null;
            }
        }

        private static MemberInfo FixMember(MemberInfo member)
        {
            if (member is MethodInfo && member.Name.StartsWith("get_"))
            {
                return member.DeclaringType.GetTypeInfo().GetDeclaredProperty(member.Name.Substring(4));
            }

            return member;
        }
    }
}
