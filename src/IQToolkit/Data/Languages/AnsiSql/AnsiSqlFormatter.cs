// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.IO;

namespace IQToolkit.Data.AnsiSql
{
    using Expressions;
    using Utils;

    /// <summary>
    /// ANSI SQL <see cref="QueryFormatter"/>
    /// </summary>
    public sealed class AnsiSqlFormatter : QueryFormatter
    {
        private AnsiSqlFormatter()
        {
        }

        public static readonly AnsiSqlFormatter Default =
            new AnsiSqlFormatter();

        public override FormattedQuery Format(Expression expression, FormattingOptions? options = null)
        {
            var textWriter = new StringWriter();
            var parameters = new List<Expression>();
            var diagnostics = new List<Diagnostic>();
            var visitor = new SqlFormatterVisitor(options, AnsiSqlLanguage.Singleton, textWriter, parameters, diagnostics);

            visitor.Visit(expression);

            return new FormattedQuery(
                textWriter.ToString(),
                parameters,
                diagnostics
                );
        }

        /// <summary>
        /// The visitor that does all the text generation.
        /// </summary>
        public class SqlFormatterVisitor : DbVoidExpressionVisitor
        {
            private readonly FormattingOptions _options;
            private readonly QueryLanguage? _language;
            private readonly List<Expression> _parameterReferences;
            private readonly List<Diagnostic> _diagnostics;
            private readonly Dictionary<TableAlias, string> _aliases;
            private readonly IndentWriter _writer;

            public SqlFormatterVisitor(
                FormattingOptions? options,
                QueryLanguage? language,
                TextWriter writer,
                List<Expression> parameterReferences,
                List<Diagnostic> diagnostics)
            {
                _options = options ?? FormattingOptions.Default;
                _language = language;
                _parameterReferences = parameterReferences;
                _diagnostics = diagnostics;
                _aliases = new Dictionary<TableAlias, string>();
                _writer = new IndentWriter(writer, _options.Indentation);
            }

            protected FormattingOptions Options => _options;
            protected QueryLanguage? Language => _language;

            protected bool HideColumnAliases { get; set; }
            protected bool HideTableAliases { get; set; }
            protected bool IsNested { get; set; }

            /// <summary>
            /// Adds the diagnostic to the reported diagnostics.
            /// </summary>
            protected void ReportDiagnostic(Diagnostic diagnostic)
            {
                _diagnostics.Add(diagnostic);
            }

            /// <summary>
            /// Adds the diagnostic to the reported diagnostics.
            /// </summary>
            protected void ReportDiagnostic(string message)
            {
                _diagnostics.Add(new Diagnostic(message));
            }

            #region WriteXXX
            /// <summary>
            /// Write the text.
            /// </summary>
            protected void Write(string text)
            {
                _writer.Write(text);
            }

            /// <summary>
            /// Writes the text indented.
            /// The indentation only occurs if the text is the first text written after a new line.
            /// </summary>
            protected void WriteIndented(string text)
            {
                _writer.WriteIndented(text);
            }

            /// <summary>
            /// Executes the action with writing indented one level deeper.
            /// </summary>
            protected void WriteIndented(Action action)
            {
                _writer.WriteIndented(action);
            }

            /// <summary>
            /// Writes a new line conditionally.
            /// </summary>
            protected void WriteLine(bool allowBlankLines = false)
            {
                _writer.WriteLine(allowBlankLines);
            }

            /// <summary>
            /// Write a blank line.
            /// </summary>
            protected void WriteBlankLine()
            {
                _writer.WriteBlankLine();
            }

            /// <summary>
            /// Writes the text and then a new line.
            /// </summary>
            protected void WriteLine(string text)
            {
                _writer.WriteLine(text);
            }

            /// <summary>
            /// Writes the list of expressions separated by the separator.
            /// </summary>
            protected void WriteSeparated<TExpression>(
                IReadOnlyList<TExpression> expressions, 
                string seperator)
                where TExpression : Expression
            {
                _writer.WriteSeparated(
                    expressions, 
                    expr => this.WriteValue(expr),
                    () => this.Write(seperator)
                    );
            }

            /// <summary>
            /// Writes the list of items separated by the separator.
            /// </summary>
            protected void WriteSeparated<T>(
                IReadOnlyList<T> items,
                string seperator,
                Action<T> fnWriteItem)
            {
                _writer.WriteSeparated(
                    items,
                    fnWriteItem,
                    () => this.Write(seperator)
                    );
            }

