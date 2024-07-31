// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

namespace IQToolkit.TSql
{
    using Entities;
    using Expressions;
    using Expressions.Sql;

    /// <summary>
    /// Microsoft Transact SQL (TSQL) <see cref="QueryFormatter"/>
    /// </summary>
    public class TSqlFormatter : QueryFormatter
    {
        protected TSqlFormatter()
        {
        }

        public static readonly TSqlFormatter Singleton =
            new TSqlFormatter();

        public override FormattedQuery Format(Expression expression, FormattingOptions? options = null)
        {
            var writer = new StringWriter();
            var parameters = new List<Expression>();
            var diagnostics = new List<Diagnostic>();
            var visitor = new TSqlFormatterVisitor(options, writer, parameters, diagnostics);
            visitor.Visit(expression);
            return new FormattedQuery(
                writer.ToString(),
                parameters,
                diagnostics
                );
        }

        public class TSqlFormatterVisitor : AnsiSql.AnsiSqlFormatter.SqlFormatterVisitor
        {
            public TSqlFormatterVisitor(
                FormattingOptions? options,
                StringWriter writer,
                List<Expression> parameters,
                List<Diagnostic> diagnostics)
                : base(options, TSqlLanguage.Singleton, writer, parameters, diagnostics)
            {
            }

            public new TSqlLanguage Language => 
                TSqlLanguage.Singleton;

            protected override void WriteAggregateName(string aggregateName)
            {
                if (aggregateName == "LongCount")
                {
                    this.Write("COUNT_BIG");
                }
                else
                {
                    base.WriteAggregateName(aggregateName);
                }
            }

