// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Designates an expression/value as a client parameter.
    /// </summary>
    public sealed class ClientParameterExpression : DbExpression
    {
        public string Name { get; }
        public QueryType QueryType { get; }
        public Expression Value { get; }

        public ClientParameterExpression(string name, QueryType queryType, Expression value)
            : base(value.Type)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            if (value == null)
                throw new ArgumentNullException("value");
            this.Name = name;
            this.QueryType = queryType;
            this.Value = value;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.ClientParameter;

        public ClientParameterExpression Update(
            string name,
            QueryType queryType,
            Expression value)
        {
            if (name != this.Name
                || queryType != this.QueryType
                || value != this.Value)
            {
                return new ClientParameterExpression(name, queryType, value);
            }
            else
            {
                return this;
            }
        }
    }
}
