// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    public sealed class VariableDeclaration
    {
        public string Name { get; }
        public QueryType QueryType { get; }
        public Expression Expression { get; }

        public VariableDeclaration(string name, QueryType queryType, Expression expression)
        {
            this.Name = name;
            this.QueryType = queryType;
            this.Expression = expression;
        }

        public VariableDeclaration Update(
            string name,
            QueryType queryType,
            Expression expression)
        {
            if (name != this.Name
                || queryType != this.QueryType
                || expression != this.Expression)
            {
                return new VariableDeclaration(name, queryType, expression);
            }
            else
            {
                return this;
            }
        }
    }
}