// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name}: {this.DbNodeType}")]
    public abstract class DbExpression : Expression 
    {
        protected string DebugText =>
            $"{DbExpressionDebugFormatter.Singleton.Format(this)}";

        public override Type Type { get; }

        protected DbExpression(Type type)
        {
            this.Type = type;
        }

        public abstract DbExpressionType DbNodeType { get; }

        public override ExpressionType NodeType =>
            (ExpressionType)(int)this.DbNodeType;

        public virtual bool IsPredicate => false;
    }
}
