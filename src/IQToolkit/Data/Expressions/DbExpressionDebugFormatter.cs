// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Expressions
{
    using AnsiSql;

    /// <summary>
    /// Formats an expressions tree into pseudo language syntax.
    /// Useful debug visualization purposes.
    /// </summary>
    public class DbExpressionDebugFormatter : ExpressionFormatter
    {
        public DbExpressionDebugFormatter()
        {
        }

        public static readonly ExpressionFormatter Singleton =
            new DbExpressionDebugFormatter();

        public override void Format(Expression expression, TextWriter writer, string indentation = "  ")
        {
            try
            {
                new Writer(writer, indentation).Write(expression);
            }
            catch (Exception e)
            {
                writer.Write($"Error: {e.Message}");
            }
        }

        public class Writer : ExpressionDebugFormatter.Writer
        {
            private ImmutableDictionary<TableAlias, AliasedExpression> _aliasMap;

            public Writer(TextWriter writer, string indentation)
                : base(writer, indentation)
            {
                _aliasMap = ImmutableDictionary<TableAlias, AliasedExpression>.Empty;
            }

            protected void DeclareAlias(AliasedExpression expr)
            {
                if (!_aliasMap.TryGetValue(expr.Alias, out var aliasedExpr))
                {
                    _aliasMap = _aliasMap.Add(expr.Alias, expr);
                }
            }

            private void DeclareAliases(Expression? context)
            {
                switch (context)
                {
                    case AliasedExpression aliased:
                        this.DeclareAlias(aliased);
                        break;

                    case JoinExpression join:
                        this.DeclareAliases(join.Left);
                        this.DeclareAliases(join.Right);
                        break;
                }
            }

            protected string GetReferencedAliasName(TableAlias alias)
            {
                return _aliasMap.TryGetValue(alias, out _)
                    ? $"A{alias.SequenceId}"
                    : $"??A{alias.SequenceId}??"; // not declared
            }

            protected string GetDeclaredAliasName(TableAlias alias)
            {
                return _aliasMap.TryGetValue(alias, out _)
                    ? $"++A{alias.SequenceId}++"  // already declared
                    : $"A{alias.SequenceId}";
            }

            private string FormatQuery(Expression query)
            {
                return AnsiSqlFormatter.Default.Format(query, FormattingOptions.DebugDefault).Text;
            }

            public override void Write(Expression exp)
            {
                var oldAliasMap = _aliasMap;

                switch (exp)
                {
                    case AggregateExpression agx:
                        this.WriteAggregate(agx);
                        break;
                    case BatchExpression bx:
                        this.WriteBatch(bx);
                        break;
                    case BetweenExpression btx:
                        this.WriteBetween(btx);
                        break;
                    case BlockCommand blc:
                        this.WriteBlockCommand(blc);
                        break;
                    case ClientJoinExpression cjx:
                        this.WriteClientJoin(cjx);
                        break;
                    case ClientParameterExpression cpx:
                        this.WriteClientParameter(cpx);
                        break;
                    case ClientProjectionExpression px:
                        this.WriteClientProjection(px);
                        break;
                    case ColumnExpression cx:
                        this.WriteColumn(cx);
                        break;
                    case DbBinaryExpression dbb:
                        goto default;
                    case DbFunctionCallExpression fx:
                        this.WriteFunction(fx);
                        break;
                    case DbLiteralExpression dbl:
                        goto default;
                    case DbPrefixUnaryExpression dbp:
                        break;
                    case DeclarationCommand decc:
                        this.WriteDeclarationCommand(decc);
                        break;
                    case DeleteCommand delc:
                        this.WriteDeleteCommand(delc);
                        break;
                    case EntityExpression ex:
                        this.WriteEntity(ex);
                        break;
                    case ExistsSubqueryExpression esx:
                        this.WriteExists(esx);
                        break;
                    case IfCommand ifc:
                        goto default;
                    case InSubqueryExpression ins:
                        this.WriteInSubquery(ins);
                        break;
                    case InValuesExpression inv:
                        this.WriteInValues(inv);
                        break;
                    case InsertCommand insc:
                        goto default;
                    case IsNullExpression isnx:
                        this.WriteIsNull(isnx);
                        break;
                    case JoinExpression jx:
                        this.WriteJoin(jx);
                        break;
                    case OuterJoinedExpression oj:
                        this.WriteOuterJoined(oj);
                        break;
                    case RowNumberExpression rnx:
                        goto default;
                    case ScalarSubqueryExpression scsx:
                        this.WriteScalarSubquery(scsx);
                        break;
                    case SelectExpression sx:
                        this.WriteSelect(sx);
                        break;
                    case TableExpression tx:
                        this.WriteTable(tx);
                        break;
                    case TaggedExpression agsx:
                        this.WriteTagged(agsx);
                        break;
                    case UpdateCommand upc:
                        goto default;
                    case VariableExpression vx:
                        this.WriteVariable(vx);
                        break;
                    default:
                        if (exp is DbExpression)
                        {
                            this.Write($"Unhandled({exp.GetType().Name})");
                        }
                        else
                        {
                            base.Write(exp);
                        }
                        break;
                }

                _aliasMap = oldAliasMap;
            }

            protected override bool RequiresParenthesis(Expression ex)
            {
                switch (ex)
                {
                    case VariableExpression _:
                    case DbFunctionCallExpression _:
                    case ColumnExpression _:
                    case TableExpression _:
                    case JoinExpression _:
                        return false;

                    default:
                        return base.RequiresParenthesis(ex);
                }
            }

            protected virtual void WriteAggregate(AggregateExpression agx)
            {
                this.Write($"{agx.AggregateName}(");
                if (agx.IsDistinct)
                    this.Write("DISTINCT ");
                if (agx.Argument != null)
                    this.Write(agx.Argument);
                this.Write(")");
            }

            protected virtual void WriteBatch(BatchExpression batch)
            {
                this.Write("Batch(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write("Input: ");
                    this.WriteIndented(() =>
                        this.Write(batch.Input));

                    this.WriteLine();
                    this.Write("Operation: ");
                    this.WriteIndented(() =>
                        this.Write(batch.Operation));

                    this.WriteLine();
                    this.Write("BatchSize: ");
                    this.WriteIndented(() =>
                        this.Write(batch.BatchSize));

                    this.WriteLine();
                    this.Write("Stream: ");
                    this.WriteIndented(() =>
                        this.Write(batch.Stream));

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteBetween(BetweenExpression between)
            {
                this.Write("Between(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write("Expression: ");
                    this.WriteIndented(() =>
                        this.Write(between.Expression));

                    this.WriteLine();
                    this.Write("Lower: ");
                    this.WriteIndented(() =>
                        this.Write(between.Lower));

                    this.WriteLine();
                    this.Write("Upper: ");
                    this.WriteIndented(() =>
                        this.Write(between.Upper));

                    this.WriteLine();
                    this.Write(")");
                });
            }   

            protected virtual void WriteBlockCommand(BlockCommand block)
            {
                this.Write("BlockCommand(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteLineSeparated(block.Commands, this.Write);

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteClientJoin(ClientJoinExpression join)
            {
                this.Write("ClientJoin(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write("OuterKey: ");
                    this.WriteIndented(() =>
                        this.WriteExpressionList(join.OuterKey));

                    this.WriteLine();
                    this.Write("Projection: ");
                    this.WriteIndented(() =>
                        this.Write(join.Projection));

                    // declare alias from projection to be visible in inner key
                    this.DeclareAlias(join.Projection.Select);

                    this.WriteLine();
                    this.Write("InnerKey: ");
                    this.WriteIndented(() =>
                        this.WriteExpressionList(join.InnerKey));

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteClientParameter(ClientParameterExpression parameter)
            {
                this.Write($"{parameter.Name}(");
                this.Write(parameter.Value);
                this.Write(")");
            }

            protected virtual void WriteClientProjection(ClientProjectionExpression projection)
            {
                // add early
                this.Write("Project(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write("Select: ");
                    this.WriteIndented(() =>
                        this.Write(projection.Select));

                    this.DeclareAlias(projection.Select);

                    this.WriteLine();
                    this.Write("Projector: ");
                    this.WriteIndented(() =>
                        this.Write(projection.Projector));

                    if (projection.Aggregator != null)
                    {
                        this.WriteLine();
                        this.WriteLine("Aggregator: ");
                        this.WriteIndented(() =>
                            this.Write(projection.Aggregator));
                    }

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteColumn(ColumnExpression column)
            {
                string aliasName;

                if (_aliasMap.TryGetValue(column.Alias, out var aliased))
                {
                    aliasName = this.GetReferencedAliasName(column.Alias);

                    if (aliased is SelectExpression select)
                    {
                        // verify the column is actual specified in the select
                        var decl = select.Columns.FirstOrDefault(c => c.Name == column.Name);
                        if (decl != null)
                        {
                            this.Write($"Column({aliasName}, \"{column.Name}\")");
                        }
                        else
                        {
                            // column not known
                            this.Write($"Column({aliasName}, ??\"{column.Name}\"??)");
                        }
                    }
                    else
                    {
                        // table's don't have to specify the columns
                        this.Write($"Column({aliasName}, \"{column.Name}\")");
                    }
                }
                else
                {
                    // alias name not currently declared.
                    aliasName = this.GetReferencedAliasName(column.Alias); // already has ??
                    this.Write($"Column({aliasName}, ??\"{column.Name}\"??)");
                }
            }

            protected virtual void WriteColumnDeclaration(ColumnDeclaration column)
            {
                this.Write($"{column.Name} = ");
                this.Write(column.Expression);
            }

            protected virtual void WriteDeclarationCommand(DeclarationCommand decl)
            {
                this.WriteLine();
                this.Write("Declare(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteLine("Variables: ");
                    this.WriteLine();
                    this.WriteIndented(() =>
                    {
                        this.WriteLineSeparated(decl.Variables, this.WriteVariableDeclaration);
                    });

                    if (decl.Source != null)
                    {
                        this.WriteLine();
                        this.Write("Source: ");
                        this.WriteIndented(() =>
                            this.Write(decl.Source));
                    }

                    this.WriteLine();
                    this.Write(")");
                });

                this.WriteLine();
            }

            protected virtual void WriteDeleteCommand(DeleteCommand delete)
            {
                this.WriteLine();
                this.Write("DeleteCommand(");
                this.WriteIndented(() =>
                {
                    this.WriteLine("Table: ");
                    this.Write(delete.Table);

                    if (delete.Where != null)
                    {
                        this.WriteLine();
                        this.Write("Where: ");
                        this.Write(delete.Where);
                    }

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteEntity(EntityExpression entity)
            {
                this.WriteLine("Entity(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write(entity.Expression);

                    this.WriteLine();
                    this.WriteLine(")");
                });
            }

            protected virtual void WriteExists(ExistsSubqueryExpression exists)
            {
                this.WriteLine("Exists(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write(exists.Select);

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteFunction(DbFunctionCallExpression function)
            {
                this.Write(function.Name);
                if (function.Arguments.Count > 0)
                {
                    this.Write("(");
                    this.WriteIndented(() =>
                    {
                        this.WriteExpressionList(function.Arguments);
                        this.Write(")");
                    });
                }
            }

            protected virtual void WriteInSubquery(InSubqueryExpression insub)
            {
                this.Write(insub.Expression);
                this.Write(" IN (");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write(insub.Select);

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteInValues(InValuesExpression invalues)
            {
                this.Write(invalues.Expression);
                this.Write(" IN (");
                this.WriteCommaSeparated(invalues.Values, this.Write);
                this.Write(")");
            }

            protected virtual void WriteIsNull(IsNullExpression isNull)
            {
                this.Write("IsNull(");
                this.WriteIndented(() =>
                    this.Write(isNull.Expression));
                this.Write(")");
            }

            protected virtual void WriteJoin(JoinExpression join)
            {
                this.WriteLine($"{join.JoinType.ToString()}(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write("Left: ");
                    this.WriteIndented(() =>
                        this.Write(join.Left));

                    this.WriteLine();
                    this.Write("Right: ");
                    this.WriteIndented(() =>
                        this.Write(join.Right));

                    if (join.Condition != null)
                    {
                        this.WriteLine();
                        this.Write("On: ");
                        this.WriteIndented(() =>
                            this.Write(join.Condition));
                    }

                    this.WriteLine();
                    this.WriteLine(")");
                });
            }

            protected virtual void WriteOrderExpression(OrderExpression order)
            {
                this.Write(order.Expression);
                if (order.OrderType == OrderType.Descending)
                    this.Write(" DESC");
            }

            protected virtual void WriteOuterJoined(OuterJoinedExpression outer)
            {
                this.Write("OuterJoined(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write("Test: ");
                    this.Write(outer.Test);

                    this.WriteLine();
                    this.Write("Expression: ");
                    this.Write(outer.Expression);

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteScalarSubquery(ScalarSubqueryExpression scalar)
            {
                this.Write("ScalarSubquery(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write(scalar.Select);

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteSelect(SelectExpression select)
            {
                this.WriteLine("Select(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    var name = this.GetDeclaredAliasName(select.Alias);
                    this.Write($"Alias: {name}");

                    if (select.From != null)
                    {
                        this.WriteLine();
                        this.Write($"From: ");
                        this.WriteLine();
                        this.WriteIndented(() =>
                            this.Write(select.From));
                    }

                    this.DeclareAliases(select.From);

                    if (select.IsDistinct)
                    {
                        this.WriteLine();
                        this.WriteLine("IsDistinct: true");
                    }

                    if (select.IsReverse)
                    {
                        this.WriteLine();
                        this.WriteLine("IsReverse: true");
                    }

                    if (select.Skip != null)
                    {
                        this.WriteLine();
                        this.WriteLine("Skip: ");
                        this.Write(select.Skip);
                    }

                    if (select.Take != null)
                    {
                        this.WriteLine();
                        this.WriteLine("Take: ");
                        this.Write(select.Take);
                    }

                    if (select.Where != null)
                    {
                        this.WriteLine();
                        this.Write("Where: ");
                        this.WriteIndented(() =>
                            this.Write(select.Where));
                    }

                    if (select.OrderBy.Count > 0)
                    {
                        this.WriteLine();
                        this.Write("OrderBy: ");
                        this.WriteIndented(() =>
                            this.WriteCommaSeparated(select.OrderBy, this.WriteOrderExpression));
                    }

                    if (select.GroupBy.Count > 0)
                    {
                        this.WriteLine();
                        this.Write("GroupBy: ");
                        this.WriteIndented(() =>
                            this.WriteCommaSeparated(select.GroupBy, this.Write));
                    }

                    this.WriteLine();
                    this.Write("Columns: ");
                    this.WriteIndented(() =>
                        this.WriteCommaSeparated(select.Columns, this.WriteColumnDeclaration));

                    this.WriteLine();
                    this.WriteLine(")");
                });
            }

            protected virtual void WriteTable(TableExpression tx)
            {
                var aliasName = this.GetDeclaredAliasName(tx.Alias);
                this.Write($"Table({aliasName}, \"{tx.Name}\")");
            }

            protected virtual void WriteTagged(TaggedExpression tagged)
            {
                this.Write("Tagged(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteLine($"Id: {tagged.Id}");

                    this.WriteLine();
                    this.Write("Expression: ");
                    this.WriteIndented(() =>
                        this.Write(tagged.Expression));

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected virtual void WriteVariable(VariableExpression vex)
            {
                this.Write($"Variable({vex.Name})");
            }

            protected virtual void WriteVariableDeclaration(VariableDeclaration decl)
            {
                this.Write($"{decl.Name} = ");
                this.Write(decl.Expression);
            }
        }
    }
}
 