// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Expressions
{
    using Utils;

    /// <summary>
    /// A call to a database query language function.
    /// </summary>
    public sealed class DbFunctionCallExpression : DbOperation
    {
        public string Name { get; }
        public IReadOnlyList<Expression> Arguments { get; }

        public DbFunctionCallExpression(
            Type type, 
            bool isPredicate,
            string name, 
            IEnumerable<Expression>? arguments = null)
            : base(type, isPredicate)
        {
            this.Name = name;
            this.Arguments = arguments.ToReadOnly();
        }

        public DbFunctionCallExpression(
            Type type,
            string name,
            IEnumerable<Expression>? arguments = null)
            : this(type, false, name, arguments)
        {
        }


        public override DbExpressionType DbNodeType =>
            DbExpressionType.Function;

        public DbFunctionCallExpression Update(
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
                return new DbFunctionCallExpression(type, isPredicate, name, arguments);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitDbFunctionCall(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var arguments = this.Arguments.Rewrite(visitor);
            return this.Update(this.Type, this.IsPredicate, this.Name, arguments);
        }
    }
}
