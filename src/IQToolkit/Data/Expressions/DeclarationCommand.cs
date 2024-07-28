﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    /// <summary>
    /// A <see cref="CommandExpression"/> that declared variables.
    /// </summary>
    public sealed class DeclarationCommand : CommandExpression
    {
        /// <summary>
        /// The declared variables.
        /// </summary>
        public IReadOnlyList<VariableDeclaration> Variables { get; }

        /// <summary>
        /// An optional source/query expression used to initialize the variables.
        /// </summary>
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
