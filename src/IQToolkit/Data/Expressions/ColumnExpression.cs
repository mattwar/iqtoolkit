// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A reference to a column in a query
    /// </summary>
    public sealed class ColumnExpression : DbExpression, IEquatable<ColumnExpression>
    {
        /// <summary>
        /// The alias of the table that the column belongs to.
        /// </summary>
        public TableAlias Alias { get; }

        /// <summary>
        /// The name of the column.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The database type of the column.
        /// </summary>
        public QueryType QueryType { get; }

        public ColumnExpression(Type type, QueryType queryType, TableAlias alias, string name)
            : base(type)
        {
            this.Alias = alias;
            this.Name = name;
            this.QueryType = queryType;
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Column;

        public override string ToString() =>
            this.Alias.ToString() + ".Column(" + this.Name + ")";

        public override int GetHashCode() =>
            this.Alias.GetHashCode() + this.Name.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is ColumnExpression cx && Equals(cx);

        public bool Equals(ColumnExpression other) =>
            ((object)this) == (object)other
            || (this.Alias == other.Alias && this.Name == other.Name);

        public ColumnExpression Update(
            Type type,
            QueryType queryType,
            TableAlias alias,
            string name)
        {
            if (type != this.Type
                || queryType != this.QueryType
                || alias != this.Alias
                || name != this.Name)
            {
                return new ColumnExpression(type, queryType, alias, name);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitColumn(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            return this;
        }
    }
}
