// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A reference to a variable declared in a <see cref="DeclarationCommand"/>.
    /// </summary>
    public sealed class VariableExpression : DbExpression
    {
        /// <summary>
        /// The name of the variable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The database type of the variable.
        /// </summary>
        public QueryType QueryType { get; }

        public VariableExpression(string name, Type type, QueryType queryType)
            : base(type)
        {
            this.Name = name;
            this.QueryType = queryType;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Variable;

        public VariableExpression Update(string name, Type type, QueryType queryType)
        {
            if (name != this.Name
                || type != this.Type
                || queryType != this.QueryType)
            {
                return new VariableExpression(name, type, queryType);
            }
            else
            {
                return this;
            }
        }
    }
}