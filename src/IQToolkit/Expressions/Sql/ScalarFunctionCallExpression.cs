// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Expressions.Sql
{
    using Utils;

    /// <summary>
    /// A call to a database query language function.
    /// </summary>
    public sealed class ScalarFunctionCallExpression : ScalarOperation
    {
        public string Name { get; }
        public IReadOnlyList<Expression> Arguments { get; }

        public ScalarFunctionCallExpression(
            Type type, 
            bool isPredicate,
            string name, 
            IEnumerable<Expression>? arguments = null)
            : base(type, isPredicate)
        {
            this.Name = name;
            this.Arguments = arguments.ToReadOnly();
        }

        public ScalarFunctionCallExpression(
            Type type,
            string name,
            IEnumerable<Expression>? arguments = null)
            : this(type, false, name, arguments)
        {
        }

        public ScalarFunctionCallExpression Update(
            Type type, 
            bool isPredicate,
            string name, 
            IEnumerable<Expression> arguments)
        {
            if (type != this.Type
                || isPredicate != this.IsPredicate
                || name != this.Name 
                || arguments != this.Arguments)
            {
                return new ScalarFunctionCallExpression(type, isPredicate, name, arguments);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitScalarFunctionCall(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var arguments = this.Arguments.Rewrite(visitor);
            return this.Update(this.Type, this.IsPredicate, this.Name, arguments);
        }
    }
}
