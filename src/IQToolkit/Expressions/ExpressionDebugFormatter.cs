// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.IO;

namespace IQToolkit.Expressions
{
    using System.Globalization;
    using Utils;

    /// <summary>
    /// Formats an expression into a pseudo language syntax.
    /// For debug visualization purposes only.
    /// </summary>
    public class ExpressionDebugFormatter : ExpressionFormatter
    {
        private ExpressionDebugFormatter()
        {
        }

        public static readonly ExpressionFormatter Singleton =
            new ExpressionDebugFormatter();

        public override void Format(Expression expression, TextWriter writer, string indentation)
        {
            new Writer(writer, indentation).Write(expression);
        }

        public class Writer : IndentWriter
        {
            public Writer(
                TextWriter textWriter,
                string indentation)
                : base(textWriter, indentation)
            {
            }

            public virtual void Write(Expression expression)
            {
                switch (expression)
                {
                    case BinaryExpression be:
                        this.WriteBinary(be);
                        break;
                    case BlockExpression blk:
                        this.WriteBlock(blk);
                        break;
                    case ConditionalExpression ce:
                        this.WriteConditional(ce);
                        break;
                    case ConstantExpression ce:
                        this.WriteConstant(ce);
                        break;
                    case DebugInfoExpression dbi:
                        this.WriteDebugInfo(dbi);
                        break;
                    case DefaultExpression def:
                        this.WriteDefault(def);
                        break;
                    case DynamicExpression dyn:
                        this.WriteDynamic(dyn);
                        break;
                    case GotoExpression go2:
                        this.WriteGoto(go2);
                        break;
                    case IndexExpression idx:
                        this.WriteIndex(idx);
                        break;
                    case InvocationExpression ie:
                        this.WriteInvocation(ie);
                        break;
                    case LabelExpression lab:
                        this.WriteLabel(lab);
                        break;
                    case LambdaExpression le:
                        this.WriteLambda(le);
                        break;
                    case ListInitExpression li:
                        this.WriteListInit(li);
                        break;
                    case LoopExpression lop:
                        this.WriteLoop(lop);
                        break;
                    case MemberExpression me:
                        this.WriteMember(me);
                        break;
                    case MemberInitExpression mi:
                        this.WriteMemberInit(mi);
                        break;
                    case MethodCallExpression mc:
                        this.WriteMethodCall(mc);
                        break;
                    case NewExpression ne:
                        this.WriteNew(ne);
                        break;
                    case NewArrayExpression na:
                        this.WriteNewArray(na);
                        break;
                    case ParameterExpression pe:
                        this.WriteParameter(pe);
                        break;
                    case RuntimeVariablesExpression rve:
                        this.WriteRuntimeVariables(rve);
                        break;
                    case SwitchExpression sx:
                        this.WriteSwitch(sx);
                        break;
                    case TryExpression trx:
                        this.WriteTry(trx);
                        break;
                    case TypeBinaryExpression tbe:
                        this.WriteTypeBinary(tbe);
                        break;
                    case UnaryExpression ue:
                        this.WriteUnary(ue);
                        break;
                    default:
                        this.Write(expression.ToString());
                        break;
                }
            }

            protected virtual void WriteBlockAlways(Expression expression)
            {
                if (expression is BlockExpression)
                {
                    this.Write(expression);
                }
                else
                {
                    this.WriteLine();
                    this.WriteLine("{");
                    this.WriteIndented(() => this.Write(expression));
                    this.WriteLine("}");
                }
            }

            protected virtual void WriteBinary(BinaryExpression b)
            {
                switch (b.NodeType)
                {
                    case ExpressionType.ArrayIndex:
                        this.WriteOperand(b.Left);
                        this.Write("[");
                        this.Write(b.Right);
                        this.Write("]");
                        break;
                    default:
                        if (GetInfixOperator(b.NodeType) is { } opText)
                        {
                            this.WriteOperand(b.Left);
                            this.Write(" ");
                            this.Write(opText);
                            this.Write(" ");
                            this.WriteOperand(b.Right);
                        }
                        else
                        {
                            this.Write(b.NodeType.ToString());
                            this.Write("(");
                            this.Write(b.Left);
                            this.Write(", ");
                            this.Write(b.Right);
                            this.Write(")");
                        }
                        break;
                }
            }