            /// <summary>
            /// Writes the list of items separated by a separator.
            /// </summary>
            protected void WriteSeparated<T>(
                IReadOnlyList<T> items,
                Action<T> fnWriteItem,
                Action fnWriteSeparator)
            {
                _writer.WriteSeparated(items, fnWriteItem, fnWriteSeparator);
            }

            protected void WriteLineSeparated<T>(
                IReadOnlyList<T> items,
                Action<T> fnWriteItem)
            {
                WriteSeparated(
                    items, 
                    fnWriteItem, 
                    () => this.WriteLine()
                    );
            }

            protected void WriteBlankLineSeparated<T>(
                IReadOnlyList<T> items,
                Action<T> fnWriteItem)
            {
                WriteSeparated(
                    items,
                    fnWriteItem,
                    () => this.WriteBlankLine()
                    );
            }

            /// <summary>
            /// Writes the list of expression with a comma separator.
            /// </summary>
            protected void WriteCommaSeparated<TExpression>(IReadOnlyList<TExpression> expressions)
                where TExpression : Expression
            {
                WriteSeparated(expressions, ", ");
            }

            /// <summary>
            /// Writes the list of items with a comma separator.
            /// </summary>
            protected void WriteCommaSeparated<T>(IReadOnlyList<T> items, Action<T> fnWriteItem)
            {
                WriteSeparated(items, ", ", fnWriteItem);
            }
            #endregion

            /// <summary>
            /// Writes a parameter name.
            /// </summary>
            protected virtual void WriteParameterName(string name)
            {
                if (this.Options.IsOdbc)
                {
                    this.Write("?");
                }
                else
                {
                    this.WriteVariableName(name);
                }
            }

            /// <summary>
            /// Writes a variable name.
            /// </summary>
            protected virtual void WriteVariableName(string name)
            {
                this.Write("@");
                this.Write(name);
            }

            /// <summary>
            /// Writes a 'AS table-name' expression
            /// </summary>
            protected virtual void WriteAsAliasName(string aliasName)
            {
                this.Write("AS ");
                this.WriteAliasName(aliasName);
            }

            /// <summary>
            /// Writes a table alias name.
            /// </summary>
            protected virtual void WriteAliasName(string aliasName)
            {
                this.Write(aliasName);
            }

            /// <summary>
            /// Writes the 'AS name' expression
            /// </summary>
            protected virtual void WriteAsColumnName(string columnName)
            {
                this.Write("AS ");
                this.WriteColumnName(columnName);
            }

            /// <summary>
            /// Writes a column name.
            /// </summary>
            protected virtual void WriteColumnName(string columnName)
            {
                string name = (this.Language != null) 
                    ? this.Language.Quote(columnName) 
                    : columnName;
                this.Write(name);
            }

            /// <summary>
            /// Writes the table name
            /// </summary>
            protected virtual void WriteTableName(string tableName)
            {
                string name = (this.Language != null) 
                    ? this.Language.Quote(tableName) 
                    : tableName;
                this.Write(name);
            }

            /// <summary>
            /// Write the expression as a predicate.
            /// </summary>
            protected virtual void WritePredicate(Expression expr)
            {
                if (IsPredicate(expr))
                {
                    this.Visit(expr);
                }
                else
                {
                    this.WriteValueOperand(expr);
                    this.Write(" <> 0");
                }
            }

            /// <summary>
            /// Write the expression a value (not predicate)
            /// </summary>
            protected virtual void WriteValue(Expression expr)
            {
                if (IsPredicate(expr))
                    this.ReportDiagnostic($"ANSI SQL does not support predicates as values.");
                this.Visit(expr);
            }

            /// <summary>
            /// True if the expression needs parenthesis when used as an operand.
            /// </summary>
            protected virtual bool NeedsParenthesesAsOperand(Expression expr)
            {
                return !(
                    expr is ColumnExpression
                    || expr is TableExpression
                    || expr is ConstantExpression
                    || expr.NodeType == ExpressionType.Convert
                    || expr.NodeType == ExpressionType.ConvertChecked
                    || expr is ScalarSubqueryExpression
                    || expr is ExistsSubqueryExpression
                    || expr is BetweenExpression
                    || expr is InSubqueryExpression
                    || expr is ClientProjectionExpression // select - is top or already in parens
                    || expr is ClientParameterExpression // this is a parameter
                    );
            }

