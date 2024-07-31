// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Expressions.Sql
{
    /// <summary>
    /// Designates an expression to be evaluated on the client and sent as a parameter to the query.
    /// </summary>
    public sealed class ClientParameterExpression : SqlExpression
    {
        public string Name { get; }
        public QueryType QueryType { get; }
        public Expression Value { get; }

        public ClientParameterExpression(string name, QueryType queryType, Expression value)
            : base(value.Type)
        {
            this.Name = name;
            this.QueryType = queryType;
            this.Value = value;
        }

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

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitClientParameter(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var value = visitor.Visit(this.Value);
            return this.Update(this.Name, this.QueryType, value);
        }
    }
}