            protected override void VisitMember(MemberExpression m)
            {
                if (m.Member.DeclaringType == typeof(string))
                {
                    switch (m.Member.Name)
                    {
                        case "Length":
                            this.Write("LEN(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                    }
                }
                else if (m.Member.DeclaringType == typeof(DateTime) || m.Member.DeclaringType == typeof(DateTimeOffset))
                {
                    switch (m.Member.Name)
                    {
                        case "Day":
                            this.Write("DAY(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Month":
                            this.Write("MONTH(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Year":
                            this.Write("YEAR(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Hour":
                            this.Write("DATEPART(hour, ");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Minute":
                            this.Write("DATEPART(minute, ");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Second":
                            this.Write("DATEPART(second, ");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Millisecond":
                            this.Write("DATEPART(millisecond, ");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "DayOfWeek":
                            this.Write("(DATEPART(weekday, ");
                            this.WriteValue(m.Expression);
                            this.Write(") - 1)");
                            return;
                        case "DayOfYear":
                            this.Write("(DATEPART(dayofyear, ");
                            this.WriteValue(m.Expression);
                            this.Write(") - 1)");
                            return;
                    }
                }

                base.VisitMember(m);
            }

            protected override void VisitMethodCall(MethodCallExpression m)
            {
                if (m.Method.DeclaringType == typeof(string))
                {
                    switch (m.Method.Name)
                    {
                        case "StartsWith":
                            this.WriteValueOperand(m.Object);
                            this.Write(" LIKE ");
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write(" + '%'");
                            return;
                        case "EndsWith":
                            this.WriteValueOperand(m.Object);
                            this.Write(" LIKE '%' + ");
                            this.WriteValueOperand(m.Arguments[0]);
                            return;
                        case "Contains":
                            this.WriteValueOperand(m.Object);
                            this.Write(" LIKE '%' + ");
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write(" + '%'");
                            return;
                        case "Concat":
                            IReadOnlyList<Expression> args = m.Arguments;
                            if (args.Count == 1 && args[0] is NewArrayExpression newArray)
                                args = newArray.Expressions;
                            WriteSeparated(args, " + ");
                            return;
                        case "IsNullOrEmpty":
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write(" IS NULL OR ");
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write(" = ''");
                            return;
                        case "ToUpper":
                            this.Write("UPPER(");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                        case "ToLower":
                            this.Write("LOWER(");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                        case "Replace":
                            this.Write("REPLACE(");
                            this.WriteValue(m.Object);
                            this.Write(", ");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(", ");
                            this.WriteValue(m.Arguments[1]);
                            this.Write(")");
                            return;
                        case "Substring":
                            this.Write("SUBSTRING(");
                            this.WriteValue(m.Object);
                            this.Write(", ");
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write(" + 1, ");
                            if (m.Arguments.Count == 2)
                            {
                                this.Visit(m.Arguments[1]);
                            }
                            else
                            {
                                this.Write("8000");
                            }
                            this.Write(")");
                            return;
                        case "Remove":
                            this.Write("STUFF(");
                            this.WriteValue(m.Object);
                            this.Write(", ");
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write(" + 1, ");
                            if (m.Arguments.Count == 2)
                            {
                                this.Visit(m.Arguments[1]);
                            }
                            else
                            {
                                this.Write("8000");
                            }
                            this.Write(", '')");
                            return;
                        case "IndexOf":
                            this.Write("(CHARINDEX(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(", ");
                            this.WriteValue(m.Object);
                            if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
                            {
                                this.Write(", ");
                                this.WriteValue(m.Arguments[1]);
                                this.Write(" + 1");
                            }
                            this.Write(") - 1)");
                            return;
                        case "Trim":
                            this.Write("RTRIM(LTRIM(");
                            this.WriteValue(m.Object);
                            this.Write("))");
                            return;
                    }
                }
                else if (m.Method.DeclaringType == typeof(DateTime))
                {
                    switch (m.Method.Name)
                    {
                        case "op_Subtract":
                            if (m.Arguments[1].Type == typeof(DateTime))
                            {
                                this.Write("DATEDIFF(");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(", ");
                                this.WriteValue(m.Arguments[1]);
                                this.Write(")");
                                return;
                            }
                            break;
                        case "AddYears":
                            this.Write("DATEADD(YYYY,");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(",");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                        case "AddMonths":
                            this.Write("DATEADD(MM,");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(",");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                        case "AddDays":
                            this.Write("DATEADD(DAY,");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(",");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                        case "AddHours":
                            this.Write("DATEADD(HH,");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(",");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                        case "AddMinutes":
                            this.Write("DATEADD(MI,");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(",");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                        case "AddSeconds":
                            this.Write("DATEADD(SS,");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(",");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                        case "AddMilliseconds":
                            this.Write("DATEADD(MS,");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(",");
                            this.WriteValue(m.Object);
                            this.Write(")");
                            return;
                    }
                }
                else if (m.Method.DeclaringType == typeof(Decimal))
                {
                    switch (m.Method.Name)
                    {
                        case "Add":
                        case "Subtract":
                        case "Multiply":
                        case "Divide":
                        case "Remainder":
                            this.Write("(");
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write(" ");
                            this.Write(GetOperatorText(m.Method.Name));
                            this.Write(" ");
                            this.WriteValueOperand(m.Arguments[1]);
                            this.Write(")");
                            return;
                        case "Negate":
                            this.Write("-");
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write("");
                            return;
                        case "Ceiling":
                        case "Floor":
                            this.Write(m.Method.Name.ToUpper());
                            this.Write("(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(")");
                            return;
                        case "Round":
                            if (m.Arguments.Count == 1)
                            {
                                this.Write("ROUND(");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(", 0)");
                                return;
                            }
                            else if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
                            {
                                this.Write("ROUND(");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(", ");
                                this.WriteValue(m.Arguments[1]);
                                this.Write(")");
                                return;
                            }
                            break;
                        case "Truncate":
                            this.Write("ROUND(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(", 0, 1)");
                            return;
                    }
                }
                else if (m.Method.DeclaringType == typeof(Math))
                {
                    switch (m.Method.Name)
                    {
                        case "Abs":
                        case "Acos":
                        case "Asin":
                        case "Atan":
                        case "Cos":
                        case "Exp":
                        case "Log10":
                        case "Sin":
                        case "Tan":
                        case "Sqrt":
                        case "Sign":
                        case "Ceiling":
                        case "Floor":
                            this.Write(m.Method.Name.ToUpper());
                            this.Write("(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(")");
                            return;
                        case "Atan2":
                            this.Write("ATN2(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(", ");
                            this.WriteValue(m.Arguments[1]);
                            this.Write(")");
                            return;
                        case "Log":
                            if (m.Arguments.Count == 1)
                                goto case "Log10";
                            break;
                        case "Pow":
                            this.Write("POWER(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(", ");
                            this.WriteValue(m.Arguments[1]);
                            this.Write(")");
                            return;
                        case "Round":
                            if (m.Arguments.Count == 1)
                            {
                                this.Write("ROUND(");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(", 0)");
                                return;
                            }
                            else if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
                            {
                                this.Write("ROUND(");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(", ");
                                this.WriteValue(m.Arguments[1]);
                                this.Write(")");
                                return;
                            }
                            break;
                        case "Truncate":
                            this.Write("ROUND(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(", 0, 1)");
                            return;
                    }
                }
                if (m.Method.Name == "ToString")
                {
                    if (m.Object.Type != typeof(string))
                    {
                        this.Write("CONVERT(NVARCHAR, ");
                        this.WriteValue(m.Object);
                        this.Write(")");
                    }
                    else
                    {
                        this.WriteValue(m.Object);
                    }
                    return;
                }
                else if (!m.Method.IsStatic && m.Method.Name == "CompareTo" && m.Method.ReturnType == typeof(int) && m.Arguments.Count == 1)
                {
                    this.Write("(CASE WHEN ");
                    this.WriteValueOperand(m.Object);
                    this.Write(" = ");
                    this.WriteValueOperand(m.Arguments[0]);
                    this.Write(" THEN 0 WHEN ");
                    this.WriteValueOperand(m.Object);
                    this.Write(" < ");
                    this.WriteValueOperand(m.Arguments[0]);
                    this.Write(" THEN -1 ELSE 1 END)");
                    return;
                }
                else if (m.Method.IsStatic && m.Method.Name == "Compare" && m.Method.ReturnType == typeof(int) && m.Arguments.Count == 2)
                {
                    this.Write("(CASE WHEN ");
                    this.WriteValueOperand(m.Arguments[0]);
                    this.Write(" = ");
                    this.WriteValueOperand(m.Arguments[1]);
                    this.Write(" THEN 0 WHEN ");
                    this.WriteValueOperand(m.Arguments[0]);
                    this.Write(" < ");
                    this.WriteValueOperand(m.Arguments[1]);
                    this.Write(" THEN -1 ELSE 1 END)");
                    return;
                }

                base.VisitMethodCall(m);
            }

            protected override void VisitNew(NewExpression nex)
            {
                if (nex.Constructor.DeclaringType == typeof(DateTime))
                {
                    if (nex.Arguments.Count == 3)
                    {
                        this.Write("Convert(DateTime, ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[0]);
                        this.Write(") + '/' + ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[1]);
                        this.Write(") + '/' + ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[2]);
                        this.Write("))");
                        return;
                    }
                    else if (nex.Arguments.Count == 6)
                    {
                        this.Write("Convert(DateTime, ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[0]);
                        this.Write(") + '/' + ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[1]);
                        this.Write(") + '/' + ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[2]);
                        this.Write(") + ' ' + ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[3]);
                        this.Write(") + ':' + ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[4]);
                        this.Write(") + ':' + ");
                        this.Write("Convert(nvarchar, ");
                        this.WriteValue(nex.Arguments[5]);
                        this.Write("))");
                        return;
                    }
                }

                base.VisitNew(nex);
            }

            protected override void VisitBinary(BinaryExpression b)
            {
                if (b.NodeType == ExpressionType.Power)
                {
                    this.Write("POWER(");
                    this.WriteValue(b.Left);
                    this.Write(", ");
                    this.WriteValue(b.Right);
                    this.Write(")");
                    return;
                }
                else if (b.NodeType == ExpressionType.Coalesce)
                {
                    this.Write("COALESCE(");
                    this.WriteValue(b.Left);
                    this.Write(", ");
                    var right = b.Right;
                    while (right.NodeType == ExpressionType.Coalesce)
                    {
                        var rb = (BinaryExpression)right;
                        this.WriteValue(rb.Left);
                        this.Write(", ");
                        right = rb.Right;
                    }
                    this.WriteValue(right);
                    this.Write(")");
                    return;
                }
                else if (b.NodeType == ExpressionType.LeftShift)
                {
                    this.WriteValueOperand(b.Left);
                    this.Write(" * POWER(2, ");
                    this.WriteValue(b.Right);
                    this.Write(")");
                    return;
                }
                else if (b.NodeType == ExpressionType.RightShift)
                {
                    this.WriteValueOperand(b.Left);
                    this.Write(" / POWER(2, ");
                    this.WriteValue(b.Right);
                    this.Write(")");
                    return;
                }
                
                base.VisitBinary(b);
            }

            protected override void WriteValue(Expression expr)
            {
                if (IsPredicate(expr))
                {
                    this.Write("CASE WHEN (");
                    this.Visit(expr);
                    this.Write(") THEN 1 ELSE 0 END");
                }
                else
                {
                    base.WriteValue(expr);
                }
            }

            protected override void VisitConditional(ConditionalExpression c)
            {
                if (this.IsPredicate(c.Test))
                {
                    this.Write("CASE WHEN ");
                    this.WritePredicate(c.Test);
                    this.Write(" THEN ");
                    this.WriteValue(c.IfTrue);
                    
                    Expression ifFalse = c.IfFalse;
                    while (ifFalse != null && ifFalse.NodeType == ExpressionType.Conditional)
                    {
                        ConditionalExpression fc = (ConditionalExpression)ifFalse;
                        this.Write(" WHEN ");
                        this.WritePredicate(fc.Test);
                        this.Write(" THEN ");
                        this.WriteValue(fc.IfTrue);
                        ifFalse = fc.IfFalse;
                    }

                    if (ifFalse != null)
                    {
                        this.Write(" ELSE ");
                        this.WriteValue(ifFalse);
                    }

                    this.Write(" END");
                }
                else
                {
                    this.Write("CASE ");
                    this.WriteValue(c.Test);
                    this.Write(" WHEN 0 THEN ");
                    this.WriteValue(c.IfFalse);
                    this.Write(" ELSE ");
                    this.WriteValue(c.IfTrue);
                    this.Write(" END");
                }

                return;
            }

            protected override void VisitRowNumber(RowNumberExpression rowNumber)
            {
                this.Write("ROW_NUMBER() OVER(");

                if (rowNumber.OrderBy != null && rowNumber.OrderBy.Count > 0)
                {
                    this.Write("ORDER BY ");

                    this.WriteCommaSeparated(rowNumber.OrderBy, exp =>
                    {
                        this.WriteValue(exp.Expression);
                        if (exp.OrderType != OrderType.Ascending)
                        {
                            this.Write(" DESC");
                        }
                    });
                }

                this.Write(")");
            }

            protected override void VisitIfCommand(IfCommand ifx)
            {
                if (this.Language.AllowsMultipleCommands)
                {
                    this.WriteLine();
                    this.Write("IF ");
                    this.WritePredicate(ifx.Test);

                    this.WriteLine();
                    this.Write("BEGIN");
                    this.WriteIndented(() =>
                    {
                        this.WriteLine();
                        this.VisitStatement(ifx.IfTrue);
                    });

                    if (ifx.IfFalse != null)
                    {
                        this.Write("END ELSE BEGIN");
                        this.WriteIndented(() =>
                        {
                            this.WriteLine();
                            this.VisitStatement(ifx.IfFalse);
                        });
                    }

                    this.WriteLine();
                    this.Write("END");
                }
                else
                {
                    base.VisitIfCommand(ifx);
                }
            }

            protected override void VisitBlockCommand(BlockCommand block)
            {
                if (this.Language.AllowsMultipleCommands)
                {
                    this.WriteBlankLineSeparated(
                        block.Commands,
                        command => this.VisitStatement(command)
                        );
                }
                else
                {
                    base.VisitBlockCommand(block);
                }
            }

            protected override void VisitDeclarationCommand(DeclarationCommand decl)
            {
                if (this.Language.AllowsMultipleCommands)
                {
                    this.WriteLineSeparated(
                        decl.Variables,
                        variable =>
                        {
                            this.Write("DECLARE ");
                            this.WriteVariableName(variable.Name);
                            this.Write(" ");
                            this.Write(this.Language.TypeSystem.Format(variable.QueryType, false));
                        });

                    if (decl.Source != null)
                    {
                        this.WriteLine();
                        this.Write("SELECT ");
                        this.WriteCommaSeparated(
                            decl.Variables,
                            variable =>
                            {
                                this.WriteVariableName(variable.Name);
                                this.Write(" = ");
                                this.WriteValue(variable.Expression);
                            });

                        if (decl.Source.From != null)
                        {
                            this.WriteLine();
                            this.Write("FROM ");
                            this.WriteIndented(() =>
                                this.WriteSource(decl.Source.From)
                                );
                        }

                        if (decl.Source.Where != null)
                        {
                            this.WriteLine();
                            this.Write("WHERE ");
                            this.WriteIndented(() => 
                                this.WritePredicate(decl.Source.Where));
                        }
                    }
                    else
                    {
                        this.WriteLineSeparated(
                            decl.Variables,
                            variable =>
                            {
                                this.Write("SET ");
                                this.WriteVariableName(variable.Name);
                                this.Write(" = ");
                                this.Visit(variable.Expression);
                            });
                    }
                }
                else
                {
                    base.VisitDeclarationCommand(decl);
                }
            }
        }
    }
}