            /// <summary>
            /// Write the expression as a predicate and operand.
            /// </summary>
            protected virtual void WritePredicateOperand(Expression expr)
            {
                if (IsPredicate(expr))
                {
                    if (NeedsParenthesesAsOperand(expr))
                    {
                        this.Write("(");
                        this.Visit(expr);
                        this.Write(")");
                    }
                    else
                    {
                        this.Visit(expr);
                    }
                }
                else
                {
                    this.Write("(");
                    this.WriteValueOperand(expr);
                    this.Write(" <> 0");
                    this.Write(")");
                }
            }

            /// <summary>
            /// Write the expression as a value and operand.
            /// </summary>
            protected virtual void WriteValueOperand(Expression expr)
            {
                // TODO: use IFF when predicate
                if (NeedsParenthesesAsOperand(expr))
                {
                    this.Write("(");
                    this.Visit(expr);
                    this.Write(")");
                }
                else
                {
                    this.Visit(expr);
                }
            }

            /// <summary>
            /// Writes the literal value.
            /// </summary>
            protected virtual void WriteLiteral(object? value)
            {
                if (value == null)
                {
                    this.Write("NULL");
                }
                else if (value.GetType().IsEnum)
                {
                    // write enum literals as numbers
                    var enumText = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())).ToString();
                    this.Write(enumText);
                }
                else
                {
                    switch (value)
                    {
                        case bool b:
                            this.Write(b ? "1" : "0");
                            break;
                        case string s:
                            this.Write("'");
                            this.Write(s);
                            this.Write("'");
                            break;
                        case int _:
                        case uint _:
                        case short _:
                        case ushort _:
                        case long _:
                        case ulong _:
                            var intText = value.ToString();
                            this.Write(intText);
                            break;

                        case float _:
                        case double _:
                        case decimal _:
                            var numberText = ((IConvertible)value).ToString(NumberFormatInfo.InvariantInfo);
                            this.Write(numberText);
                            break;
                        default:
                            this.ReportDiagnostic($"The literal for '{value}' is not supported");
                            this.Write(value.ToString());
                            break;
                    }
                }
            }

            /// <summary>
            /// Gets the generated table alias name.
            /// </summary>
            protected virtual string GetTableAliasName(TableAlias alias)
            {
                if (!_aliases.TryGetValue(alias, out var name))
                {
                    name = $"__Alias({alias.SequenceId})";
                    _aliases.Add(alias, name);
                    this.ReportDiagnostic($"Undeclared table alias: {name}");
                }

                return name;
            }

            /// <summary>
            /// Generates a name of the table alias, if not already generated.
            /// </summary>
            protected void AddAlias(TableAlias alias)
            {
                if (!_aliases.TryGetValue(alias, out var name))
                {
                    name = "t" + _aliases.Count;
                    _aliases.Add(alias, name);
                }
            }

            /// <summary>
            /// Generate names for all aliased expressions.
            /// </summary>
            protected virtual void AddAliases(Expression? expr)
            {
                if (expr is AliasedExpression ax)
                {
                    this.AddAlias(ax.Alias);
                }
                else if (expr is JoinExpression jx)
                {
                    this.AddAliases(jx.Left);
                    this.AddAliases(jx.Right);
                }
            }

            protected override void VisitUnknown(Expression expression)
            {
                this.VisitUnsupported(expression);
            }

            protected override void VisitUnhandled(Expression expression)
            {
                this.VisitUnsupported(expression);
            }

            protected void VisitUnsupported(Expression expression)
            {
                this.ReportDiagnostic($"The expression node of type '{expression.GetNodeTypeName()}' is not supported");
                this.Write($"__{expression.GetNodeTypeName()}__");
            }

            protected override void VisitMember(MemberExpression m)
            {
                this.ReportDiagnostic($"The member access '{m.Member}' is not supported");
                this.WriteValue(m.Expression);
                this.Write(".");
                this.Write(m.Member.Name);
            }