            protected virtual void WriteBlock(BlockExpression block)
            {

                this.WriteLine();
                this.WriteLine("{");
                this.WriteIndented(() =>
                {
                    if (block.Variables.Count > 0)
                    {
                        this.WriteLineSeparated(
                            block.Variables,
                            fnWriteItem: variable =>
                            {
                                this.WriteLine();
                                this.WriteVariable(variable);
                                this.WriteLine(";");
                            });
                    }

                    this.WriteLine();
                    this.WriteSeparated(
                        block.Expressions,
                        fnWriteItem: expression =>
                        {
                            this.WriteLine();
                            this.Write(expression);
                        },
                        () => this.WriteLine(";")
                        );
                });
                this.WriteLine();
                this.WriteLine("}");
            }

            protected virtual void WriteVariable(ParameterExpression variable)
            {
                this.Write(GetTypeName(variable.Type));
                this.Write(" ");
                this.Write(variable.Name);
            }

            protected virtual void WriteConditional(ConditionalExpression c)
            {
                this.Write(c.Test);
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.Write("? ");
                    this.WriteIndented(() =>
                        this.Write(c.IfTrue));                   
                    this.WriteLine();
                    this.Write(": ");
                    this.WriteIndented(() =>
                        this.Write(c.IfFalse));
                    this.WriteLine();
                });
            }

            protected virtual void WriteConstant(ConstantExpression c)
            {
                if (IsPrimitiveValue(c.Value))
                {
                    this.WriteLiteral(c.Value);
                }
                else
                {
                    this.Write("Constant(");
                    this.WriteIndented(() =>
                    {
                        this.WriteLine();
                        this.WriteLiteral(c.Value);
                        this.WriteLine();
                        this.Write(")");
                    });
                }
            }

            private bool IsPrimitiveType(Type type)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                        return false;

                    case TypeCode.Object:
                        return type == typeof(DateTimeOffset)
                            || type == typeof(TimeSpan)
                            || type == typeof(Guid);

