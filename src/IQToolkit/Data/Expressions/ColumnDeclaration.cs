// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A declaration of a column in a SQL SELECT expression
    /// </summary>
    public sealed class ColumnDeclaration
    {
        public string Name { get; }
        public Expression Expression { get; }
        public QueryType QueryType { get; }

        public ColumnDeclaration(string name, Expression expression, QueryType queryType)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (expression == null)
                throw new ArgumentNullException("expression");
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            this.Name = name;
            this.Expression = expression;
            this.QueryType = queryType;
        }

        public ColumnDeclaration Update(
            string name,
            Expression expression,
            QueryType queryType)
        {
            if (name != this.Name
                || expression != this.Expression
                || queryType != this.QueryType)
            {
                return new ColumnDeclaration(name, expression, queryType);
            }
            else
            {
                return this;
            }
        }
    }
}
