// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Expressions.Sql
{
    /// <summary>
    /// A reference to a variable declared in a <see cref="DeclarationCommand"/>.
    /// </summary>
    public sealed class VariableExpression : SqlExpression
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

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitVariable(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            return this;
        }
    }
}