// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// An extended <see cref="ExpressionVisitor"/> for <see cref="SqlExpression"/> nodes.
    /// </summary>
    public abstract class SqlExpressionVisitor : ExpressionVisitor
    {
        protected internal virtual Expression VisitAggregate(AggregateExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitBatch(BatchExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitBetween(BetweenExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitBlockCommand(BlockCommand original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitClientJoin(ClientJoinExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitClientParameter(ClientParameterExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitClientProjection(ClientProjectionExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitColumn(ColumnExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual ColumnAssignment VisitColumnAssignment(ColumnAssignment original)
        {
            return original.VisitChildren(this);
        }

        protected internal virtual ColumnDeclaration VisitColumnDeclaration(ColumnDeclaration original)
        {
            return original.VisitChildren(this);
        }

        protected internal virtual Expression VisitDeclarationCommand(DeclarationCommand original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitDeleteCommand(DeleteCommand original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitEntity(EntityExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitExistsSubquery(ExistsSubqueryExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitIfCommand(IfCommand original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitInsertCommand(InsertCommand original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitInSubquery(InSubqueryExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitInValues(InValuesExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitIsNull(IsNullExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitLiteral(LiteralExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitJoin(JoinExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual OrderExpression VisitOrder(OrderExpression original)
        {
            return original.VisitChildren(this);
        }

        protected internal virtual Expression VisitOuterJoined(OuterJoinedExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitRowNumber(RowNumberExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitScalarBinary(ScalarBinaryExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitScalarFunctionCall(ScalarFunctionCallExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitScalarPrefixUnary(ScalarPrefixUnaryExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitScalarSubquery(ScalarSubqueryExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitSelect(SelectExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitTable(TableExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitTagged(TaggedExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitUpdateCommand(UpdateCommand original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual Expression VisitVariable(VariableExpression original)
        {
            return this.VisitExtension(original);
        }

        protected internal virtual VariableDeclaration VisitVariableDeclaration(VariableDeclaration original)
        {
            return original.VisitChildren(this);
        }
    }
}