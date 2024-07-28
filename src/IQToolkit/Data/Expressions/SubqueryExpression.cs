// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Base class for subquery expressions; scalar, exists and in expressions.
    /// </summary>
    public abstract class SubqueryExpression : DbExpression
    {
        /// <summary>
        /// An optional expression
        /// </summary>
        public SelectExpression Select { get; }

        protected SubqueryExpression(Type type, SelectExpression select)
            : base(type)
        {
            this.Select = select;
        }
    }
}
