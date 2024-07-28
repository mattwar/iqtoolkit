// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// An extended expression visitor.
    /// Includes <see cref="DbExpression"/> nodes.
    /// </summary>
    public abstract class DbExpressionVisitor : IQToolkit.Expressions.ExpressionVisitor
    {
        public override void Visit(Expression? exp)
        {
            if (exp == null)
                return;

            switch (exp)
            {
                case AggregateExpression ax:
                    this.VisitAggregate(ax);
                    break;
                case TaggedExpression asx:
                    this.VisitTagged(asx);
                    break;
                case BatchExpression bx:
                    this.VisitBatch(bx);
                    break;
                case BetweenExpression bx:
                    this.VisitBetween(bx);
                    break;
                case BlockCommand bc:
                    this.VisitBlockCommand(bc);
                    break;
                case ClientJoinExpression cjx:
                    this.VisitClientJoin(cjx);
                    break;
                case ClientParameterExpression cp:
                    this.VisitClientParameter(cp);
                    break;
                case ClientProjectionExpression px:
                    this.VisitClientProjection(px);
                    break;
                case ColumnExpression cx:
                    this.VisitColumn(cx);
                    break;
                case DeclarationCommand dc:
                    this.VisitDeclarationCommand(dc);
                    break;
                case DeleteCommand dc:
                    this.VisitDeleteCommand(dc);
                    break;
                case DbBinaryExpression db:
                    this.VisitDbBinary(db);
                    break;
                case FunctionCallExpression fx:
                    this.VisitDbFunctionCall(fx);
                    break;
                case DbLiteralExpression dl:
                    this.VisitDbLiteral(dl);
                    break;
                case DbPrefixUnaryExpression du:
                    this.VisitDbPrefixUnary(du);
                    break;
                case EntityExpression ex:
                    this.VisitEntity(ex);
                    break;
                case ExistsSubqueryExpression ex:
                    this.VisitExistsSubquery(ex);
                    break;
                case IfCommand ic:
                    this.VisitIfCommand(ic);
                    break;
                case InSubqueryExpression inx:
                    this.VisitInSubquery(inx);
                    break;
                case InValuesExpression inv:
                    this.VisitInValues(inv);
                    break;
                case InsertCommand ic:
                    this.VisitInsertCommand(ic);
                    break;
                case IsNullExpression inx:
                    this.VisitIsNull(inx);
                    break;
                case JoinExpression jx:
                    this.VisitJoin(jx);
                    break;
                case OuterJoinedExpression ojx:
                    this.VisitOuterJoined(ojx);
                    break;
                case RowNumberExpression rnx:
                    this.VisitRowNumber(rnx);
                    break;
                case ScalarSubqueryExpression sx:
                    this.VisitScalarSubquery(sx);
                    break;
                case SelectExpression sx:
                    this.VisitSelect(sx);
                    break;
                case TableExpression tx:
                    this.VisitTable(tx);
                    break;
                case UpdateCommand uc:
                    this.VisitUpdateCommand(uc);
                    break;
                case VariableExpression vx:
                    this.VisitVariable(vx);
                    break;
                default:
                    base.Visit(exp);
                    break;
            }
        }

        protected virtual void VisitAggregate(AggregateExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitBatch(BatchExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitBetween(BetweenExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitBlockCommand(BlockCommand expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitClientJoin(ClientJoinExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitClientParameter(ClientParameterExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitClientProjection(ClientProjectionExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitColumn(ColumnExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitDbBinary(DbBinaryExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitDbFunctionCall(FunctionCallExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitDbLiteral(DbLiteralExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitDbPrefixUnary(DbPrefixUnaryExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitDeclarationCommand(DeclarationCommand expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitDeleteCommand(DeleteCommand expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitEntity(EntityExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitExistsSubquery(ExistsSubqueryExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitIfCommand(IfCommand expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitInSubquery(InSubqueryExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitInValues(InValuesExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitInsertCommand(InsertCommand expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitIsNull(IsNullExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitJoin(JoinExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitOuterJoined(OuterJoinedExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitRowNumber(RowNumberExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitSelect(SelectExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitScalarSubquery(ScalarSubqueryExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitTable(TableExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitTagged(TaggedExpression expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitUpdateCommand(UpdateCommand expr) { this.VisitUnhandled(expr); }
        protected virtual void VisitVariable(VariableExpression expr) { this.VisitUnhandled(expr); }
    }

    /// <summary>
    /// An extended expression visitor.
    /// Includes <see cref="DbExpression"/> nodes.
    /// </summary>
    public abstract class DbExpressionVisitor<TResult> : IQToolkit.Expressions.ExpressionVisitor<TResult>
    {
        public override TResult Visit(Expression? exp)
        {
            if (exp == null)
                return default!;

            switch (exp)
            {
                case AggregateExpression ax:
                    return this.VisitAggregate(ax);
                case BatchExpression bx:
                    return this.VisitBatch(bx);
                case BetweenExpression bx:
                    return this.VisitBetween(bx);
                case BlockCommand bc:
                    return this.VisitBlockCommand(bc);
                case ClientJoinExpression cjx:
                    return this.VisitClientJoin(cjx);
                case ClientParameterExpression cp:
                    return this.VisitClientParameter(cp);
                case ClientProjectionExpression px:
                    return this.VisitClientProjection(px);
                case ColumnExpression cx:
                    return this.VisitColumn(cx);
                case DeclarationCommand dc:
                    return this.VisitDeclarationCommand(dc);
                case DeleteCommand dc:
                    return this.VisitDeleteCommand(dc);
                case DbBinaryExpression db:
                    return this.VisitDbBinary(db);
                case FunctionCallExpression fx:
                    return this.VisitDbFunctionCall(fx);
                case DbLiteralExpression dl:
                    return this.VisitDbLiteral(dl);
                case DbPrefixUnaryExpression du:
                    return this.VisitDbPrefixUnary(du);
                case EntityExpression ex:
                    return this.VisitEntity(ex);
                case ExistsSubqueryExpression ex:
                    return this.VisitExistsSubquery(ex);
                case IfCommand ic:
                    return this.VisitIfCommand(ic);
                case InSubqueryExpression inx:
                    return this.VisitInSubquery(inx);
                case InValuesExpression inv:
                    return this.VisitInValues(inv);
                case InsertCommand ic:
                    return this.VisitInsertCommand(ic);
                case IsNullExpression inx:
                    return this.VisitIsNull(inx);
                case JoinExpression jx:
                    return this.VisitJoin(jx);
                case OuterJoinedExpression ojx:
                    return this.VisitOuterJoined(ojx);
                case RowNumberExpression rnx:
                    return this.VisitRowNumber(rnx);
                case ScalarSubqueryExpression sx:
                    return this.VisitScalarSubquery(sx);
                case SelectExpression sx:
                    return this.VisitSelect(sx);
                case TableExpression tx:
                    return this.VisitTable(tx);
                case TaggedExpression asx:
                    return this.VisitTagged(asx);
                case UpdateCommand uc:
                    return this.VisitUpdateCommand(uc);
                case VariableExpression vx:
                    return this.VisitVariable(vx);
                default:
                    return base.Visit(exp);
            }
        }

        protected virtual TResult VisitAggregate(AggregateExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitBatch(BatchExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitBetween(BetweenExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitBlockCommand(BlockCommand expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitClientJoin(ClientJoinExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitClientParameter(ClientParameterExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitClientProjection(ClientProjectionExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitColumn(ColumnExpression expr) => this.VisitUnhandled(expr); 
        protected virtual TResult VisitDbBinary(DbBinaryExpression expr) => this.VisitUnhandled(expr); 
        protected virtual TResult VisitDbFunctionCall(FunctionCallExpression expr) => this.VisitUnhandled(expr); 
        protected virtual TResult VisitDbLiteral(DbLiteralExpression expr) => this.VisitUnhandled(expr); 
        protected virtual TResult VisitDbPrefixUnary(DbPrefixUnaryExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitDeclarationCommand(DeclarationCommand expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitDeleteCommand(DeleteCommand expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitEntity(EntityExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitExistsSubquery(ExistsSubqueryExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitIfCommand(IfCommand expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitInSubquery(InSubqueryExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitInValues(InValuesExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitInsertCommand(InsertCommand expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitIsNull(IsNullExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitJoin(JoinExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitOuterJoined(OuterJoinedExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitRowNumber(RowNumberExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitSelect(SelectExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitScalarSubquery(ScalarSubqueryExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitTable(TableExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitTagged(TaggedExpression expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitUpdateCommand(UpdateCommand expr) => this.VisitUnhandled(expr);
        protected virtual TResult VisitVariable(VariableExpression expr) => this.VisitUnhandled(expr);
    }
}