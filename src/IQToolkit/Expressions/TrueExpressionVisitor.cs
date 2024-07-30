// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Expressions
{
    /// <summary>
    /// The base class of an expression visitor.
    /// </summary>
    public abstract class TrueExpressionVisitor
    {
        protected TrueExpressionVisitor()
        {
        }

        public virtual void Visit(Expression? expression)
        {
            if (expression == null)
                return;

            switch (expression)
            {
                case BinaryExpression be:
                    this.VisitBinary(be);
                    break;
                case BlockExpression bl:
                    this.VisitBlock(bl);
                    break;
                case ConditionalExpression ce:
                    this.VisitConditional(ce);
                    break;
                case ConstantExpression ce:
                    this.VisitConstant(ce);
                    break;
                case DebugInfoExpression dix:
                    this.VisitDebugInfo(dix);
                    break;
                case DefaultExpression defx:
                    this.VisitDefault(defx);
                    break;
                case DynamicExpression de:
                    this.VisitDynamic(de);
                    break;
                case GotoExpression gx:
                    this.VisitGoto(gx);
                    break;
                case IndexExpression ix:
                    this.VisitIndex(ix);
                    break;
                case InvocationExpression ie:
                    this.VisitInvocation(ie);
                    break;
                case LabelExpression lab:
                    this.VisitLabel(lab);
                    break;
                case LambdaExpression le:
                    this.VisitLambda(le);
                    break;
                case ListInitExpression li:
                    this.VisitListInit(li);
                    break;
                case LoopExpression loop:
                    this.VisitLoop(loop);
                    break;
                case MemberExpression me:
                    this.VisitMemberAccess(me);
                    break;
                case MemberInitExpression mi:
                    this.VisitMemberInit(mi);
                    break;
                case MethodCallExpression mc:
                    this.VisitMethodCall(mc);
                    break;
                case NewExpression ne:
                    this.VisitNew(ne);
                    break;
                case NewArrayExpression na:
                    this.VisitNewArray(na);
                    break;
                case ParameterExpression pe:
                    this.VisitParameter(pe);
                    break;
                case RuntimeVariablesExpression rve:
                    this.VisitRuntimeVariables(rve);
                    break;
                case SwitchExpression sx:
                    this.VisitSwitch(sx);
                    break;
                case TryExpression trx:
                    this.VisitTry(trx);
                    break;
                case TypeBinaryExpression tbe:
                    this.VisitTypeBinary(tbe);
                    break;
                case UnaryExpression ue:
                    this.VisitUnary(ue);
                    break;
                default:
                    this.VisitUnknown(expression);
                    break;
            }
        }

        protected virtual void VisitBinary(BinaryExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitBlock(BlockExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitConstant(ConstantExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitConditional(ConditionalExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitDebugInfo(DebugInfoExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitDefault(DefaultExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitDynamic(DynamicExpression expr) => VisitDynamic(expr);
        protected virtual void VisitGoto(GotoExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitIndex(IndexExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitInvocation(InvocationExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitLabel(LabelExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitLambda(LambdaExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitLoop(LoopExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitListInit(ListInitExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitMemberAccess(MemberExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitMemberInit(MemberInitExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitMethodCall(MethodCallExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitNew(NewExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitNewArray(NewArrayExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitParameter(ParameterExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitRuntimeVariables(RuntimeVariablesExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitSwitch(SwitchExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitTry(TryExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitTypeBinary(TypeBinaryExpression expr) => VisitUnhandled(expr);
        protected virtual void VisitUnary(UnaryExpression expr) => VisitUnhandled(expr);


        /// <summary>
        /// Called when an unknown expression type is encountered.
        /// </summary>
        protected virtual void VisitUnknown(Expression expression)
        {
            throw new Exception($"Unknown expression type: '{expression.GetType().Name}'");
        }

        /// <summary>
        /// Called when an expression type is not overridden.
        /// </summary>
        protected virtual void VisitUnhandled(Expression expression)
        {
        }

        protected virtual void VisitList<TItem>(
            IReadOnlyList<TItem> list,
            Action<TItem> fnItemVisitor)
        {
            foreach (var item in list)
            {
                fnItemVisitor(item);
            }
        }

        protected virtual void VisitExpressionList<TExpression>(
            IReadOnlyList<TExpression> list)
            where TExpression : Expression
        {
            foreach (var item in list)
            {
                this.Visit(item);
            }
        }
    }
}