            protected override void VisitMethodCall(MethodCallExpression m)
            {
                if (m.Method.DeclaringType == typeof(Decimal))
                {
                    switch (m.Method.Name)
                    {
                        case "Add":
                        case "Subtract":
                        case "Multiply":
                        case "Divide":
                        case "Remainder":
                            var opText = GetOperatorText(m.Method.Name)!;
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write(" ");
                            this.Write(opText);
                            this.Write(" ");
                            this.WriteValueOperand(m.Arguments[1]);
                            return;
                        case "Negate":
                            this.Write("-");
                            this.WriteValueOperand(m.Arguments[0]);
                            this.Write("");
                            return;
                        case "Compare":
                            this.Visit(
                                Expression.Condition(
                                    Expression.Equal(m.Arguments[0], m.Arguments[1]),
                                    Expression.Constant(0),
                                    Expression.Condition(
                                        Expression.LessThan(m.Arguments[0], m.Arguments[1]),
                                        Expression.Constant(-1),
                                        Expression.Constant(1)
                                        )));
                            return;
                    }
                }
                else if (m.Method.Name == "ToString" && m.Object.Type == typeof(string))
                {
                    this.WriteValue(m.Object);  // no op
                    return;
                }
                else if (m.Method.Name == "Equals")
                {
                    if (m.Method.IsStatic && m.Method.DeclaringType == typeof(object))
                    {
                        this.WriteValueOperand(m.Arguments[0]);
                        this.Write(" = ");
                        this.WriteValueOperand(m.Arguments[1]);
                        return;
                    }
                    else if (!m.Method.IsStatic && m.Arguments.Count == 1 && m.Arguments[0].Type == m.Object.Type)
                    {
                        this.WriteValueOperand(m.Object);
                        this.Write(" = ");
                        this.WriteValueOperand(m.Arguments[0]);
                        return;
                    }
                }

                this.ReportDiagnostic($"The method '{m.Method.Name}' is not supported");

                if (m.Object != null)
                {
                    this.WriteValue(m.Object);
                    this.Write(".");
                }

                this.Write(m.Method.Name);
                this.Write("(");
                this.WriteCommaSeparated(m.Arguments);
                this.Write(")");
            }

            protected virtual bool IsInteger(Type type)
            {
                return TypeHelper.IsInteger(type);
            }

            protected override void VisitNew(NewExpression nex)
            {
                this.ReportDiagnostic($"The constructor for '{nex.Constructor.DeclaringType}' is not supported");
                this.Write("new ");
                this.Write(nex.Type.Name);
                this.Write("(");
                this.WriteCommaSeparated(nex.Arguments);
                this.Write(")");
            }

            protected override void VisitOuterJoined(OuterJoinedExpression expr)
            {
                // outer-joinedness only matters to client projection
                this.Visit(expr.Expression);
            }

            protected override void VisitUnary(UnaryExpression u)
            {
                string op = this.GetOperatorText(u);

                switch (u.NodeType)
                {
                    case ExpressionType.Not:
                        if (u.Operand is IsNullExpression isNull)
                        {
                            this.WriteValueOperand(isNull.Expression);
                            this.Write(" IS NOT NULL");
                            return;
                        }
                        else if (IsBoolean(u.Operand.Type) || op.Length > 1)
                        {
                            this.Write(op);
                            this.Write(" ");
                            this.WritePredicateOperand(u.Operand);
                            return;
                        }
                        else
                        {
                            this.Write(op);
                            this.WriteValueOperand(u.Operand);
                            return;
                        }
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                        this.Write(op);
                        this.WriteValueOperand(u.Operand);
                        return;
                    case ExpressionType.UnaryPlus:
                        this.WriteValue(u.Operand);
                        return;
                    case ExpressionType.Convert:
                        // ignore conversions for now
                        this.Visit(u.Operand);
                        return;
                }

                this.ReportDiagnostic($"The unary operator '{u.NodeType}' is not supported");
                this.Write($"__{u.NodeType}__");
                this.Write("(");
                this.Visit(u.Operand);
                this.Write(")");
            }

