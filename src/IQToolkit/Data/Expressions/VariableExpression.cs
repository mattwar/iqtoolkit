// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Variables declared in a <see cref="DeclarationCommand"/>.
    /// </summary>
    public sealed class VariableExpression : DbExpression
    {
        public string Name { get; }
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