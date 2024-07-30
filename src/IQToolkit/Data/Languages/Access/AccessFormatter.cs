// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Data.Access
{
    using Expressions;
    using Utils;

    /// <summary>
    /// Microsoft Access SQL <see cref="QueryFormatter"/>
    /// </summary>
    public sealed class AccessFormatter : QueryFormatter
    {
        private AccessFormatter()
        {
        }

        public static readonly AccessFormatter Singleton =
            new AccessFormatter();

        public override FormattedQuery Format(Expression expression, FormattingOptions? options = null)
        {
            var writer = new StringWriter();
            var parameterRefs = new List<Expression>();
            var diagnostics = new List<Diagnostic>();
            var formatter = new AccessFormatterVisitor(options, writer, parameterRefs, diagnostics);
            formatter.FormatWithParameters(expression);
            return new FormattedQuery(
                writer.ToString(),
                parameterRefs,
                diagnostics
                );
        }

        public class AccessFormatterVisitor : AnsiSql.AnsiSqlFormatter.SqlFormatterVisitor
        {
            public AccessFormatterVisitor(
                FormattingOptions? options, 
                TextWriter writer,
                List<Expression> parameterReferences,
                List<Diagnostic> diagnostics)
                : base(options, AccessLanguage.Singleton, writer, parameterReferences, diagnostics)
            {
            }

            public virtual void FormatWithParameters(Expression expression)
            {
                if (!this.Options.IsOdbc)
                {
                    var parameters = expression
                        .FindAll<ClientParameterExpression>()
                        .DistinctBy(cp => cp.Name)
                        .ToList();

                    if (parameters.Count > 0)
                    {
                        this.Write("PARAMETERS ");
                        this.WriteCommaSeparated(parameters, param =>
                        {
                            this.WriteParameterName(param.Name);
                            this.Write(" ");
                            this.Write(AccessLanguage.Singleton.TypeSystem.Format(param.QueryType, true));
                        });
                        this.Write(";");
                        this.WriteLine();
                    }
                }

                this.Visit(expression);
            }

            protected override void WriteValue(Expression expr)
            {
                if (IsPredicate(expr))
                {
                    this.Write("IIF(");
                    this.WritePredicate(expr);
                    this.Write(", 1, 0)");
                }
                else
                {
                    base.WriteValue(expr);
                }
            }

            protected override void VisitSelect(SelectExpression select)
            {
                if (select.Skip != null)
                {
                    if (select.OrderBy.Count == 0)
                    {
                        this.ReportDiagnostic("Microsoft Access SQL does not support the SKIP operation without explicit ordering");
                    }
                    else if (select.Take == null)
                    {
                        this.ReportDiagnostic("Microsoft Access SQL does not support the SKIP operation without the TAKE operation");
                    }
                    else
                    {
                        this.ReportDiagnostic("Microsfot Access SQL does not support the SKIP operation in this query");
                    }
                }

                base.VisitSelect(select);
            }

            protected override void WriteTopClause(Expression expression)
            {
                this.Write("TOP ");
                this.WriteValue(expression);
                this.Write(" ");
            }

            protected override void VisitJoin(JoinExpression join)
            {
                switch (join.JoinType)
                {
                    case JoinType.CrossJoin:
                        this.WriteCrossJoinSource(join.Left);
                        this.Write(", ");
                        this.WriteCrossJoinSource(join.Right);
                        break;
                    default:
                        base.VisitJoin(join);
                        break;
                }
            }

            protected virtual void WriteCrossJoinSource(Expression source)
            {
                if (source is JoinExpression join
                    && join.JoinType != JoinType.CrossJoin)
                {
                    this.WriteJoinNestedSource(source);
                }
                else
                {
                    // don't nest the cross joins
                    this.WriteSource(source);
                }
            }

            protected virtual void WriteJoinNestedSource(Expression source)
            {
                this.Write("(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteSource(source);

                    this.WriteLine();
                    this.WriteLine(")");
                });
            }

            protected override void WriteJoinLeftSource(Expression source)
            {
                if (source is JoinExpression)
                {
                    this.WriteJoinNestedSource(source);
                }
                else
                {
                    this.WriteSource(source);
                }
            }

            protected override void WriteJoinOn(Expression expression)
            {
                this.Write("(");
                this.WritePredicate(expression);
                this.Write(")");
            }

            protected override void VisitDeclarationCommand(DeclarationCommand decl)
            {
                if (decl.Source != null)
                {
                    this.Visit(decl.Source);
                }
                else
                {
                    base.VisitDeclarationCommand(decl);
                }
            }

            protected override void WriteColumns(IReadOnlyList<ColumnDeclaration> columns)
            {
                if (columns.Count == 0)
                {
                    this.Write("0");
                }
                else
                {
                    base.WriteColumns(columns);
                }
            }

            protected override void VisitMember(MemberExpression m)
            {
                if (m.Expression == null)
                    return;

                if (m.Member.DeclaringType == typeof(string))
                {
                    switch (m.Member.Name)
                    {
                        case "Length":
                            this.Write("Len(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                    }
                }
                else if (m.Member.DeclaringType == typeof(DateTime) 
                    || m.Member.DeclaringType == typeof(DateTimeOffset))
                {
                    switch (m.Member.Name)
                    {
                        case "Day":
                            this.Write("Day(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Month":
                            this.Write("Month(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Year":
                            this.Write("Year(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Hour":
                            this.Write("Hour( ");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Minute":
                            this.Write("Minute(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "Second":
                            this.Write("Second(");
                            this.WriteValue(m.Expression);
                            this.Write(")");
                            return;
                        case "DayOfWeek":
                            this.Write("(Weekday(");
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
                    if (m.Object == null)
                    {
                        switch (m.Method.Name)
                        {
                            case "Concat":
                                var args = m.Arguments;
                                if (args.Count == 1 && args[0] is NewArrayExpression newArray)
                                    args = newArray.Expressions;
                                this.WriteSeparated(args, " + ", this.WriteValueOperand);
                                return;
                            case "IsNullOrEmpty":
                                this.WriteValueOperand(m.Arguments[0]);
                                this.Write(" IS NULL OR ");
                                this.WriteValueOperand(m.Arguments[0]);
                                this.Write(" = ''");
                                return;
                        }
                    }
                    else
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
                            case "ToUpper":
                                this.Write("UCase(");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                            case "ToLower":
                                this.Write("LCase(");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                            case "Substring":
                                this.Write("Mid(");
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
                            case "Replace":
                                this.Write("Replace(");
                                this.WriteValue(m.Object);
                                this.Write(", ");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(", ");
                                this.WriteValue(m.Arguments[1]);
                                this.Write(")");
                                return;
                            case "IndexOf":
                                this.Write("InStr(");
                                if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
                                {
                                    this.WriteValueOperand(m.Arguments[1]);
                                    this.Write(" + 1, ");
                                }
                                this.Visit(m.Object);
                                this.Write(", ");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(") - 1");
                                return;
                            case "Trim":
                                this.Write("Trim(");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                        }
                    }
                }
                else if (m.Method.DeclaringType == typeof(DateTime))
                {
                    if (m.Object == null)
                    {
                        switch (m.Method.Name)
                        {
                            case "op_Subtract":
                                if (m.Arguments[1].Type == typeof(DateTime))
                                {
                                    this.Write("DateDiff('d',");
                                    this.WriteValue(m.Arguments[0]);
                                    this.Write(",");
                                    this.WriteValue(m.Arguments[1]);
                                    this.Write(")");
                                    return;
                                }
                                break;
                        }
                    }
                    else
                    {
                        switch (m.Method.Name)
                        {
                            case "AddYears":
                                this.Write("DateAdd('yyyy',");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(",");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                            case "AddMonths":
                                this.Write("DateAdd('m',");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(",");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                            case "AddDays":
                                this.Write("DateAdd('d',");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(",");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                            case "AddHours":
                                this.Write("DateAdd('h',");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(",");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                            case "AddMinutes":
                                this.Write("DateAdd('n',");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(",");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                            case "AddSeconds":
                                this.Write("DateAdd('s',");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(",");
                                this.WriteValue(m.Object);
                                this.Write(")");
                                return;
                        }
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
                            this.WriteValue(m.Arguments[0]);
                            this.Write(" ");
                            this.Write(GetOperatorText(m.Method.Name)!);
                            this.Write(" ");
                            this.WriteValue(m.Arguments[1]);
                            this.Write(")");
                            return;
                        case "Negate":
                            this.Write("-");
                            this.WriteValueOperand(m.Arguments[0]);
                            return;
                        case "Truncate":
                            this.Write("Fix");
                            this.Write("(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(")");
                            return;
                        case "Round":
                            if (m.Arguments.Count == 1)
                            {
                                this.Write("Round(");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(")");
                                return;
                            }
                            break;
                    }
                }
                else if (m.Method.DeclaringType == typeof(Math))
                {
                    switch (m.Method.Name)
                    {
                        case "Abs":
                        case "Cos":
                        case "Exp":
                        case "Sin":
                        case "Tan":
                            this.Write(m.Method.Name.ToUpper());
                            this.Write("(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(")");
                            return;
                        case "Sqrt":
                            this.Write("Sqr(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(")");
                            return;
                        case "Sign":
                            this.Write("Sgn(");
                            this.Visit(m.Arguments[0]);
                            this.Write(")");
                            return;
                        case "Atan":
                            this.Write("Atn(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(")");
                            return;
                        case "Log":
                            if (m.Arguments.Count == 1)
                            {
                                this.Write("Log(");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(")");
                                return;
                            }
                            break;
                        case "Pow":
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write("^");
                            this.WriteValueOperand(m.Arguments[1]);
                            return;
                        case "Round":
                            if (m.Arguments.Count == 1)
                            {
                                this.Write("Round(");
                                this.WriteValue(m.Arguments[0]);
                                this.Write(")");
                                return;
                            }
                            break;
                        case "Truncate":
                            this.Write("Fix(");
                            this.WriteValue(m.Arguments[0]);
                            this.Write(")");
                            return;
                    }
                }

                if (m.Method.Name == "ToString"
                    && m.Object != null)
                {
                    if (m.Object.Type != typeof(string))
                    {
                        this.Write("CStr(");
                        this.WriteValue(m.Object);
                        this.Write(")");
                    }
                    else
                    {
                        this.WriteValue(m.Object);
                    }
                    return;
                }
                else if (!m.Method.IsStatic
                    && m.Method.Name == "CompareTo"
                    && m.Method.ReturnType == typeof(int)
                    && m.Arguments.Count == 1
                    && m.Object != null)
                {
                    this.Write("IIF(");
                    this.WriteValueOperand(m.Object);
                    this.Write(" = ");
                    this.WriteValueOperand(m.Arguments[0]);
                    this.Write(", 0, IIF(");
                    this.WriteValueOperand(m.Object);
                    this.Write(" < ");
                    this.WriteValueOperand(m.Arguments[0]);
                    this.Write(", -1, 1))");
                    return;
                }
                else if (m.Method.IsStatic
                    && m.Method.Name == "Compare"
                    && m.Method.ReturnType == typeof(int)
                    && m.Arguments.Count == 2)
                {
                    this.Write("IIF(");
                    this.WriteValueOperand(m.Arguments[0]);
                    this.Write(" = ");
                    this.WriteValueOperand(m.Arguments[1]);
                    this.Write(", 0, IIF(");
                    this.WriteValueOperand(m.Arguments[0]);
                    this.Write(" < ");
                    this.WriteValueOperand(m.Arguments[1]);
                    this.Write(", -1, 1))");
                    return;
                }

                base.VisitMethodCall(m);
            }

            protected override void VisitNew(NewExpression nex)
            {
                if (nex.Constructor != null
                    && nex.Constructor.DeclaringType == typeof(DateTime))
                {
                    if (nex.Arguments.Count == 3)
                    {
                        this.Write("CDate(");
                        this.WriteValueOperand(nex.Arguments[0]);
                        this.Write(" & '/' & ");
                        this.WriteValueOperand(nex.Arguments[1]);
                        this.Write(" & '/' & ");
                        this.WriteValueOperand(nex.Arguments[2]);
                        this.Write(")");
                        return;
                    }
                    else if (nex.Arguments.Count == 6)
                    {
                        this.Write("CDate(");
                        this.WriteValueOperand(nex.Arguments[0]);
                        this.Write(" & '/' & ");
                        this.WriteValueOperand(nex.Arguments[1]);
                        this.Write(" & '/' & ");
                        this.WriteValueOperand(nex.Arguments[2]);
                        this.Write(" & ' ' & ");
                        this.WriteValueOperand(nex.Arguments[3]);
                        this.Write(" & ':' & ");
                        this.WriteValueOperand(nex.Arguments[4]);
                        this.Write(" & + ':' & ");
                        this.WriteValueOperand(nex.Arguments[5]);
                        this.Write(")");
                        return;
                    }
                }
                
                base.VisitNew(nex);
            }

            protected override void VisitBinary(BinaryExpression b)
            {
                if (b.NodeType == ExpressionType.Power)
                {
                    this.WriteValueOperand(b.Left);
                    this.Write("^");
                    this.WriteValueOperand(b.Right);
                    return;
                }
                else if (b.NodeType == ExpressionType.Coalesce)
                {
                    this.Write("IIF(");
                    this.WriteValue(b.Left);
                    this.Write(" IS NOT NULL, ");
                    this.WriteValue(b.Left);
                    this.Write(", ");
                    this.WriteValue(b.Right);
                    this.Write(")");
                    return;
                }
                else if (b.NodeType == ExpressionType.LeftShift)
                {
                    this.Write("(");
                    this.WriteValue(b.Left);
                    this.Write(" * (2^");
                    this.WriteValue(b.Right);
                    this.Write("))");
                    return;
                }
                else if (b.NodeType == ExpressionType.RightShift)
                {
                    this.Write("(");
                    this.WriteValue(b.Left);
                    this.Write(@" \ (2^");
                    this.WriteValue(b.Right);
                    this.Write("))");
                    return;
                }
                
                base.VisitBinary(b);
            }

            protected override void VisitConditional(ConditionalExpression c)
            {
                this.Write("IIF(");
                this.WritePredicate(c.Test);
                this.Write(", ");
                this.WriteValue(c.IfTrue);
                this.Write(", ");
                this.WriteValue(c.IfFalse);
                this.Write(")");
            }

            protected override string GetOperatorText(BinaryExpression b)
            {
                switch (b.NodeType)
                {
                    case ExpressionType.And:
                        if (b.Type == typeof(bool) || b.Type == typeof(bool?))
                            return "AND";
                        return "BAND";
                    case ExpressionType.AndAlso:
                        return "AND";
                    case ExpressionType.Or:
                        if (b.Type == typeof(bool) || b.Type == typeof(bool?))
                            return "OR";
                        return "BOR";
                    case ExpressionType.OrElse:
                        return "OR";
                    case ExpressionType.Modulo:
                        return "MOD";
                    case ExpressionType.ExclusiveOr:
                        return "XOR";
                    case ExpressionType.Divide:
                        if (this.IsInteger(b.Type))
                            return "\\"; // integer divide
                        goto default;
                    default:
                        return base.GetOperatorText(b);
                }
            }

            protected override string GetOperatorText(UnaryExpression u)
            {
                switch (u.NodeType)
                {
                    case ExpressionType.Not:
                        return "NOT";
                    default:
                        return base.GetOperatorText(u);
                }
            }

            protected override string GetOperatorText(string methodName)
            {
                if (methodName == "Remainder")
                {
                    return "MOD";
                }
                else
                {
                    return base.GetOperatorText(methodName);
                }
            }

            protected override void WriteLiteral(object? value)
            {
                if (value is bool b)
                {
                    this.Write(b ? "-1" : "0");
                }
                else
                {
                    base.WriteLiteral(value);
                }
            }
        }
    }
}
