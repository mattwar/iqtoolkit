// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Expressions
{
    using Sql;
    using Utils;

    /// <summary>
    /// A SQL SELECT expression.
    /// </summary>
    public sealed class SelectExpression : AliasedExpression
    {
        public IReadOnlyList<ColumnDeclaration> Columns { get; }
        public Expression? From { get; }
        public Expression? Where { get; }
        public IReadOnlyList<OrderExpression> OrderBy { get; }
        public IReadOnlyList<Expression> GroupBy { get; }
        public bool IsDistinct { get; }
        public Expression? Skip { get; }
        public Expression? Take { get; }
        public bool IsReverse { get; }

        public SelectExpression(
            TableAlias alias,
            IEnumerable<ColumnDeclaration>? columns,
            Expression? from,
            Expression? where,
            IEnumerable<OrderExpression>? orderBy,
            IEnumerable<Expression>? groupBy,
            bool isDistinct,
            Expression? skip,
            Expression? take,
            bool isReverse
            )
            : base(typeof(void), alias)
        {
            this.Columns = columns.ToReadOnly();
            this.IsDistinct = isDistinct;
            this.From = from;
            this.Where = where;
            this.OrderBy = orderBy.ToReadOnly();
            this.GroupBy = groupBy.ToReadOnly();
            this.Take = take;
            this.Skip = skip;
            this.IsReverse = isReverse;
        }

        public SelectExpression(
            TableAlias alias,
            IEnumerable<ColumnDeclaration>? columns,
            Expression? from,
            Expression? where,
            IEnumerable<OrderExpression>? orderBy,
            IEnumerable<Expression>? groupBy
            )
            : this(alias, columns, from, where, orderBy, groupBy, false, null, null, false)
        {
        }

        public SelectExpression(
            TableAlias alias, 
            IEnumerable<ColumnDeclaration>? columns,
            Expression? from, 
            Expression? where
            )
            : this(alias, columns, from, where, null, null)
        {
        }

        public SelectExpression(
            TableAlias alias,
            IEnumerable<ColumnDeclaration>? columns,
            Expression? from
            )
            : this(alias, columns, from, null, null, null)
        {
        }

        public override DbExpressionType DbNodeType =>
            DbExpressionType.Select;

        public string QueryText => 
            SqlFormatter.Default.Format(this, FormattingOptions.DebugDefault).Text;

        public SelectExpression Update(
            TableAlias alias,
            Expression? from,
            Expression? where,
            IEnumerable<OrderExpression>? orderBy,
            IEnumerable<Expression>? groupBy,
            Expression? skip,
            Expression? take,
            bool isDistinct,
            bool isReverse,
            IEnumerable<ColumnDeclaration> columns
            )
        {
            if (alias != this.Alias
                || from != this.From
                || where != this.Where
                || orderBy != this.OrderBy
                || groupBy != this.GroupBy
                || take != this.Take
                || skip != this.Skip
                || isDistinct != this.IsDistinct
                || columns != this.Columns
                || isReverse != this.IsReverse
                )
            {
                return new SelectExpression(
                    alias, 
                    columns, 
                    from, 
                    where, 
                    orderBy, 
                    groupBy, 
                    isDistinct, 
                    skip, 
                    take, 
                    isReverse
                    );
            }
            else
            {
                return this;
            }
        }

        public SelectExpression WithColumns(
            IEnumerable<ColumnDeclaration> columns)
        {
            return Update(
                this.Alias, 
                this.From, 
                this.Where, 
                this.OrderBy, 
                this.GroupBy,
                this.Skip, 
                this.Take, 
                this.IsDistinct, 
                this.IsReverse, 
                columns
                );
        }

        public SelectExpression AddColumn(
            ColumnDeclaration column)
        {
            return this.WithColumns(this.Columns.Add(column));
        }

        public SelectExpression RemoveColumn(
            ColumnDeclaration column)
        {
            return this.WithColumns(this.Columns.Remove(column));
        }


        public SelectExpression WithIsDistinct(
            bool isDistinct)
        {
            return Update(
                this.Alias,
                this.From,
                this.Where,
                this.OrderBy,
                this.GroupBy,
                this.Skip,
                this.Take,
                isDistinct,
                this.IsReverse,
                this.Columns
                );
        }

        public SelectExpression WithIsReverse(bool isReverse)
        {
            return Update(
                this.Alias,
                this.From,
                this.Where,
                this.OrderBy,
                this.GroupBy,
                this.Skip,
                this.Take,
                this.IsDistinct,
                isReverse,
                this.Columns
                );
        }

        public SelectExpression WithWhere(Expression? where)
        {
            return Update(
                this.Alias,
                this.From,
                where,
                this.OrderBy,
                this.GroupBy,
                this.Skip,
                this.Take,
                this.IsDistinct,
                this.IsReverse,
                this.Columns
                );
        }

        public SelectExpression WithOrderBy(IEnumerable<OrderExpression>? orderBy)
        {
            return Update(
                this.Alias,
                this.From,
                this.Where,
                orderBy,
                this.GroupBy,
                this.Skip,
                this.Take,
                this.IsDistinct,
                this.IsReverse,
                this.Columns
                );
        }

        public SelectExpression AddOrderByExpression(OrderExpression ordering)
        {
            return this.WithOrderBy(this.OrderBy.Add(ordering));
        }

        public SelectExpression RemoveOrderByExpression(OrderExpression ordering)
        {
            return this.WithOrderBy(this.OrderBy.Remove(ordering));
        }

        public SelectExpression WithGroupBy(IEnumerable<Expression>? groupBy)
        {
            return Update(
                this.Alias,
                this.From,
                this.Where,
                this.OrderBy,
                groupBy,
                this.Skip,
                this.Take,
                this.IsDistinct,
                this.IsReverse,
                this.Columns
                );
        }

        public SelectExpression AddGroupByExpression(Expression expression)
        {
            return this.WithGroupBy(this.GroupBy.Add(expression));
        }

        public SelectExpression RemoveGroupByExpression(Expression expression)
        {
            return this.WithGroupBy(this.GroupBy.Remove(expression));
        }

        public SelectExpression WithSkip(Expression? skip)
        {
            return Update(
                this.Alias,
                this.From,
                this.Where,
                this.OrderBy,
                this.GroupBy,
                skip,
                this.Take,
                this.IsDistinct,
                this.IsReverse,
                this.Columns
                );
        }

        public SelectExpression WithTake(Expression? take)
        {
            return Update(
                this.Alias,
                this.From,
                this.Where,
                this.OrderBy,
                this.GroupBy,
                this.Skip,
                take,
                this.IsDistinct,
                this.IsReverse,
                this.Columns
                );
        }

        public SelectExpression WithFrom(Expression? from)
        {
            return Update(
                this.Alias,
                from,
                this.Where,
                this.OrderBy,
                this.GroupBy,
                this.Skip,
                this.Take,
                this.IsDistinct,
                this.IsReverse,
                this.Columns
                );
        }

        public SelectExpression WithAlias(TableAlias alias)
        {
            return Update(
                alias,
                this.From,
                this.Where,
                this.OrderBy,
                this.GroupBy,
                this.Skip,
                this.Take,
                this.IsDistinct,
                this.IsReverse,
                this.Columns
                );
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitSelect(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var from = visitor.Visit(this.From);
            var where = visitor.Visit(this.Where);
            var orderBy = this.OrderBy.Rewrite(v => v.Accept(visitor));
            var groupBy = this.GroupBy.Rewrite(visitor);
            var skip = visitor.Visit(this.Skip);
            var take = visitor.Visit(this.Take);
            var columns = this.Columns.Rewrite(cd => cd.Accept(visitor));

            return this.Update(
                this.Alias,
                from,
                where,
                orderBy,
                groupBy,
                skip,
                take,
                this.IsDistinct,
                this.IsReverse,
                columns
                );
        }
    }
}
