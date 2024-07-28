// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// An extended expression rewriter including custom DbExpression nodes
    /// </summary>
    public abstract class DbExpressionRewriter : ExpressionRewriter
    {
        public override Expression Rewrite(Expression exp)
        {
            switch (exp)
            {
                case AggregateExpression ax:
                    return this.RewriteAggregate(ax);
                case BatchExpression bx:
                    return this.RewriteBatch(bx);
                case BetweenExpression bx:
                    return this.RewriteBetween(bx);
                case BlockCommand bc:
                    return this.RewriteBlockCommand(bc);
                case ClientJoinExpression cjx:
                    return this.RewriteClientJoin(cjx);
                case ClientParameterExpression cp:
                    return this.RewriteClientParameter(cp);
                case ClientProjectionExpression px:
                    return this.RewriteClientProjection(px);
                case ColumnExpression cx:
                    return this.RewriteColumn(cx);
                case DeclarationCommand dc:
                    return this.RewriteDeclarationCommand(dc);
                case DeleteCommand dc:
                    return this.RewriteDeleteCommand(dc);
                case DbBinaryExpression db:
                    return this.RewriteDbBinary(db);
                case FunctionCallExpression fx:
                    return this.RewriteDbFunctionCall(fx);
                case DbPrefixUnaryExpression du:
                    return this.RewriteDbPrefixUnary(du);
                case EntityExpression ex:
                    return this.RewriteEntity(ex);
                case ExistsSubqueryExpression ex:
                    return this.RewriteExistsSubquery(ex);
                case IfCommand ic:
                    return this.RewriteIfCommand(ic);
                case InSubqueryExpression inx:
                    return this.RewriteInSubquery(inx);
                case InValuesExpression inv:
                    return this.RewriteInValues(inv);
                case InsertCommand ic:
                    return this.RewriteInsertCommand(ic);
                case IsNullExpression inx:
                    return this.RewriteIsNull(inx);
                case JoinExpression jx:
                    return this.RewriteJoin(jx);
                case OuterJoinedExpression ojx:
                    return this.RewriteOuterJoined(ojx);
                case RowNumberExpression rnx:
                    return this.RewriteRowNumber(rnx);
                case ScalarSubqueryExpression sx:
                    return this.RewriteScalarSubquery(sx);
                case SelectExpression sx:
                    return this.RewriteSelect(sx);
                case TableExpression tx:
                    return this.RewriteTable(tx);
                case TaggedExpression asx:
                    return this.RewriteTagged(asx);
                case UpdateCommand uc:
                    return this.RewriteUpdateCommand(uc);
                case VariableExpression vx:
                    return this.RewriteVariable(vx);
                default:
                    return base.Rewrite(exp);
            }
        }

        protected virtual Expression RewriteAggregate(AggregateExpression original)
        {
            var argument = this.RewriteN(original.Argument);
            return original.Update(
                original.Type, 
                original.AggregateName, 
                argument, 
                original.IsDistinct
                );
        }

        protected virtual Expression RewriteBatch(BatchExpression original)
        {
            var operation = (LambdaExpression)this.Rewrite(original.Operation);
            var batchSize = this.Rewrite(original.BatchSize);
            var stream = this.Rewrite(original.Stream);
            return original.Update(original.Input, operation, batchSize, stream);
        }

        protected virtual Expression RewriteBetween(BetweenExpression original)
        {
            var expr = this.Rewrite(original.Expression);
            var lower = this.Rewrite(original.Lower);
            var upper = this.Rewrite(original.Upper);
            return original.Update(expr, lower, upper);
        }

        protected virtual Expression RewriteBlockCommand(BlockCommand original)
        {
            var commands = this.RewriteExpressionList(original.Commands);
            return original.Update(commands);
        }

        protected virtual Expression RewriteClientJoin(ClientJoinExpression original)
        {
            var projection = (ClientProjectionExpression)this.Rewrite(original.Projection);
            var outerKey = this.RewriteExpressionList(original.OuterKey);
            var innerKey = this.RewriteExpressionList(original.InnerKey);
            return original.Update(projection, outerKey, innerKey);
        }

        protected virtual Expression RewriteClientParameter(ClientParameterExpression original)
        {
            var value = this.Rewrite(original.Value);
            return original.Update(original.Name, original.QueryType, value);
        }

        protected virtual Expression RewriteClientProjection(ClientProjectionExpression original)
        {
            var select = (SelectExpression)this.Rewrite(original.Select);
            var projector = this.Rewrite(original.Projector);
            return original.Update(select, projector, original.Aggregator);
        }

        protected virtual Expression RewriteColumn(ColumnExpression original)
        {
            return original;
        }

        protected virtual ColumnAssignment VisitColumnAssignment(ColumnAssignment original)
        {
            var column = (ColumnExpression)this.Rewrite(original.Column);
            var expression = this.Rewrite(original.Expression);
            return original.Update(column, expression);
        }

        protected virtual IReadOnlyList<ColumnAssignment> VisitColumnAssignments(
            IReadOnlyList<ColumnAssignment> original) =>
            this.RewriteList(original, VisitColumnAssignment);

        protected virtual ColumnDeclaration VisitColumnDeclaration(ColumnDeclaration original)
        {
            var expression = this.Rewrite(original.Expression);
            return original.Update(original.Name, expression, original.QueryType);
        }

        protected virtual IReadOnlyList<ColumnDeclaration> RewriteColumnDeclarations(
            IReadOnlyList<ColumnDeclaration> original)
        {
            return this.RewriteList(original, VisitColumnDeclaration);
        }

        protected virtual Expression RewriteDeclarationCommand(DeclarationCommand original)
        {
            var variables = this.VisitVariableDeclarations(original.Variables);
            var source = (SelectExpression?)this.RewriteN(original.Source);
            return original.Update(variables, source);
        }

        protected virtual Expression RewriteDeleteCommand(DeleteCommand original)
        {
            var table = (TableExpression)this.Rewrite(original.Table);
            var where = this.RewriteN(original.Where);
            return original.Update(table, where);
        }

        protected virtual Expression RewriteDbBinary(DbBinaryExpression original)
        {
            var left = this.Rewrite(original.Left);
            var right = this.Rewrite(original.Right);
            return original.Update(original.Type, original.IsPredicate, left, original.Operator, right);
        }

        protected virtual Expression RewriteDbFunctionCall(FunctionCallExpression original)
        {
            var arguments = this.RewriteExpressionList(original.Arguments);
            return original.Update(original.Type, original.IsPredicate, original.Name, arguments);
        }

        protected virtual Expression RewriteDbLiteral(DbLiteralExpression original)
        {
            return original;
        }

        protected virtual Expression RewriteDbPrefixUnary(DbPrefixUnaryExpression original)
        {
            var operand = this.Rewrite(original.Operand);
            return original.Update(original.Type, original.IsPredicate, original.Operator, operand);
        }

        protected virtual Expression RewriteEntity(EntityExpression original)
        {
            var exp = this.Rewrite(original.Expression)!;
            return original.Update(original.Entity, exp);
        }

        protected virtual Expression RewriteExistsSubquery(ExistsSubqueryExpression original)
        {
            var select = (SelectExpression)this.Rewrite(original.Select);
            return original.Update(select);
        }

        protected virtual Expression RewriteIfCommand(IfCommand original)
        {
            var check = this.Rewrite(original.Test);
            var ifTrue = this.Rewrite(original.IfTrue);
            var ifFalse = this.RewriteN(original.IfFalse);
            return original.Update(check, ifTrue, ifFalse);
        }

        protected virtual Expression RewriteInSubquery(InSubqueryExpression original)
        {
            var expr = this.Rewrite(original.Expression);
            var select = (SelectExpression?)this.RewriteN(original.Select)!;
            return original.Update(expr, select);
        }

        protected virtual Expression RewriteInValues(InValuesExpression original)
        {
            var expr = this.Rewrite(original.Expression);
            var values = this.RewriteExpressionList(original.Values);
            return original.Update(expr, values);
        }

        protected virtual Expression RewriteInsertCommand(InsertCommand original)
        {
            var table = (TableExpression)this.Rewrite(original.Table);
            var assignments = this.VisitColumnAssignments(original.Assignments);
            return original.Update(table, assignments);
        }

        protected virtual Expression RewriteIsNull(IsNullExpression original)
        {
            var expr = this.Rewrite(original.Expression);
            return original.Update(expr);
        }

        protected virtual Expression RewriteJoin(JoinExpression original)
        {
            var left = this.Rewrite(original.Left);
            var right = this.Rewrite(original.Right);
            var condition = this.RewriteN(original.Condition);
            return original.Update(original.JoinType, left, right, condition);
        }

        protected virtual Expression RewriteOuterJoined(OuterJoinedExpression original)
        {
            var test = this.Rewrite(original.Test);
            var expression = this.Rewrite(original.Expression);
            return original.Update(test, expression);
        }

        protected virtual OrderExpression VisitOrder(OrderExpression original)
        {
            var expression = this.Rewrite(original.Expression);
            return original.Update(original.OrderType, expression);
        }

        protected virtual IReadOnlyList<OrderExpression> VisitOrderExpressions(
            IReadOnlyList<OrderExpression> expressions) =>
            this.RewriteList(expressions, VisitOrder);

        protected virtual Expression RewriteRowNumber(RowNumberExpression original)
        {
            var orderby = this.VisitOrderExpressions(original.OrderBy);
            return original.Update(orderby);
        }

        protected virtual Expression RewriteScalarSubquery(ScalarSubqueryExpression original)
        {
            var select = (SelectExpression)this.Rewrite(original.Select);
            return original.Update(original.Type, select);
        }

        protected virtual Expression RewriteSelect(SelectExpression original)
        {
            var from = this.RewriteN(original.From);
            var where = this.RewriteN(original.Where);
            var orderBy = this.VisitOrderExpressions(original.OrderBy);
            var groupBy = this.RewriteExpressionList(original.GroupBy);
            var skip = this.RewriteN(original.Skip);
            var take = this.RewriteN(original.Take);
            var columns = this.RewriteColumnDeclarations(original.Columns);

            return original.Update(
                original.Alias, 
                from, 
                where, 
                orderBy, 
                groupBy, 
                skip, 
                take, 
                original.IsDistinct, 
                original.IsReverse, 
                columns
                );
        }

        protected virtual Expression RewriteTable(TableExpression original)
        {
            return original;
        }

        protected virtual Expression RewriteTagged(TaggedExpression original)
        {
            var expression = this.Rewrite(original.Expression);
            return original.Update(expression);
        }

        protected virtual Expression RewriteUpdateCommand(UpdateCommand original)
        {
            var table = (TableExpression)this.Rewrite(original.Table);
            var where = this.Rewrite(original.Where);
            var assignments = this.VisitColumnAssignments(original.Assignments);
            return original.Update(table, where, assignments);
        }

        protected virtual Expression RewriteVariable(VariableExpression original)
        {
            return original;
        }

        protected virtual VariableDeclaration VisitVariableDeclaration(VariableDeclaration original)
        {
            var expression = this.Rewrite(original.Expression);
            return original.Update(original.Name, original.QueryType, expression);
        }

        protected virtual IReadOnlyList<VariableDeclaration> VisitVariableDeclarations(
            IReadOnlyList<VariableDeclaration> decls) =>
            this.RewriteList(decls, VisitVariableDeclaration);
    }
}