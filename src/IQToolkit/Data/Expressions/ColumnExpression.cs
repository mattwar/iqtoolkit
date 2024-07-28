// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A custom expression node that represents a reference to a column in a SQL query
    /// </summary>
    public sealed class ColumnExpression : DbExpression, IEquatable<ColumnExpression>
    {
        public TableAlias Alias { get; }
        public string Name { get; }
        public QueryType QueryType { get; }

        public ColumnExpression(Type type, QueryType queryType, TableAlias alias, string name)
            : base(type)
        {
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            if (name == null)
                throw new ArgumentNullException("name");
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
    }
}
