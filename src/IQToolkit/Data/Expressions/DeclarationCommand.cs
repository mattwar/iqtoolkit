// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    public sealed class DeclarationCommand : CommandExpression
    {
        public IReadOnlyList<VariableDeclaration> Variables { get; }
        public SelectExpression? Source { get; }

        public DeclarationCommand(IEnumerable<VariableDeclaration> variables, SelectExpression? source)
            : base(typeof(void))
        {
            this.Variables = variables.ToReadOnly();
            this.Source = source;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Declaration;

        public DeclarationCommand Update(
            IEnumerable<VariableDeclaration> variables, 
            SelectExpression? source)
        {
            if (variables != this.Variables 
                || source != this.Source)
            {
                return new DeclarationCommand(variables, source);
            }
            else
            {
                return this;
            }
        }
    }
}
