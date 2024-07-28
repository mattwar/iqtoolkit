// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Expressions
{
    /// <summary>
    /// The base class of an expression rewriter.
    /// </summary>
    public abstract class ExpressionRewriter
    {
        public ExpressionRewriter()
        {
        }

        /// <summary>
        /// Same as Visit, but allows nulls.
        /// </summary>
        protected Expression? RewriteN(Expression? expr)
        {
            if (expr == null)
                return null;
            return this.Rewrite(expr);
        }

#if DEBUG
        private const int MAX_DEPTH = 1000;
        private int _depth = 0;
#endif

        /// <summary>
        /// Dispatch to the appropriate VisitXXX method.
        /// </summary>
        public virtual Expression Rewrite(Expression expression)
        {
#if DEBUG
            if (++_depth > MAX_DEPTH)
            {
            }
#endif
            Expression result;

            switch (expression)
            {
                case BinaryExpression be:
                    result = this.RewriteBinary(be);
                    break;
                case BlockExpression bl:
                    result = this.RewriteBlock(bl);
                    break;
                case ConditionalExpression ce:
                    result = this.RewriteConditional(ce);
                    break;
                case ConstantExpression ce:
                    result = this.RewriteConstant(ce);
                    break;
                case DebugInfoExpression dix:
                    result = this.RewriteDebugInfo(dix);
                    break;
                case DefaultExpression defx:
                    result = this.RewriteDefault(defx);
                    break;
                case DynamicExpression de:
                    result = this.RewriteDynamic(de);
                    break;
                case GotoExpression gx:
                    result = this.RewriteGoto(gx);
                    break;
                case IndexExpression ix:
                    result = this.RewriteIndex(ix);
                    break;
                case InvocationExpression ie:
                    result = this.RewriteInvocation(ie);
                    break;
                case LabelExpression lab:
                    result = this.RewriteLabel(lab);
                    break;
                case LambdaExpression le:
                    result = this.RewriteLambda(le);
                    break;
                case ListInitExpression li:
                    result = this.RewriteListInit(li);
                    break;
                case LoopExpression loop:
                    result = this.RewriteLoop(loop);
                    break;
                case MethodCallExpression mc:
                    result = this.RewriteMethodCall(mc);
                    break;
                case MemberExpression me:
                    result = this.RewriteMemberAccess(me);
                    break;
                case MemberInitExpression mi:
                    result = this.RewriteMemberInit(mi);
                    break;
                case NewExpression ne:
                    result = this.RewriteNew(ne);
                    break;
                case NewArrayExpression na:
                    result = this.RewriteNewArray(na);
                    break;
                case ParameterExpression pe:
                    result = this.RewriteParameter(pe);
                    break;
                case RuntimeVariablesExpression rve:
                    result = this.RewriteRuntimeVariables(rve);
                    break;
                case SwitchExpression sx:
                    result = this.RewriteSwitch(sx);
                    break;
                case TryExpression trx:
                    result = this.RewriteTry(trx);
                    break;
                case TypeBinaryExpression tbe:
                    result = this.RewriteTypeBinary(tbe);
                    break;
                case UnaryExpression ue:
                    result = this.RewriteUnary(ue);
                    break;
                default:
                    result = this.RewriteUnknown(expression);
                    break;
            }
#if DEBUG
            _depth--;
#endif

            return result;
        }

        protected virtual Expression RewriteBinary(BinaryExpression original)
        {
            var left = this.Rewrite(original.Left);
            var right = this.Rewrite(original.Right);
            var conversion = this.RewriteN(original.Conversion);

            return original.Update(
                left, 
                right, 
                conversion, 
                original.IsLiftedToNull, 
                original.Method);
        }

        protected virtual Expression RewriteBlock(BlockExpression original)
        {
            var variables = this.RewriteList(original.Variables, this.RewriteVariableDeclaration);
            var expressions = this.RewriteExpressionList(original.Expressions);
            return original.Update(variables, expressions);
        }

        protected virtual ParameterExpression RewriteVariableDeclaration(ParameterExpression original)
        {
            return original;
        }

        protected virtual Expression RewriteConditional(ConditionalExpression original)
        {
            var test = this.Rewrite(original.Test);
            var ifTrue = this.Rewrite(original.IfTrue);
            var ifFalse = this.Rewrite(original.IfFalse);
            return original.Update(test, ifTrue, ifFalse);
        }

        protected virtual Expression RewriteConstant(ConstantExpression original)
        {
            return original;
        }

        protected virtual Expression RewriteDebugInfo(DebugInfoExpression original)
        {
            return original;
        }

        protected virtual Expression RewriteDefault(DefaultExpression original)
        {
            return original;
        }

        protected virtual Expression RewriteDynamic(DynamicExpression original)
        {
            var arguments = this.RewriteExpressionList(original.Arguments);
            return original.Update(arguments);
        }

        protected virtual Expression RewriteGoto(GotoExpression original)
        {
            var value = this.RewriteN(original.Value);
            return original.Update(
                original.Kind,
                original.Target,
                value,
                original.Type
                );
        }

        protected virtual Expression RewriteIndex(IndexExpression original)
        {
            var instance = this.RewriteN(original.Object);
            var arguments = this.RewriteExpressionList(original.Arguments);
            return original.Update(instance, original.Indexer, arguments);
        }

        protected virtual Expression RewriteInvocation(InvocationExpression original)
        {
            var expr = this.Rewrite(original.Expression);
            var args = this.RewriteExpressionList(original.Arguments);
            return original.Update(expr, args);
        }

        protected virtual Expression RewriteLabel(LabelExpression original)
        {
            var defaultValue = this.RewriteN(original.DefaultValue);
            return original.Update(original.Target, defaultValue);
        }

        protected virtual Expression RewriteLambda(LambdaExpression original)
        {
            var body = this.Rewrite(original.Body);
            return original.Update(original.Type, body, original.Parameters);
        }

        protected virtual Expression RewriteListInit(ListInitExpression original)
        {
            var newExpression = (NewExpression)this.Rewrite(original.NewExpression);
            var initializers = this.RewriteElementInitializerList(original.Initializers);
            return original.Update(newExpression, initializers);
        }

        protected virtual Expression RewriteLoop(LoopExpression original)
        {
            var body = this.Rewrite(original.Body);
            return original.Update(original.BreakLabel, original.ContinueLabel, body);
        }

        protected virtual Expression RewriteMethodCall(MethodCallExpression original)
        {
            var instance = this.RewriteN(original.Object);
            var arguments = this.RewriteExpressionList(original.Arguments);
            return original.Update(instance, original.Method, arguments);
        }

        protected virtual Expression RewriteMemberAccess(MemberExpression original)
        {
            var exp = this.Rewrite(original.Expression);
            return original.Update(exp, original.Member);
        }

        protected virtual Expression RewriteMemberInit(MemberInitExpression original)
        {
            var newExpression = (NewExpression)this.Rewrite(original.NewExpression);
            var bindings = this.RewriteBindingList(original.Bindings);
            return original.Update(newExpression, bindings);
        }

        protected virtual MemberBinding RewriteBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.RewriteMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return this.RewriteMemberMemberBinding((MemberMemberBinding)binding);
                case MemberBindingType.ListBinding:
                    return this.RewriteMemberListBinding((MemberListBinding)binding);
                default:
                    throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
            }
        }

        protected virtual IReadOnlyList<MemberBinding> RewriteBindingList(
            IReadOnlyList<MemberBinding> original) =>
            this.RewriteList(original, this.RewriteBinding);


        protected virtual MemberAssignment RewriteMemberAssignment(MemberAssignment original)
        {
            var expression = this.Rewrite(original.Expression);
            return original.Update(original.Member, expression);
        }

        protected virtual MemberMemberBinding RewriteMemberMemberBinding(MemberMemberBinding original)
        {
            var bindings = this.RewriteBindingList(original.Bindings);
            return original.Update(original.Member, bindings);
        }

        protected virtual MemberListBinding RewriteMemberListBinding(MemberListBinding original)
        {
            var initializers = this.RewriteElementInitializerList(original.Initializers);
            return original.Update(original.Member, initializers);
        }

        protected virtual ElementInit RewriteElementInitializer(ElementInit original)
        {
            var arguments = this.RewriteExpressionList(original.Arguments);
            return original.Update(original.AddMethod, arguments);
        }

        protected virtual IReadOnlyList<ElementInit> RewriteElementInitializerList(
            IReadOnlyList<ElementInit> original) =>
            this.RewriteList(original, this.RewriteElementInitializer);

        protected virtual Expression RewriteNew(NewExpression original)
        {
            var arguments = this.RewriteExpressionList(original.Arguments);
            return original.Update(original.Constructor, arguments, original.Members);
        }

        protected virtual Expression RewriteNewArray(NewArrayExpression original)
        {
            var exprs = this.RewriteExpressionList(original.Expressions);
            return original.Update(original.Type, exprs);
        }

        protected virtual Expression RewriteParameter(ParameterExpression original)
        {
            return original;
        }

        protected virtual Expression RewriteRuntimeVariables(RuntimeVariablesExpression original)
        {
            var variables = this.RewriteList(original.Variables, this.RewriteVariableDeclaration);
            return original.Update(variables);
        }

        protected virtual Expression RewriteSwitch(SwitchExpression original)
        {
            var switchValue = this.Rewrite(original.SwitchValue);
            var cases = this.RewriteList(original.Cases, this.RewriteSwitchCase);
            var defaultBody = this.RewriteN(original.DefaultBody);
            return original.Update(switchValue, cases, defaultBody);
        }

        protected virtual SwitchCase RewriteSwitchCase(SwitchCase original)
        {
            var testValues = this.RewriteExpressionList(original.TestValues);
            var body = this.Rewrite(original.Body);
            return original.Update(testValues, body);
        }

        protected virtual Expression RewriteTry(TryExpression original)
        {
            var body = this.Rewrite(original.Body);
            var handlers = this.RewriteList(original.Handlers, this.RewriteCatchBlock);
            var @finally = this.RewriteN(original.Finally);
            var fault = this.RewriteN(original.Fault);
            return original.Update(body, handlers, @finally, fault);
        }

        protected virtual CatchBlock RewriteCatchBlock(CatchBlock original)
        {
            var variable = original.Variable != null ? this.RewriteVariableDeclaration(original.Variable) : null;
            var filter = this.RewriteN(original.Filter);
            var body = this.Rewrite(original.Body);
            return original.Update(variable, filter, body);
        }

        protected virtual Expression RewriteTypeBinary(TypeBinaryExpression original)
        {
            var expr = this.Rewrite(original.Expression);
            return original.Update(expr);
        }

        protected virtual Expression RewriteUnary(UnaryExpression original)
        {
            var operand = this.Rewrite(original.Operand);
            return original.Update(operand);
        }

        /// <summary>
        /// Handles/rewrites an unknown type of expression.
        /// </summary>
        protected virtual Expression RewriteUnknown(Expression expression)
        {
            throw new InvalidOperationException($"Unhandled expression type: '{expression.NodeType}'");
        }

        /// <summary>
        /// Rewrites a list given an item rewriter.
        /// </summary>
        protected IReadOnlyList<T> RewriteList<T>(
            IReadOnlyList<T> original,
            Func<T, T?> fnItemRewriter)
            where T : class
        {
            List<T>? list = null;

            for (int i = 0, n = original.Count; i < n; i++)
            {
                var newT = fnItemRewriter(original[i]);
                if (newT != null)
                {
                    if (list != null)
                    {
                        list.Add(newT);
                    }
                    else if (newT != original[i])
                    {
                        list = new List<T>(n);

                        for (int j = 0; j < i; j++)
                        {
                            list.Add(original[j]);
                        }

                        list.Add(newT);
                    }
                }
            }

            if (list != null)
            {
                return list.AsReadOnly();
            }

            return original;
        }

        /// <summary>
        /// Rewrites a list of expressions.
        /// </summary>
        protected virtual IReadOnlyList<Expression> RewriteExpressionList(
            IReadOnlyList<Expression> original) =>
            RewriteList(original, exp => this.Rewrite(exp));
    }
}