            protected override void VisitBinary(BinaryExpression b)
            {
                var op = this.GetOperatorText(b);
                var left = b.Left;
                var right = b.Right;

                switch (b.NodeType)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        if (this.IsBoolean(left.Type))
                        {
                            this.WritePredicateOperand(left);
                            this.Write(" ");
                            this.Write(op);
                            this.Write(" ");
                            this.WritePredicateOperand(right);
                        }
                        else
                        {
                            this.WriteValueOperand(left);
                            this.Write(" ");
                            this.Write(op);
                            this.Write(" ");
                            this.WriteValueOperand(right);
                        }
                        return;
                    case ExpressionType.Equal:
                        if (right is ConstantExpression rightConst
                            && rightConst.Value == null)
                        {
                            this.WriteValueOperand(left);
                            this.Write(" IS NULL");
                            return;
                        }
                        else if (left is ConstantExpression leftConst
                            && leftConst.Value == null)
                        {
                            this.WriteValueOperand(right);
                            this.Write(" IS NULL");
                            return;
                        }
                        goto case ExpressionType.LessThan;
                    case ExpressionType.NotEqual:
                        if (right is ConstantExpression neRightConst
                            && neRightConst.Value == null)
                        {
                            this.WriteValueOperand(left);
                            this.Write(" IS NOT NULL");
                            return;
                        }
                        else if (left is ConstantExpression neLeftConst
                            && neLeftConst.Value == null)
                        {
                            this.WriteValueOperand(right);
                            this.Write(" IS NOT NULL");
                            return;
                        }
                        goto case ExpressionType.LessThan;
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                        // check for special x.CompareTo(y) && type.Compare(x,y)
                        if (left is MethodCallExpression mc 
                            && right is ConstantExpression mcConst
                            && mcConst.Value is int mcConstValue
                            && mcConstValue == 0)
                        {
                            if (mc.Method.Name == "CompareTo" && !mc.Method.IsStatic && mc.Arguments.Count == 1)
                            {
                                left = mc.Object;
                                right = mc.Arguments[0];
                            }
                            else if (
                                (mc.Method.DeclaringType == typeof(string) || mc.Method.DeclaringType == typeof(decimal))
                                    && mc.Method.Name == "Compare" && mc.Method.IsStatic && mc.Arguments.Count == 2)
                            {
                                left = mc.Arguments[0];
                                right = mc.Arguments[1];
                            }
                        }
                        goto case ExpressionType.Add;
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                    case ExpressionType.ExclusiveOr:
                    case ExpressionType.LeftShift:
                    case ExpressionType.RightShift:
                        this.WriteValueOperand(left);
                        this.Write(" ");
                        this.Write(op);
                        this.Write(" ");
                        this.WriteValueOperand(right);
                        return;
                    default:
                        this.ReportDiagnostic($"The binary operator '{b.NodeType}' is not supported");
                        this.Write($"__{b.NodeType}__");
                        this.Write("(");
                        this.Visit(b.Left);
                        this.Write(", ");
                        this.Visit(b.Right);
                        this.Write(")");
                        return;
                }
            }

            protected virtual string GetOperatorText(string methodName)
            {
                switch (methodName)
                {
                    case "Add": return "+";
                    case "Subtract": return "-";
                    case "Multiply": return "*";
                    case "Divide": return "/";
                    case "Negate": return "-";
                    case "Remainder": return "%";
                    default: return "";
                }
            }

            protected virtual string GetOperatorText(UnaryExpression u)
            {
                switch (u.NodeType)
                {
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                        return "-";
                    case ExpressionType.UnaryPlus:
                        return "+";
                    case ExpressionType.Not:
                        return IsBoolean(u.Operand.Type) ? "NOT" : "~";
                    default:
                        return "";
                }
            }

            protected virtual string GetOperatorText(BinaryExpression b)
            {
                switch (b.NodeType)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                        return (IsBoolean(b.Left.Type)) ? "AND" : "&";
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        return (IsBoolean(b.Left.Type) ? "OR" : "|");
                    case ExpressionType.Equal:
                        return "=";
                    case ExpressionType.NotEqual:
                        return "<>";
                    case ExpressionType.LessThan:
                        return "<";
                    case ExpressionType.LessThanOrEqual:
                        return "<=";
                    case ExpressionType.GreaterThan:
                        return ">";
                    case ExpressionType.GreaterThanOrEqual:
                        return ">=";
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        return "+";
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                        return "-";
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                        return "*";
                    case ExpressionType.Divide:
                        return "/";
                    case ExpressionType.Modulo:
                        return "%";
                    case ExpressionType.ExclusiveOr:
                        return "^";
                    case ExpressionType.LeftShift:
                        return "<<";
                    case ExpressionType.RightShift:
                        return ">>";
                    default:
                        return "";
                }
            }

            protected virtual bool IsBoolean(Type type)
            {
                return type == typeof(bool) || type == typeof(bool?);
            }

            protected virtual bool IsPredicate(Expression expr)
            {
                switch (expr.NodeType)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        return IsBoolean(((BinaryExpression)expr).Type);
                    case ExpressionType.Not:
                        return IsBoolean(((UnaryExpression)expr).Type);
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case (ExpressionType)DbExpressionType.IsNull:
                    case (ExpressionType)DbExpressionType.Between:
                    case (ExpressionType)DbExpressionType.ExistsSubquery:
                    case (ExpressionType)DbExpressionType.InSubquery:
                        return true;
                    case ExpressionType.Call:
                        return IsBoolean(((MethodCallExpression)expr).Type);
                    default:
                        return false;
                }
            }

            protected override void VisitConditional(ConditionalExpression c)
            {
                this.ReportDiagnostic("Conditional expressions not supported");
                this.Write("IFF(");
                this.Visit(c.Test);
                this.Write(", ");
                this.Visit(c.IfTrue);
                this.Write(", ");
                this.Visit(c.IfFalse);
                this.Write(")");
            }

            protected override void VisitConstant(ConstantExpression c)
            {
                this.WriteLiteral(c.Value);
            }

            protected override void VisitColumn(ColumnExpression column)
            {
                if (column.Alias != null && !this.HideColumnAliases)
                {
                    this.WriteAliasName(GetTableAliasName(column.Alias));
                    this.Write(".");
                }

                this.WriteColumnName(column.Name);
            }

            protected override void VisitParameter(ParameterExpression expr)
            {
                this.ReportDiagnostic($"Unexepected parameter: {expr.Name}");
                this.Write(expr.Name);
            }

            protected override void VisitClientProjection(ClientProjectionExpression proj)
            {
                // treat these like scalar subqueries
                if (proj.Projector is ColumnExpression)
                {
                    this.Write("(");
                    this.WriteIndented(() =>
                    {
                        this.WriteLine();
                        this.WriteValue(proj.Select);
                    });
                    this.Write(")");
                }
                else
                {
                    this.ReportDiagnostic("Non-scalar projections cannot be translated to SQL.");
                    this.Write("(");
                    this.WriteIndented(() =>
                    {
                        this.WriteLine();
                        this.WriteValue(proj.Select);
                    });
                    this.Write(")");
                }
            }

            protected override void VisitSelect(SelectExpression select)
            {
                this.WriteSelect(select, false);
            }

            protected virtual void WriteSelect(SelectExpression select, bool isScalarSubquery)
            { 
                this.AddAliases(select.From);

                this.Write("SELECT ");
                this.WriteIndented(() =>
                {
                    if (select.IsDistinct)
                    {
                        this.Write("DISTINCT ");
                    }

                    if (select.Take != null)
                    {
                        this.WriteTopClause(select.Take);
                    }

                    if (isScalarSubquery)
                    {
                        this.WriteValue(select.Columns[0].Expression);
                    }
                    else
                    {
                        this.WriteColumns(select.Columns);
                    }
                });

                if (select.From != null)
                {
                    this.WriteLine();
                    this.Write("FROM ");
                    this.WriteIndented(() => 
                        this.WriteSource(select.From));
                    this.WriteLine();
                }

                if (select.Where != null)
                {
                    this.WriteLine();
                    this.Write("WHERE ");
                    this.WriteIndented(() => 
                        this.WritePredicate(select.Where));
                    this.WriteLine();
                }

                if (select.GroupBy.Count > 0)
                {
                    this.WriteLine();
                    this.Write("GROUP BY ");
                    this.WriteIndented(() =>
                        this.WriteCommaSeparated(select.GroupBy));
                    this.WriteLine();
                }

                if (select.OrderBy.Count > 0)
                {
                    this.WriteLine();
                    this.Write("ORDER BY ");
                    this.WriteIndented(() =>
                    {
                        this.WriteCommaSeparated(select.OrderBy, ob =>
                        {
                            this.WriteValue(ob.Expression);

                            if (ob.OrderType != OrderType.Ascending)
                            {
                                this.Write(" DESC");
                            }
                        });
                    });
                    this.WriteLine();
                }
            }

            protected virtual void WriteTopClause(Expression expression)
            {
                this.Write("TOP (");
                this.WriteIndented(() =>
                    this.WriteValue(expression));
                this.Write(") ");
            }

            protected virtual void WriteColumns(IReadOnlyList<ColumnDeclaration> columns)
            {
                if (columns.Count > 0)
                {
                    this.WriteCommaSeparated(columns, column =>
                    {
                        var c = column.Expression as ColumnExpression;
                        this.WriteValue(column.Expression);

                        if (!string.IsNullOrEmpty(column.Name) && (c == null || c.Name != column.Name))
                        {
                            this.Write(" ");
                            this.WriteAsColumnName(column.Name);
                        }
                    });
                }
                else
                {
                    this.Write("NULL ");

                    if (this.IsNested)
                    {
                        this.WriteAsColumnName("tmp");
                        this.Write(" ");
                    }
                }
            }

            protected virtual void WriteSource(Expression source)
            {
                bool oldIsNestedQuery = this.IsNested;
                this.IsNested = true;

                switch (source)
                {
                    case TableExpression table:
                        this.WriteTableName(table.Name);
                        if (!this.HideTableAliases)
                        {
                            this.Write(" ");
                            this.WriteAsAliasName(GetTableAliasName(table.Alias));
                        }
                        break;
                    case SelectExpression select:
                        this.Write("(");
                        this.WriteIndented(() =>
                        {
                            this.WriteLine();
                            this.Visit(select);
                            this.Write(") ");
                            this.WriteAsAliasName(GetTableAliasName(select.Alias));
                            this.WriteLine();
                        });
                        break;
                    case JoinExpression join:
                        this.VisitJoin(join);
                        break;
                    default:
                        this.ReportDiagnostic($"Select source of type '{source.GetType().Name}' is not valid");
                        this.Visit(source);
                        break;
                }

                this.IsNested = oldIsNestedQuery;
            }

            protected override void VisitJoin(JoinExpression join)
            {
                this.WriteLine();
                this.WriteJoinLeftSource(join.Left);

                this.WriteLine();
                this.WriteJoinType(join.JoinType);
                this.Write(" ");

                this.WriteJoinRightSource(join.Right);

                if (join.Condition != null)
                {
                    this.WriteIndented(() =>
                    {
                        this.WriteLine();
                        this.Write("ON ");
                        this.WriteJoinOn(join.Condition);
                        this.WriteLine();
                    });
                }
            }

            protected virtual void WriteJoinLeftSource(Expression source)
            {
                this.WriteSource(source);
            }

            protected virtual void WriteJoinRightSource(Expression source)
            {
                this.WriteSource(source);
            }

            protected virtual void WriteJoinType(JoinType joinType)
            {
                switch (joinType)
                {
                    case JoinType.CrossJoin:
                        this.Write("CROSS JOIN");
                        break;
                    case JoinType.InnerJoin:
                        this.Write("INNER JOIN");
                        break;
                    case JoinType.CrossApply:
                        this.Write("CROSS APPLY");
                        break;
                    case JoinType.OuterApply:
                        this.Write("OUTER APPLY");
                        break;
                    case JoinType.LeftOuterJoin:
                    case JoinType.SingletonLeftOuterJoin:
                        this.Write("LEFT OUTER JOIN");
                        break;
                }
            }

            protected virtual void WriteJoinOn(Expression expression)
            {
                this.WritePredicate(expression);
            }


            protected virtual void WriteAggregateName(string aggregateName)
            {
                switch (aggregateName)
                {
                    case "Average":
                        this.Write("AVG");
                        break;
                    case "LongCount":
                        this.Write("COUNT");
                        break;
                    default:
                        this.Write(aggregateName.ToUpper());
                        break;
                }
            }

            protected virtual bool RequiresAsteriskWhenNoArgument(string aggregateName)
            {
                return aggregateName == "Count" || aggregateName == "LongCount";
            }

            protected override void VisitAggregate(AggregateExpression aggregate)
            {
                this.WriteAggregateName(aggregate.AggregateName);

                this.Write("(");

                if (aggregate.IsDistinct)
                {
                    this.Write("DISTINCT ");
                }
                if (aggregate.Argument != null)
                {
                    this.WriteValue(aggregate.Argument);
                }
                else if (RequiresAsteriskWhenNoArgument(aggregate.AggregateName))
                {
                    this.Write("*");
                }

                this.Write(")");
            }

            protected override void VisitIsNull(IsNullExpression isnull)
            {
                this.WriteValueOperand(isnull.Expression);
                this.Write(" IS NULL");
            }

            protected override void VisitBetween(BetweenExpression between)
            {
                this.WriteValueOperand(between.Expression);
                this.Write(" BETWEEN ");
                this.WriteValueOperand(between.Lower);
                this.Write(" AND ");
                this.WriteValueOperand(between.Upper);
            }

            protected override void VisitRowNumber(RowNumberExpression rowNumber)
            {
                this.ReportDiagnostic("Row number expression not supported.");
                this.Write("__RowNumber__");
                // TODO: display anyway?
            }

            protected override void VisitScalarSubquery(ScalarSubqueryExpression subquery)
            {
                this.Write("(");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteSelect(subquery.Select, isScalarSubquery: true);

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected override void VisitExistsSubquery(ExistsSubqueryExpression exists)
            {
                this.WriteIndented(() =>
                {
                    this.Write("EXISTS(");
                    this.WriteLine();
                    this.Visit(exists.Select);
                    this.Write(")");
                });
            }

            protected override void VisitInSubquery(InSubqueryExpression @in)
            {
                this.WriteValue(@in.Expression);
                this.Write(" IN (");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Visit(@in.Select);

                    this.WriteLine();
                    this.Write(")");
                });
            }

            protected override void VisitInValues(InValuesExpression @in)
            {
                if (@in.Values.Count == 0)
                {
                    this.Write("0 <> 0");
                }
                else
                {
                    this.WriteValue(@in.Expression);
                    this.Write(" IN (");
                    this.WriteIndented(() =>
                        this.WriteCommaSeparated(@in.Values));
                    this.Write(")");
                }
            }

            protected override void VisitClientParameter(ClientParameterExpression parameter)
            {
                _parameterReferences.Add(parameter);
                this.WriteParameterName(parameter.Name);
            }

            protected override void VisitInsertCommand(InsertCommand insert)
            {
                this.Write("INSERT INTO ");
                this.WriteTableName(insert.Table.Name);
                this.Write("(");
                this.WriteCommaSeparated(insert.Assignments, assignment =>
                {
                    this.WriteColumnName(assignment.Column.Name);
                });
                this.Write(")");

                this.WriteLine();
                this.Write("VALUES (");
                this.WriteCommaSeparated(insert.Assignments, assignments =>
                {
                    this.WriteValue(assignments.Expression);
                });
                this.Write(")");
            }

            protected override void VisitUpdateCommand(UpdateCommand update)
            {
                this.Write("UPDATE ");
                this.WriteTableName(update.Table.Name);
                
                bool saveHide = this.HideColumnAliases;
                this.HideColumnAliases = true;
                
                this.WriteLine();
                this.Write("SET ");
                this.WriteCommaSeparated(update.Assignments, assignment =>
                {
                    this.Visit(assignment.Column);
                    this.Write(" = ");
                    this.Visit(assignment.Expression);
                });

                if (update.Where != null)
                {
                    this.WriteLine();
                    this.Write("WHERE ");
                    this.WriteIndented(() => 
                        this.WritePredicate(update.Where));
                }

                this.HideColumnAliases = saveHide;
            }

            protected override void VisitDeleteCommand(DeleteCommand delete)
            {
                this.Write("DELETE FROM ");

                bool saveHideTable = this.HideTableAliases;
                bool saveHideColumn = this.HideColumnAliases;
                this.HideTableAliases = true;
                this.HideColumnAliases = true;

                this.WriteSource(delete.Table);

                if (delete.Where != null)
                {
                    this.WriteLine();
                    this.Write("WHERE ");
                    this.WriteIndented(() =>
                        this.WritePredicate(delete.Where));
                }

                this.HideTableAliases = saveHideTable;
                this.HideColumnAliases = saveHideColumn;
            }

            protected override void VisitIfCommand(IfCommand ifx)
            {
                this.ReportDiagnostic("IfCommand not supported");
            }

            protected override void VisitBlockCommand(BlockCommand block)
            {
                this.ReportDiagnostic("BlockCommand not supported");
            }

            protected override void VisitDeclarationCommand(DeclarationCommand decl)
            {
                this.ReportDiagnostic("DeclarationCommand not supported");
            }

            protected override void VisitVariable(VariableExpression vex)
            {
                this.WriteVariableName(vex.Name);
            }

            protected virtual void VisitStatement(Expression expression)
            {
                var p = expression as ClientProjectionExpression;
                if (p != null)
                {
                    this.Visit(p.Select);
                }
                else
                {
                    this.Visit(expression);
                }
            }

            protected override void VisitDbFunctionCall(DbFunctionCallExpression func)
            {
                this.Write(func.Name);

                if (func.Arguments.Count > 0)
                {
                    this.WriteIndented(() =>
                    {
                        this.Write("(");
                        this.WriteCommaSeparated(func.Arguments);
                        this.Write(")");
                    });
                }
            }
        }
    }
}
