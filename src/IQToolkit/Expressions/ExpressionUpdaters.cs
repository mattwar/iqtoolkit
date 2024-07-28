// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Expressions
{
    /// <summary>
    /// Additional Update methods for Expression nodes.
    /// </summary>
    public static class ExpressionUpdaters
    {
        public static BinaryExpression Update(
            this BinaryExpression original,
            Expression left,
            Expression right,
            Expression? conversion,
            bool isLiftedToNull,
            MethodInfo? method)
        {
            if (left != original.Left
                || right != original.Right
                || conversion != original.Conversion
                || method != original.Method
                || isLiftedToNull != original.IsLiftedToNull)
            {
                if (original.NodeType == ExpressionType.Coalesce && original.Conversion != null)
                {
                    return Expression.Coalesce(left, right, conversion as LambdaExpression);
                }
                else
                {
                    return Expression.MakeBinary(original.NodeType, left, right, isLiftedToNull, method);
                }
            }

            return original;
        }

        public static GotoExpression Update(
            this GotoExpression original,
            GotoExpressionKind kind,
            LabelTarget target,
            Expression? value,
            Type type)
        {
            if (kind != original.Kind
                || target != original.Target
                || value != original.Value
                || type != original.Type)
            {
                return Expression.MakeGoto(
                    kind,
                    target,
                    value,
                    type);
            }
            else
            {
                return original;
            }
        }

        public static IndexExpression Update(
            this IndexExpression original,
            Expression? instance,
            PropertyInfo indexer,
            IEnumerable<Expression> arguments)
        {
            if (instance != original.Object
                || indexer != original.Indexer
                || arguments != original.Arguments)
            {
                return Expression.MakeIndex(
                    instance,
                    indexer,
                    arguments
                    );
            }
            else
            {
                return original;
            }
        }

        public static LambdaExpression Update(
            this LambdaExpression original,
            Type delegateType,
            Expression body,
            IEnumerable<ParameterExpression> parameters)
        {
            if (body != original.Body
                || parameters != original.Parameters
                || delegateType != original.Type)
            {
                return Expression.Lambda(delegateType, body, parameters);
            }
            else
            {
                return original;
            }
        }

        public static MethodCallExpression Update(
            this MethodCallExpression original,
            Expression? instance,
            MethodInfo method,
            IEnumerable<Expression> arguments)
        {
            if (instance != original.Object
                || method != original.Method
                || arguments != original.Arguments)
            {
                return Expression.Call(instance, method, arguments);
            }
            else
            {
                return original;
            }
        }

        public static MemberExpression Update(
            this MemberExpression original,
            Expression expression,
            MemberInfo member)
        {
            if (expression != original.Expression
                || member != original.Member)
            {
                return Expression.MakeMemberAccess(expression, member);
            }
            else
            {
                return original;
            }
        }

        public static MemberAssignment Update(
            this MemberAssignment original,
            MemberInfo member,
            Expression expression)
        {
            if (expression != original.Expression
                || member != original.Member)
            {
                return Expression.Bind(member, expression);
            }
            else
            {
                return original;
            }
        }

        public static MemberMemberBinding Update(
            this MemberMemberBinding original,
            MemberInfo member,
            IEnumerable<MemberBinding> bindings)
        {
            if (bindings != original.Bindings
                || member != original.Member)
            {
                return Expression.MemberBind(member, bindings);
            }
            else
            {
                return original;
            }
        }

        public static MemberListBinding Update(
            this MemberListBinding binding,
            MemberInfo member,
            IEnumerable<ElementInit> initializers)
        {
            if (initializers != binding.Initializers
                || member != binding.Member)
            {
                return Expression.ListBind(member, initializers);
            }
            else
            {
                return binding;
            }
        }

        public static ElementInit Update(
            this ElementInit original,
            MethodInfo addMethod,
            IEnumerable<Expression> arguments)
        {
            if (addMethod != original.AddMethod
                || arguments != original.Arguments)
            {
                return Expression.ElementInit(addMethod, arguments);
            }
            else
            {
                return original;
            }
        }

        public static NewExpression Update(
            this NewExpression original,
            ConstructorInfo constructor,
            IEnumerable<Expression> arguments,
            IEnumerable<MemberInfo>? members)
        {
            if (constructor != original.Constructor
                || arguments != original.Arguments
                || members != original.Members)
            {
                if (original.Members != null)
                {
                    return Expression.New(constructor, arguments, members);
                }
                else
                {
                    return Expression.New(constructor, arguments);
                }
            }
            else
            {
                return original;
            }
        }

        public static NewArrayExpression Update(
            this NewArrayExpression original,
            Type arrayType,
            IEnumerable<Expression> expressions)
        {
            if (original.Type != arrayType
                || expressions != original.Expressions)
            {
                if (original.NodeType == ExpressionType.NewArrayInit)
                {
                    return Expression.NewArrayInit(arrayType.GetElementType(), expressions);
                }
                else
                {
                    return Expression.NewArrayBounds(arrayType.GetElementType(), expressions);
                }
            }
            else
            {
                return original;
            }
        }

        public static TypeBinaryExpression Update(
            this TypeBinaryExpression original,
            Expression expression,
            Type typeOperand)
        {
            if (expression != original.Expression
                || typeOperand != original.TypeOperand)
            {
                // only TypeIs expression is TypeBinary
                return Expression.TypeIs(expression, typeOperand);
            }
            else
            {
                return original;
            }
        }
    }
}