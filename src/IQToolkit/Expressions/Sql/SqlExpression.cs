// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Expressions.Sql
{
    /// <summary>
    /// The base type of all SQL expression nodes.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name}: {this.DbNodeType}")]
    public abstract class SqlExpression : Expression 
    {
        protected string DebugText =>
            $"{SqlExpressionDebugFormatter.Singleton.Format(this)}";

        public override Type Type { get; }

        protected SqlExpression(Type type)
        {
            this.Type = type;
        }

        public virtual bool IsPredicate => false;

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            return this.VisitChildren(visitor);
        }

        // Sql expressions are all extension nodes.
        public override ExpressionType NodeType => ExpressionType.Extension;
    }
}
