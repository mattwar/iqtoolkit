// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Expressions
{
    public class ExpressionShallowCloner : ExpressionVisitor<Expression>
    {
        protected ExpressionShallowCloner()
        {
        }

        private static readonly ExpressionShallowCloner _instance =
            new ExpressionShallowCloner();

        public static Expression ShallowClone(Expression expression)
        {
            return _instance.Visit(expression);
        }

        protected override Expression VisitBinary(BinaryExpression expr)
        {
            return Expression.MakeBinary(expr.NodeType, expr.Left, expr.Right);
        }

        protected override Expression VisitBlock(BlockExpression expr)
        {
            return Expression.Block(expr.Type, expr.Variables, expr.Expressions);
        }

        protected override Expression VisitConditional(ConditionalExpression expr)
        {
            return Expression.Condition(expr.Test, expr.IfTrue, expr.IfFalse);
        }

        protected override Expression VisitConstant(ConstantExpression expr)
        {
            return Expression.Constant(expr.Value, expr.Type);
        }

        protected override Expression VisitDebugInfo(DebugInfoExpression expr)
        {
            return Expression.DebugInfo(expr.Document, expr.StartLine, expr.StartColumn, expr.EndLine, expr.EndColumn);
        }

        protected override Expression VisitDefault(DefaultExpression expr)
        {
            return Expression.Default(expr.Type);
        }

        protected override Expression VisitDynamic(DynamicExpression expr)
        {
            return Expression.Dynamic(expr.Binder, expr.Type, expr.Arguments);
        }

        protected override Expression VisitGoto(GotoExpression expr)
        {
            return Expression.MakeGoto(expr.Kind, expr.Target, expr.Value, expr.Type);
        }

        protected override Expression VisitIndex(IndexExpression expr)
        {
            return Expression.MakeIndex(expr.Object, expr.Indexer, expr.Arguments);
        }

        protected override Expression VisitInvocation(InvocationExpression expr)
        {
            return Expression.Invoke(expr.Expression, expr.Arguments);
        }

        protected override Expression VisitLabel(LabelExpression expr)
        {
            return Expression.Label(expr.Target, expr.DefaultValue);
        }

        protected override Expression VisitLambda(LambdaExpression expr)
        {
            return Expression.Lambda(expr.Body, expr.Parameters);
        }

        protected override Expression VisitListInit(ListInitExpression expr)
        {
            return Expression.ListInit(expr.NewExpression, expr.Initializers);
        }

        protected override Expression VisitLoop(LoopExpression expr)
        {
            return Expression.Loop(expr.Body, expr.BreakLabel, expr.ContinueLabel);
        }

        protected override Expression VisitMemberAccess(MemberExpression expr)
        {
            return Expression.MakeMemberAccess(expr.Expression, expr.Member);
        }

        protected override Expression VisitMemberInit(MemberInitExpression expr)
        {
            return Expression.MemberInit(expr.NewExpression, expr.Bindings);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expr)
        {
            return Expression.Call(expr.Object, expr.Method, expr.Arguments);
        }

        protected override Expression VisitNew(NewExpression expr)
        {
            return Expression.New(expr.Constructor, expr.Arguments);
        }

        protected override Expression VisitNewArray(NewArrayExpression expr)
        {
            if (expr.NodeType == ExpressionType.NewArrayBounds)
                return Expression.NewArrayBounds(expr.Type.GetElementType(), expr.Expressions);
            return Expression.NewArrayInit(expr.Type.GetElementType(), expr.Expressions);
        }

        protected override Expression VisitParameter(ParameterExpression expr)
        {
            return expr;
        }

        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression expr)
        {
            return Expression.RuntimeVariables(expr.Variables);
        }

        protected override Expression VisitSwitch(SwitchExpression expr)
        {
            return Expression.Switch(expr.SwitchValue, expr.DefaultBody, expr.Comparison, expr.Cases);
        }

        protected override Expression VisitTry(TryExpression expr)
        {
            return Expression.MakeTry(expr.Type, expr.Body, expr.Finally, expr.Fault, expr.Handlers);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression expr)
        {
            return Expression.TypeIs(expr.Expression, expr.Type);
        }

        protected override Expression VisitUnary(UnaryExpression expr)
        {
            return Expression.MakeUnary(expr.NodeType, expr.Operand, expr.Type);
        }

        protected override Expression VisitUnhandled(Expression expression)
        {
            throw new InvalidOperationException($"Expression type '{expression.GetType().Name}' unhandled in {nameof(ExpressionShallowCloner)}.");
        }
    }
}