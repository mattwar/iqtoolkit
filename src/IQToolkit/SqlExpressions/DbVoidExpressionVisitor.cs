// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    /// <summary>
    /// An extended expression visitor.
    /// Includes <see cref="DbExpression"/> nodes.
    /// </summary>
    public abstract class DbVoidExpressionVisitor : IQToolkit.Expressions.VoidExpressionVisitor
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
                case DbFunctionCallExpression fx:
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
        protected virtual void VisitDbFunctionCall(DbFunctionCallExpression expr) { this.VisitUnhandled(expr); }
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
}