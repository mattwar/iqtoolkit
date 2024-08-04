// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
using System.Linq.Expressions;

namespace IQToolkit.Expressions.Sql
{
    using Entities.Mapping;

    /// <summary>
    /// A table reference in a SQL query.
    /// </summary>
    public sealed class TableExpression : AliasedExpression
    {
        /// <summary>
        /// The associated <see cref="MappedEntity"/> for the table.
        /// </summary>
        public MappedEntity Entity { get; }

        /// <summary>
        /// The name of the table.
        /// </summary>
        public string Name { get; }

        public TableExpression(TableAlias alias, MappedEntity entity, string name)
            : base(typeof(void), alias)
        {
            this.Entity = entity;
            this.Name = name;
        }

        public override string ToString()
        {
            return "T(" + this.Name + ")";
        }

        public TableExpression Update(
            TableAlias alias,
            MappedEntity entity,
            string name)
        {
            if (alias != this.Alias
                || entity != this.Entity
                || name != this.Name)
            {
                return new TableExpression(alias, entity, name);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitTable(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            return this;
        }
    }
}
