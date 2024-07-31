// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// An individual variable declaration used by the <see cref="DeclarationCommand"/>.
    /// </summary>
    public sealed class VariableDeclaration
    {
        /// <summary>
        /// The name of the variable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The database type of the variable.
        /// </summary>
        public QueryType QueryType { get; }

        /// <summary>
        /// The variable's initial value expression.
        /// </summary>
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

        internal VariableDeclaration Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitVariableDeclaration(this);
            return this.VisitChildren(visitor);
        }

        internal VariableDeclaration VisitChildren(ExpressionVisitor visitor)
        {
            var expression = visitor.Visit(this.Expression);
            return this.Update(this.Name, this.QueryType, expression);
        }
    }
}