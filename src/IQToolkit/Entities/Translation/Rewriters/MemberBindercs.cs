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
    using Utils;

    public static class MemberBinder
    {
        public static bool TryResolveMemberAccess(
            this Expression source, MemberInfo member, out Expression resolved)
        {
            switch (source)
            {
                case EntityExpression entity:
                    if (TryResolveMemberAccess(entity.Expression, member, out resolved))
                        return true;
                    break;

                case UnaryExpression ux when ux.NodeType == ExpressionType.Convert:
                    if (TryResolveMemberAccess(ux.Operand, member, out resolved))
                        return true;
                    break;

                case MemberInitExpression memberInit:
                    for (int i = 0, n = memberInit.Bindings.Count; i < n; i++)
                    {
                        if (memberInit.Bindings[i] is MemberAssignment assign
                            && MembersMatch(assign.Member, member))
                        {
                            resolved = assign.Expression;
                            return true;
                        }
                    }
                    break;

                case NewExpression nex:
                    if (nex.Members != null)
                    {
                        for (int i = 0, n = nex.Members.Count; i < n; i++)
                        {
                            if (MembersMatch(nex.Members[i], member))
                            {
                                resolved = nex.Arguments[i];
                                return true;
                            }
                        }
                    }
                    else if (nex.Type.GetTypeInfo().IsGenericType && nex.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
                    {
                        if (member.Name == "Key")
                        {
                            resolved = nex.Arguments[0];
                            return true;
                        }
                    }
                    break;

                case ClientProjectionExpression proj:
                    // member access on a projection turns into a new projection w/ member access applied
                    var newProjector = ResolveMemberAccess(proj.Projector, member);
                    var mt = TypeHelper.GetMemberType(member);
                    resolved = new ClientProjectionExpression(proj.Select, newProjector, Aggregator.GetAggregator(mt, typeof(IEnumerable<>).MakeGenericType(mt)));
                    return true;

                case OuterJoinedExpression oj:                  
                    var em = ResolveMemberAccess(oj.Expression, member);
                    resolved = em is ColumnExpression
                        ? em
                        : new OuterJoinedExpression(oj.Test, em);
                    return true;

                case ConditionalExpression cex:
                    if (TryResolveMemberAccess(cex.IfTrue, member, out var ifTrueResolved)
                        && TryResolveMemberAccess(cex.IfFalse, member, out var ifFalseResolved))
                    {
                        resolved = Expression.Condition(cex.Test, ifTrueResolved, ifFalseResolved);
                        return true;
                    }
                    break;

                case ConstantExpression con:
                    var memberType = TypeHelper.GetMemberType(member);
                    if (con.Value == null)
                    {
                        resolved = Expression.Constant(TypeHelper.GetDefault(memberType), memberType);
                        return true;
                    }
                    else
                    {
                        resolved = Expression.Constant(TypeHelper.GetFieldOrPropertyValue(member, con.Value), memberType);
                        return true;
                    }
            }

            resolved = default!;
            return false;
        }

        /// <summary>
        /// Gets the expression that represents the result of acessing the member of the source expression.
        /// </summary>
        public static Expression ResolveMemberAccess(this Expression source, MemberInfo member)
        {
            return TryResolveMemberAccess(source, member, out var resolved)
                ? resolved
                : Expression.MakeMemberAccess(source, member);
        }

        private static bool MembersMatch(MemberInfo a, MemberInfo b)
        {
            if (a.Name == b.Name)
            {
                return true;
            }

            if (a is MethodInfo && b is PropertyInfo)
            {
                return a.Name == ((PropertyInfo)b).GetMethod.Name;
            }
            else if (a is PropertyInfo && b is MethodInfo)
            {
                return ((PropertyInfo)a).GetMethod.Name == b.Name;
            }

            return false;
        }
    }
}