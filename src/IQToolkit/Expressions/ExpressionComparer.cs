// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Expressions
{
    /// <summary>
    /// Compare two expressions to determine if they are equivalent.
    /// </summary>
    public class ExpressionComparer : IEqualityComparer<Expression>
    {
        private readonly Func<object?, object?, bool> _fnValueComparer;

        protected ExpressionComparer(Func<object?, object?, bool>? fnComparer)
        {
            _fnValueComparer = fnComparer
                ?? ((a, b) => object.Equals(a, b));
        }

        /// <summary>
        /// Creates a new <see cref="ExpressionComparer"/> using the specified function
        /// to compare values in <see cref="ConstantExpression"/> nodes.
        /// </summary>
        public ExpressionComparer WithValueComparer(Func<object?, object?, bool>? fnCompare) =>
            Create(fnCompare);

        protected virtual ExpressionComparer Create(Func<object?, object?, bool>? fnCompare)
        {
            return new ExpressionComparer(fnCompare);
        }

        /// <summary>
        /// The default <see cref="ExpressionComparer"/>, 
        /// compares equality of all normal <see cref="Expression"/> nodes.
        /// </summary>
        public static readonly ExpressionComparer Default =
            new ExpressionComparer(null);

        /// <summary>
        /// Returns true if the two <see cref="Expression"/> nodes are equivalent.
        /// </summary>
        public virtual bool Equals(Expression? x, Expression? y)
        {
            return this.Compare(x, y, Scope.Default);
        }

        public virtual bool Equals(
            Expression? x,
            Expression? y,
            ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap)
        {
            return this.Compare(x, y, Scope.Default.WithParameterMap(parameterMap));
        }

        public int GetHashCode(Expression obj)
        {
            // there's no good way to do this that is not as complex as the compare methods.
            // TODO: consider building composite hash code over entire expression tree
            return 0;
        }

        protected class Scope
        {
            public ImmutableDictionary<ParameterExpression, ParameterExpression> ParameterMap;

            protected Scope(
                ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap)
            {
                this.ParameterMap = parameterMap;
            }

            public static readonly Scope Default =
                new Scope(
                    ImmutableDictionary<ParameterExpression, ParameterExpression>.Empty);

            public Scope WithParameterMap(ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap) =>
                Create(parameterMap);

            protected virtual Scope Create(
                ImmutableDictionary<ParameterExpression, ParameterExpression> parameterMap)
            {
                return new Scope(parameterMap);
            }
        }

        protected virtual bool Compare(
            Expression? a, 
            Expression? b,
            Scope s)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.NodeType != b.NodeType)
                return false;
            if (a.Type != b.Type)
                return false;

            switch (a)
            {
                case BinaryExpression binaryA:
                    return this.CompareBinary(binaryA, (BinaryExpression)b, s);
                case BlockExpression blockA:
                    return this.CompareBlock(blockA, (BlockExpression)b, s);
                case ConditionalExpression condA:
                    return this.CompareConditional(condA, (ConditionalExpression)b, s);
                case ConstantExpression constA:
                    return this.CompareConstant(constA, (ConstantExpression)b, s);
                case DebugInfoExpression debugA:
                    return this.CompareDebugInfo(debugA, (DebugInfoExpression)b);
                case DefaultExpression defaultA:
                    return this.CompareDefault(defaultA, (DefaultExpression)b);
                case DynamicExpression dynamicA:
                    return this.CompareDynamic(dynamicA, (DynamicExpression)b, s);
                case GotoExpression gotoA:
                    return this.CompareGoto(gotoA, (GotoExpression)b, s);
                case IndexExpression indexA:
                    return this.CompareIndex(indexA, (IndexExpression)b, s);
                case InvocationExpression invokeA:
                    return this.CompareInvocation(invokeA, (InvocationExpression)b, s);
                case LabelExpression labelA:
                    return this.CompareLabel(labelA, (LabelExpression)b, s);
                case LambdaExpression lambdaA:
                    return this.CompareLambda(lambdaA, (LambdaExpression)b, s);
                case ListInitExpression listInitA:
                    return this.CompareListInit(listInitA, (ListInitExpression)b, s);
                case LoopExpression loopA:
                    return this.CompareLoop(loopA, (LoopExpression)b, s);
                case MemberExpression memberA:
                    return this.CompareMemberAccess(memberA, (MemberExpression)b, s);
                case MemberInitExpression memberInitA:
                    return this.CompareMemberInit(memberInitA, (MemberInitExpression)b, s);
                case MethodCallExpression methodA:
                    return this.CompareMethodCall(methodA, (MethodCallExpression)b, s);
                case NewExpression newA:
                    return this.CompareNew(newA, (NewExpression)b, s);
                case NewArrayExpression newArrayA:
                    return this.CompareNewArray(newArrayA, (NewArrayExpression)b, s);
                case ParameterExpression paramA:
                    return this.CompareParameter(paramA, (ParameterExpression)b, s);
                case RuntimeVariablesExpression rvariablesA:
                    return this.CompareRuntimeVariables(rvariablesA, (RuntimeVariablesExpression)b, s);
                case SwitchExpression switchA:
                    return this.CompareSwitch(switchA, (SwitchExpression)b, s);
                case TryExpression tryA:
                    return this.CompareTry(tryA, (TryExpression)b, s);
                case UnaryExpression unaryA:
                    return this.CompareUnary(unaryA, (UnaryExpression)b, s);
                case TypeBinaryExpression typeIsA:
                    return this.CompareTypeBinary(typeIsA, (TypeBinaryExpression)b, s);
                default:
                    throw new Exception($"Unhandled expression type: '{a.GetType().Name}'");
            }
        }

        protected virtual bool CompareBinary(
            BinaryExpression a, 
            BinaryExpression b,
            Scope s)
        {
            return a.NodeType == b.NodeType
                && a.Method == b.Method
                && a.IsLifted == b.IsLifted
                && a.IsLiftedToNull == b.IsLiftedToNull
                && this.Compare(a.Left, b.Left, s)
                && this.Compare(a.Right, b.Right, s);
        }

        protected virtual bool CompareBinding(
            MemberBinding a, 
            MemberBinding b,
            Scope s)
        {
            switch (a)
            {
                case MemberAssignment assignA:
                    return this.CompareMemberAssignment(assignA, (MemberAssignment)b, s);
                case MemberListBinding listA:
                    return this.CompareMemberListBinding(listA, (MemberListBinding)b, s);
                case MemberMemberBinding memberA:
                    return this.CompareMemberMemberBinding(memberA, (MemberMemberBinding)b, s);
                default:
                    throw new Exception($"Unhandled member binding type: '{a.GetType().Name}'");
            }
        }

        protected virtual bool CompareBindingList(
            IReadOnlyList<MemberBinding>? a, 
            IReadOnlyList<MemberBinding>? b,
            Scope s)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!this.CompareBinding(a[i], b[i], s))
                    return false;
            }

            return true;
        }

        protected virtual bool CompareBlock(
            BlockExpression a, 
            BlockExpression b,
            Scope s)
        {
            return this.CompareExpressionList(a.Expressions, b.Expressions, s);
        }

        protected virtual bool CompareConditional(
            ConditionalExpression a, 
            ConditionalExpression b,
            Scope s)
        {
            return this.Compare(a.Test, b.Test, s)
                && this.Compare(a.IfTrue, b.IfTrue, s)
                && this.Compare(a.IfFalse, b.IfFalse, s);
        }

        protected virtual bool CompareConstant(
            ConstantExpression a, 
            ConstantExpression b,
            Scope s)
        {
            return _fnValueComparer(a.Value, b.Value);
        }

        protected virtual bool CompareDebugInfo(
            DebugInfoExpression a,
            DebugInfoExpression b)
        {
            return this.CompareDocumentInfo(a.Document, b.Document)
                && a.StartLine == b.StartLine
                && a.StartColumn == b.StartColumn
                && a.EndLine == b.EndLine
                && a.EndColumn == b.EndColumn;
        }

        protected virtual bool CompareDefault(
            DefaultExpression a,
            DefaultExpression b)
        {
            return a.Type == b.Type;
        }

        protected virtual bool CompareDocumentInfo(
            SymbolDocumentInfo a,
            SymbolDocumentInfo b)
        {
            return a.DocumentType == b.DocumentType
                && a.FileName == b.FileName
                && a.Language == b.Language
                && a.LanguageVendor == b.LanguageVendor;
        }

        protected virtual bool CompareDynamic(
            DynamicExpression a,
            DynamicExpression b,
            Scope s)
        {
            return a.DelegateType == b.DelegateType
                && a.Binder == b.Binder
                && this.CompareExpressionList(a.Arguments, b.Arguments, s);
        }

        protected virtual bool CompareElementInit(
            ElementInit a, 
            ElementInit b,
            Scope s)
        {
            return a.AddMethod == b.AddMethod
                && this.CompareExpressionList(a.Arguments, b.Arguments, s);
        }

        protected virtual bool CompareElementInitList(
            IReadOnlyList<ElementInit>? a, 
            IReadOnlyList<ElementInit>? b,
            Scope s)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!this.CompareElementInit(a[i], b[i], s))
                    return false;
            }

            return true;
        }

        protected virtual bool CompareExpressionList<TExpression>(
            IReadOnlyList<TExpression>? a, 
            IReadOnlyList<TExpression>? b,
            Scope s)
            where TExpression : Expression
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!this.Compare(a[i], b[i], s))
                    return false;
            }

            return true;
        }

        protected virtual bool CompareGoto(
            GotoExpression a,
            GotoExpression b,
            Scope s)
        {
            return a.Target == b.Target
                && this.Compare(a.Value, b.Value, s);
        }

        protected virtual bool CompareIndex(
            IndexExpression a,
            IndexExpression b,
            Scope s)
        {
            return this.Compare(a.Object, b.Object, s)
                && a.Indexer == b.Indexer
                && this.CompareExpressionList(a.Arguments, b.Arguments, s);
        }

        protected virtual bool CompareInvocation(
            InvocationExpression a, 
            InvocationExpression b,
            Scope s)
        {
            return this.Compare(a.Expression, b.Expression, s)
                && this.CompareExpressionList(a.Arguments, b.Arguments, s);
        }

        protected virtual bool CompareLabel(
            LabelExpression a,
            LabelExpression b,
            Scope s)
        {
            return a.Target == b.Target
                && this.Compare(a.DefaultValue, b.DefaultValue, s);
        }

        protected virtual bool CompareLambda(
            LambdaExpression a, 
            LambdaExpression b,
            Scope s)
        {
            var n = a.Parameters.Count;
            if (b.Parameters.Count != n)
                return false;

            // all must have same type
            for (int i = 0; i < n; i++)
            {
                if (a.Parameters[i].Type != b.Parameters[i].Type)
                    return false;
            }

            var map = s.ParameterMap;
            for (int i = 0; i < n; i++)
            {
                map = map.Add(a.Parameters[i], b.Parameters[i]);
            }
            s = s.WithParameterMap(map);

            return this.Compare(a.Body, b.Body, s);
        }

        protected virtual bool CompareListInit(
            ListInitExpression a, 
            ListInitExpression b,
            Scope s)
        {
            return this.Compare(a.NewExpression, b.NewExpression, s)
                && this.CompareElementInitList(a.Initializers, b.Initializers, s);
        }

        protected virtual bool CompareLoop(
            LoopExpression a, 
            LoopExpression b,
            Scope s)
        {
            return this.Compare(a.Body, b.Body, s)
                && a.BreakLabel == b.BreakLabel
                && a.ContinueLabel == b.ContinueLabel;
        }

        protected virtual bool CompareMemberAccess(
            MemberExpression a, 
            MemberExpression b,
            Scope s)
        {
            return a.Member == b.Member
                && this.Compare(a.Expression, b.Expression, s);
        }

        protected virtual bool CompareMemberAssignment(
            MemberAssignment a, 
            MemberAssignment b,
            Scope s)
        {
            return a.Member == b.Member
                && this.Compare(a.Expression, b.Expression, s);
        }

        protected virtual bool CompareMemberInit(
            MemberInitExpression a, 
            MemberInitExpression b,
            Scope s)
        {
            return this.Compare(a.NewExpression, b.NewExpression, s)
                && this.CompareBindingList(a.Bindings, b.Bindings, s);
        }

        protected virtual bool CompareMemberList(
            IReadOnlyList<MemberInfo>? a, 
            IReadOnlyList<MemberInfo>? b)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        protected virtual bool CompareMemberListBinding(
            MemberListBinding a, 
            MemberListBinding b,
            Scope s)
        {
            return a.Member == b.Member
                && this.CompareElementInitList(a.Initializers, b.Initializers, s);
        }

        protected virtual bool CompareMemberMemberBinding(
            MemberMemberBinding a, 
            MemberMemberBinding b,
            Scope s)
        {
            return a.Member == b.Member
                && this.CompareBindingList(a.Bindings, b.Bindings, s);
        }

        protected virtual bool CompareMethodCall(
            MethodCallExpression a, 
            MethodCallExpression b,
            Scope s)
        {
            return a.Method == b.Method
                && this.Compare(a.Object, b.Object, s)
                && this.CompareExpressionList(a.Arguments, b.Arguments, s);
        }

        protected virtual bool CompareNew(
            NewExpression a, 
            NewExpression b,
            Scope s)
        {
            return a.Constructor == b.Constructor
                && this.CompareExpressionList(a.Arguments, b.Arguments, s)
                && this.CompareMemberList(a.Members, b.Members);
        }

        protected virtual bool CompareNewArray(
            NewArrayExpression a, 
            NewArrayExpression b,
            Scope s)
        {
            return this.CompareExpressionList(a.Expressions, b.Expressions, s);
        }

        protected virtual bool CompareParameter(
            ParameterExpression a, 
            ParameterExpression b,
            Scope s)
        {
            if (s.ParameterMap.TryGetValue(a, out var mapped))
                return mapped == b;

            return a == b;
        }

        protected virtual bool CompareRuntimeVariables(
            RuntimeVariablesExpression a, 
            RuntimeVariablesExpression b,
            Scope s)
        {
            return this.CompareExpressionList(a.Variables, b.Variables, s);
        }

        protected virtual bool CompareSwitch(
            SwitchExpression a, 
            SwitchExpression b,
            Scope s)
        {
            return this.Compare(a.SwitchValue, b.SwitchValue, s)
                && this.Compare(a.DefaultBody, b.DefaultBody, s)
                && a.Comparison == b.Comparison
                && this.CompareSwitchCaseList(a.Cases, b.Cases, s);
        }

        protected virtual bool CompareSwitchCase(
            SwitchCase a, 
            SwitchCase b,
            Scope s)
        {
            return this.CompareExpressionList(a.TestValues, b.TestValues, s)
                && this.Compare(a.Body, b.Body, s);
        }

        protected virtual bool CompareSwitchCaseList(
            IReadOnlyList<SwitchCase>? a, 
            IReadOnlyList<SwitchCase>? b,
            Scope s)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!this.CompareSwitchCase(a[i], b[i], s))
                    return false;
            }

            return true;
        }

        protected virtual bool CompareTry(
            TryExpression a, 
            TryExpression b,
            Scope s)
        {
            return this.Compare(a.Body, b.Body, s)
                && this.Compare(a.Fault, b.Fault, s)
                && this.Compare(a.Finally, b.Finally, s)
                && this.CompareCatchBlockList(a.Handlers, b.Handlers, s);
        }

        protected virtual bool CompareCatchBlockList(
            IReadOnlyList<CatchBlock>? a, 
            IReadOnlyList<CatchBlock>? b,
            Scope s)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!this.CompareCatchBlock(a[i], b[i], s))
                    return false;
            }

            return true;
        }

        protected virtual bool CompareCatchBlock(
            CatchBlock a, 
            CatchBlock b,
            Scope s)
        {
            return a.Test == b.Test
                && this.Compare(a.Body, b.Body, s)
                && this.Compare(a.Variable, b.Variable, s);
        }

        protected virtual bool CompareUnary(
            UnaryExpression a, 
            UnaryExpression b,
            Scope s)
        {
            return a.NodeType == b.NodeType
                && a.Method == b.Method
                && a.IsLifted == b.IsLifted
                && a.IsLiftedToNull == b.IsLiftedToNull
                && this.Compare(a.Operand, b.Operand, s);
        }

        protected virtual bool CompareTypeBinary(
            TypeBinaryExpression a, 
            TypeBinaryExpression b,
            Scope s)
        {
            return a.TypeOperand == b.TypeOperand
                && this.Compare(a.Expression, b.Expression, s);
        }
    }
}