                    default:
                        return true;
                }
            }

            private bool IsPrimitiveValue(object? value)
            {
                return value == null
                    || IsPrimitiveType(value.GetType());
            }

            private const int MAX_LITERAL_DEPTH = 2;

            private void WriteLiteral(object? value, int depth = 0)
            {
                if (value == null)
                {
                    this.Write("null");
                    return;
                }

                var type = value.GetType();
                if (type == typeof(object))
                {
                    this.Write("object");
                }
                else if (value is string stringValue)
                {
                    this.WriteStringLiteral(stringValue);
                }
                else if (value is Type typeValue)
                {
                    this.Write($"typeof({this.GetTypeName(typeValue)})");
                }
                else if (type.IsEnum)
                {
                    this.Write($"{type.Name}.{value.ToString()}");
                }
                else if (TryGetMatchingStaticProperty(type, value, out var staticProp))
                {
                    this.Write($"{type.Name}.{staticProp.Name}");
                }
                else if (IsPrimitiveType(type))
                {
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            this.Write(value.ToString());
                            break;
                        case TypeCode.Decimal:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            var numberText = ((IConvertible)value).ToString(NumberFormatInfo.InvariantInfo);
                            this.Write(numberText);
                            break;
                        default:
                            this.Write($"{type.Name}({value.ToString()})");
                            break;
                    }
                }
                else if (type.IsArray
                    || type.IsAssignableToGeneric(typeof(IReadOnlyList<>)))
                {
                    var values = ((IEnumerable)value).OfType<object>().ToList();

                    if (values.Count == 0)
                    {
                        this.Write("[]");
                    }
                    else if (depth > MAX_LITERAL_DEPTH)
                    {
                        this.Write("[ ... ]");
                    }
                    else if (values.Count == 1 && IsPrimitiveValue(values[0]))
                    {
                        this.Write("[");
                        this.WriteLiteral(values[0], depth);
                        this.Write("]");
                    }
                    else
                    {
                        this.Write("[");
                        this.WriteIndented(() =>
                        {
                            this.WriteSeparated(
                                values,
                                item =>
                                {
                                    this.WriteLine();
                                    this.WriteLiteral(item, depth + 1);
                                },
                                () => this.WriteLine(",")
                                );

                            this.WriteLine();
                            this.Write("]");
                        });
                    }
                }
                else
                {
                    var dms = TypeHelper.GetDeclaredFieldsAndProperties(type)
                        .OrderBy(p => p.Name)
                        .ToList();

                    if (dms.Count == 0)
                    {
                        this.Write(this.GetTypeName(type));
                        this.Write(" { }");
                    }
                    else if (depth >= MAX_LITERAL_DEPTH)
                    {
                        this.Write(this.GetTypeName(type));
                        this.Write(" { ... }");
                    }
                    else
                    {
                        this.Write(this.GetTypeName(type));
                        this.WriteLine(" {");
                        this.WriteIndented(() =>
                        {
                            this.WriteLine();
                            this.WriteSeparated(
                                dms,
                                dm =>
                                {
                                    this.WriteLine();
                                    this.Write(dm.Name);
                                    this.Write(" = ");
                                    var dmValue = dm.GetFieldOrPropertyValue(value);
                                    this.WriteLiteral(dmValue, depth + 1);
                                },
                                () => this.WriteLine(","));
                            this.WriteLine();
                            this.Write("}");
                        });
                    }
                }
            }

            private static bool TryGetMatchingStaticProperty(Type type, object value, out PropertyInfo property)
            {
                var valueType = value.GetType();
                property = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy)
                    .FirstOrDefault(p => p.PropertyType.IsAssignableFrom(valueType) && p.GetValue(null) == value);
                return property != null;
            }

            private static readonly char[] _lineBreaks = new[] { '\r', '\n' };
            private static readonly char[] _special = new char[] { '\r', '\n', '\\', '\t' };

            protected virtual void WriteStringLiteral(string text)
            {
                if (text.Contains("\n"))
                {
                    // write multi-line string raw string literal.
                    var lines = text.Split(_lineBreaks, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.TrimEnd()) // leave any starting whitespace
                        .ToList();
                    this.WriteLine();
                    this.Write("\"\"\"");
                    foreach (var line in lines)
                    {
                        this.WriteLine();
                        this.Write(line);
                    }
                    this.WriteLine();
                    this.Write("\"\"\"");
                    this.WriteLine();
                }
                else
                {
                    // TODO: use escapes?
                    if (text.IndexOfAny(_special) >= 0)
                        this.Write("@");
                    this.Write("\"");
                    this.Write(text);
                    this.Write("\"");
                }
            }

            protected virtual void WriteDebugInfo(DebugInfoExpression debugInfo)
            {
                this.WriteLine();
                this.Write($"[Debug: {debugInfo.Document.FileName}: ({debugInfo.StartLine}, {debugInfo.StartColumn}) - ({debugInfo.EndLine}, {debugInfo.EndColumn})]");
                this.WriteLine();
            }

            protected virtual void WriteDefault(DefaultExpression @default)
            {
                this.Write("default");
            }

            protected virtual void WriteDynamic(DynamicExpression @dynamic)
            {
                this.Write("dynamic(");
                this.WriteArgumentList(dynamic.Arguments);
                this.Write(")");
            }

            protected virtual void WriteGoto(GotoExpression @goto)
            {
                switch (@goto.Kind)
                {
                    case GotoExpressionKind.Break:
                        this.WriteLine("break;");
                        break;
                    case GotoExpressionKind.Continue:
                        this.WriteLine("continue;");
                        break;
                    case GotoExpressionKind.Goto:
                        this.Write($"goto {@goto.Target.Name}");
                        if (@goto.Value != null)
                        {
                            this.Write(" ");
                            this.Write(@goto.Value);
                        }
                        this.WriteLine(";");
                        break;
                    case GotoExpressionKind.Return:
                        this.Write($"return");
                        if (@goto.Value != null)
                        {
                            this.Write(" ");
                            this.Write(@goto.Value);
                        }
                        this.WriteLine(";");
                        break;
                }
            }

            protected virtual void WriteIndex(IndexExpression index)
            {
                if (index.Object != null)
                    this.Write(index.Object);
                this.Write("[");
                this.WriteArgumentList(index.Arguments);
                this.Write("]");
            }

            protected virtual void WriteInvocation(InvocationExpression invocation)
            {
                this.Write(invocation.Expression);
                this.Write("(");
                this.WriteIndented(() =>
                    this.WriteExpressionList(invocation.Arguments));
                this.Write(")");
            }

            protected virtual void WriteLabel(LabelExpression label)
            {
                this.Write($"{label.Target.Name}:");
                if (label.DefaultValue != null)
                {
                    this.Write(" [");
                    this.Write(label.DefaultValue);
                    this.Write("]");
                }
                this.WriteLine();
            }

            protected virtual void WriteLambda(LambdaExpression lambda)
            {
                if (lambda.Parameters.Count != 1)
                {
                    this.Write("(");
                    this.WriteCommaSeparated(
                        lambda.Parameters,
                        parameter =>
                            this.Write(parameter.Name)
                        );
                    this.Write(")");
                }
                else
                {
                    this.Write(lambda.Parameters[0].Name);
                }

                this.Write(" => ");
                this.Write(lambda.Body);
            }

            protected virtual void WriteListInit(ListInitExpression init)
            {
                this.Write(init.NewExpression);
                this.Write(" {");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteElementInitializerList(init.Initializers);
                    this.WriteLine();
                });
                this.Write("}");
            }

            protected virtual void WriteLoop(LoopExpression loop)
            {
                this.WriteLine("loop");
                this.WriteLine("{");
                this.Write(loop.Body);
                this.WriteLine();
                this.WriteLine("}");
            }

            protected virtual void WriteMember(MemberExpression m)
            {
                this.Write(m.Expression);
                this.Write(".");
                this.Write(m.Member.Name);
            }

            protected virtual void WriteMemberInit(MemberInitExpression init)
            {
                this.Write(init.NewExpression);
                this.WriteLine();
                this.Write("{");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteBindingList(init.Bindings);
                });
                this.WriteLine();
                this.Write("}");
                this.WriteLine();
            }

            protected virtual void WriteBindingList(IReadOnlyList<MemberBinding> bindings)
            {
                this.WriteSeparated(
                    bindings,
                    fnWriteItem: binding =>
                        this.WriteBinding(binding),
                    fnWriteSeparator: () =>
                        this.WriteLine(",")
                    );
                this.WriteLine();
            }

            protected virtual void WriteBinding(MemberBinding binding)
            {
                switch (binding)
                {
                    case MemberListBinding mlb:
                        this.WriteMemberListBinding(mlb);
                        break;
                    case MemberMemberBinding mmb:
                        this.WriteMemberMemberBinding(mmb);
                        break;
                    case MemberAssignment ma:
                        this.WriteMemberAssignment(ma);
                        break;
                    default:
                        this.Write($"Unhandled({binding.GetType().Name})");
                        break;
                }
            }

            protected virtual void WriteMemberListBinding(MemberListBinding binding)
            {
                this.Write(binding.Member.Name);
                this.Write(" = {");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteElementInitializerList(binding.Initializers);
                    this.WriteLine();
                });
                this.Write("}");
            }

            protected virtual void WriteMemberMemberBinding(MemberMemberBinding binding)
            {
                this.Write(binding.Member.Name);
                this.Write(" = {");
                this.WriteIndented(() =>
                {
                    this.WriteLine();
                    this.WriteBindingList(binding.Bindings);
                    this.WriteLine();
                });
                this.Write("}");
            }

            protected virtual void WriteElementInitializer(ElementInit initializer)
            {
                if (initializer.Arguments.Count > 1)
                {
                    this.Write("{");
                    this.WriteCommaSeparated(
                        initializer.Arguments,
                        argument => this.Write(argument)
                        );
                    this.Write("}");
                }
                else
                {
                    this.Write(initializer.Arguments[0]);
                }
            }

            protected virtual void WriteElementInitializerList(IReadOnlyList<ElementInit> elementInits)
            {
                this.WriteSeparated(
                    elementInits,
                    fnWriteItem: init =>
                        this.WriteElementInitializer(init),
                    fnWriteSeparator: () =>
                        this.WriteLine(",")
                    );
            }

            protected virtual void WriteExpressionList(IReadOnlyList<Expression> expressions)
            {
                this.WriteSeparated(
                    expressions,
                    fnWriteItem: expr =>
                        this.Write(expr),
                    fnWriteSeparator: () =>
                        this.WriteLine(",")
                    );
            }

            protected virtual void WriteMemberAssignment(MemberAssignment assignment)
            {
                this.Write(assignment.Member.Name);
                this.Write(" = ");
                this.Write(assignment.Expression);
            }

            protected virtual void WriteMethodCall(MethodCallExpression m)
            {
                WriteMethodCall(m.Method, m.Object, m.Arguments);
            }

            protected virtual void WriteMethodCall(
                MethodInfo method, 
                Expression? instance, 
                IReadOnlyList<Expression> arguments)
            {
                if (instance != null)
                {
                    this.Write(instance);

                    if (instance is MethodCallExpression
                        || instance is MemberExpression
                        || instance is InvocationExpression)
                        this.WriteLine();

                    this.Write(".");
                    this.Write(method.Name);
                    this.Write("(");
                    this.WriteArgumentList(arguments);
                    this.Write(")");
                }
                else if (method.IsExtensionMethod()
                    && arguments.Count > 0)
                {
                    WriteMethodCall(method, arguments.First(), arguments.Skip(1).ToReadOnly());
                }
                else
                {
                    this.Write(this.GetTypeName(method.DeclaringType));
                    this.Write(".");
                    this.Write(method.Name);
                    this.Write("(");
                    this.WriteArgumentList(arguments);
                    this.Write(")");
                }
            }

            protected virtual void WriteNew(NewExpression nex)
            {
                this.Write("new ");
                this.Write(this.GetTypeName(nex.Constructor.DeclaringType));
                this.Write("(");
                this.WriteIndented(() =>
                    this.WriteArgumentList(nex.Arguments));
                this.Write(")");
            }

            protected virtual void WriteNewArray(NewArrayExpression na)
            {
                this.Write("new ");
                this.Write(this.GetTypeName(TypeHelper.GetSequenceElementType(na.Type)));
                this.Write("[] {");
                this.WriteIndented(() =>
                    this.WriteArgumentList(na.Expressions));
                this.Write("}");
            }

            protected virtual void WriteParameter(ParameterExpression p)
            {
                this.Write(p.Name);
            }

            protected virtual void WriteRuntimeVariables(RuntimeVariablesExpression variables)
            {
                this.WriteLine();
                this.Write("[runtime_variables: ");
                this.WriteCommaSeparated(
                    variables.Variables,
                    this.WriteVariable);
                this.WriteLine("]");
            }

            protected virtual void WriteSwitch(SwitchExpression @switch)
            {
                this.WriteLine();
                this.Write("switch (");
                this.Write(@switch.SwitchValue);
                this.WriteLine(")");
                this.WriteLine("{");
                this.WriteIndented(() =>
                {
                    this.WriteLineSeparated(
                        @switch.Cases,
                        this.WriteSwitchCase
                        );
                });

                this.WriteLine("}");
            }

            protected virtual void WriteSwitchCase(SwitchCase @case)
            {
                this.Write("case ");
                this.WriteCommaSeparated(@case.TestValues, this.Write);
                this.WriteIndented(() =>
                {
                    this.WriteLine(":");
                    this.Write(@case.Body);
                });
            }

            protected virtual void WriteTry(TryExpression @try)
            {
                this.WriteLine();
                this.WriteLine("try");
                this.WriteBlockAlways(@try.Body);

                this.WriteLine();
                this.WriteLineSeparated(@try.Handlers, this.WriteCatchBlock);

                if (@try.Finally != null)
                {
                    this.WriteLine();
                    this.WriteLine("finally");
                    this.WriteBlockAlways(@try.Finally);
                }

                if (@try.Fault != null)
                {
                    this.WriteLine();
                    this.WriteLine("fault");
                    this.WriteBlockAlways(@try.Fault);
                }
            }

            protected virtual void WriteCatchBlock(CatchBlock catchBlock)
            {
                this.WriteLine();
                this.Write("catch (");              
                if (catchBlock.Variable != null)
                    this.WriteVariable(catchBlock.Variable);
                this.Write(")");

                if (catchBlock.Filter != null)
                {
                    this.Write(" when (");
                    this.Write(catchBlock.Filter);
                    this.Write(")");
                }

                this.WriteLine();
                this.WriteBlockAlways(catchBlock.Body);
            }

            protected virtual bool RequiresParenthesis(Expression ex)
            {
                switch (ex)
                {
                    case ConstantExpression _:
                    case ParameterExpression _:
                    case MethodCallExpression _:
                    case MemberExpression _:
                        return false;
                    default:
                        return true;
                }
            }

            protected virtual void WriteOperand(Expression ex)
            {
                if (RequiresParenthesis(ex))
                {
                    this.Write("(");
                    this.Write(ex);
                    this.Write(")");
                }
                else
                {
                    this.Write(ex);
                }
            }

            protected virtual void WriteUnary(UnaryExpression u)
            {
                switch (u.NodeType)
                {
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        if (u.Type != typeof(object)
                            && !u.Type.IsAssignableFrom(u.Operand.Type))
                        {
                            this.Write("(");
                            this.Write(this.GetTypeName(u.Type));
                            this.Write(")");
                            this.WriteOperand(u.Operand);
                        }
                        else
                        {
                            this.Write(u.Operand);
                        }
                        break;
                    case ExpressionType.ArrayLength:
                        this.WriteOperand(u.Operand);
                        this.Write(".Length");
                        break;
                    case ExpressionType.Quote:
                        this.Write(u.Operand);
                        break;
                    case ExpressionType.TypeAs:
                        this.WriteOperand(u.Operand);
                        this.Write(" as ");
                        this.Write(this.GetTypeName(u.Type));
                        break;
                    case ExpressionType.TypeIs:
                        this.WriteOperand(u.Operand);
                        this.Write(" is ");
                        this.Write(this.GetTypeName(u.Type));
                        break;
                    case ExpressionType.UnaryPlus:
                        this.Write(u.Operand);
                        break;
                    default:
                        if (this.GetPrefixOperator(u.NodeType) is { } opText)
                        {
                            this.Write(opText);
                            this.WriteOperand(u.Operand);
                        }
                        else
                        {
                            this.Write(u.NodeType.ToString());
                            this.Write("(");
                            this.Write(u.Operand);
                            this.Write(")");
                        }
                        break;
                }
            }

            private static bool IsTrivialArgument(Expression expression) =>
                expression is ConstantExpression
                || expression is ParameterExpression;

            protected virtual void WriteArgumentList(IReadOnlyList<Expression> arguments)
            {
                if (arguments.Count > 0)
                {
                    if (arguments.Count > 1   
                        || !IsTrivialArgument(arguments[0]))
                    {
                        this.WriteIndented(() =>
                        {
                            this.WriteLine();
                            this.WriteSeparated(
                                arguments,
                                fnWriteItem: expr =>
                                    this.Write(expr),
                                fnWriteSeparator: () =>
                                {
                                    this.WriteLine(",");
                                    this.WriteLine();

                                });
                            this.WriteLine();
                        });
                    }
                    else
                    {
                        this.Write(arguments[0]);
                    }
                }
            }

            protected virtual void WriteTypeBinary(TypeBinaryExpression b)
            {
                this.Write(b.Expression);
                this.Write(" is ");
                this.Write(this.GetTypeName(b.TypeOperand));
            }

            protected string GetTypeName(Type type)
            {
                string name = type.Name;
                name = name.Replace('+', '.');

                int iGeneneric = name.IndexOf('`');
                if (iGeneneric > 0)
                {
                    name = name.Substring(0, iGeneneric);
                }

                if (type.IsGenericType || type.IsGenericTypeDefinition)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(name);
                    sb.Append("<");
                    var args = type.GenericTypeArguments;
                    for (int i = 0, n = args.Length; i < n; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(",");
                        }

                        if (type.IsGenericType)
                        {
                            sb.Append(this.GetTypeName(args[i]));
                        }
                    }

                    sb.Append(">");
                    name = sb.ToString();
                }

                return name;
            }

            protected virtual string? GetPrefixOperator(ExpressionType type)
            {
                switch (type)
                {
                    case ExpressionType.Not:
                        return "!";
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        return "+";
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                        return "-";
                    default:
                        return null;
                }
            }

            protected virtual string? GetInfixOperator(ExpressionType type)
            {
                switch (type)
                {
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
                    case ExpressionType.And:
                        return "&";
                    case ExpressionType.AndAlso:
                        return "&&";
                    case ExpressionType.Or:
                        return "|";
                    case ExpressionType.OrElse:
                        return "||";
                    case ExpressionType.LessThan:
                        return "<";
                    case ExpressionType.LessThanOrEqual:
                        return "<=";
                    case ExpressionType.GreaterThan:
                        return ">";
                    case ExpressionType.GreaterThanOrEqual:
                        return ">=";
                    case ExpressionType.Equal:
                        return "==";
                    case ExpressionType.NotEqual:
                        return "!=";
                    case ExpressionType.Coalesce:
                        return "??";
                    case ExpressionType.RightShift:
                        return ">>";
                    case ExpressionType.LeftShift:
                        return "<<";
                    case ExpressionType.ExclusiveOr:
                        return "^";
                    case ExpressionType.Assign:
                        return "=";
                    case ExpressionType.AddAssign:
                    case ExpressionType.AddAssignChecked:
                        return "+=";
                    case ExpressionType.AndAssign:
                        return "&=";
                    case ExpressionType.DivideAssign:
                        return "/=";
                    case ExpressionType.ExclusiveOrAssign:
                        return "^=";
                    case ExpressionType.LeftShiftAssign:
                        return "<<=";
                    case ExpressionType.ModuloAssign:
                        return "%=";
                    case ExpressionType.MultiplyAssign:
                    case ExpressionType.MultiplyAssignChecked:
                        return "*=";
                    case ExpressionType.OrAssign:
                        return "|=";
                    case ExpressionType.PostDecrementAssign:
                        return "--=";
                    case ExpressionType.PostIncrementAssign:
                        return "++=";
                    case ExpressionType.PowerAssign:
                        return "^^=";
                    case ExpressionType.PreDecrementAssign:
                        return "--=";
                    case ExpressionType.PreIncrementAssign:
                        return "++=";
                    case ExpressionType.RightShiftAssign:
                        return ">>=";
                    case ExpressionType.SubtractAssign:
                    case ExpressionType.SubtractAssignChecked:
                        return "-=";
                    default:
                        return null;
                }
            }
        }
    }
}