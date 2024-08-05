// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Entities.Translation
{
    using Expressions.Sql;

    /// <summary>
    /// Converts all external input expressions into client parameters.
    /// </summary>
    public class ClientParameterRewriter : SqlExpressionVisitor
    {
        public static Expression Rewrite(QueryLanguage language, Expression expression)
        {
            return new ClientParameterRewriter(language).Visit(expression);
        }

        private readonly QueryLanguage _language;
        private readonly Dictionary<TypeAndValue, ClientParameterExpression> _map;
        private readonly Dictionary<HashedExpression, ClientParameterExpression> _pmap;

        private ClientParameterRewriter(QueryLanguage language)
        {
            _language = language;
            _map = new Dictionary<TypeAndValue, ClientParameterExpression>();
            _pmap = new Dictionary<HashedExpression, ClientParameterExpression>();
        }

        protected internal override Expression VisitClientProjection(ClientProjectionExpression proj)
        {
            // don't parameterize the projector or aggregator!
            var select = (SelectExpression)this.Visit(proj.Select);
            return proj.Update(select, proj.Projector, proj.Aggregator);
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert
                && u.Operand.NodeType == ExpressionType.ArrayIndex)
            {
                var b = (BinaryExpression)u.Operand;
                if (IsConstantOrParameter(b.Left)
                    && IsConstantOrParameter(b.Right))
                {
                    return this.GetClientParameter(u);
                }
            }

            return base.VisitUnary(u);
        }

        private static bool IsConstantOrParameter(Expression e)
        {
            return e.NodeType == ExpressionType.Constant || e.NodeType == ExpressionType.Parameter;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            var left = this.Visit(b.Left);
            var right = this.Visit(b.Right);

            if (left is ClientParameterExpression leftCP
                && right is ColumnExpression rightCE)
            {
                left = new ClientParameterExpression(leftCP.Name, rightCE.QueryType, leftCP.Value);
            }
            else if (right is ClientParameterExpression rightCP
                && left is ColumnExpression leftCE)
            {
                right = new ClientParameterExpression(rightCP.Name, leftCE.QueryType, rightCP.Value);
            }

            return b.Update(left, b.Conversion, right);
        }

        protected internal override ColumnAssignment VisitColumnAssignment(ColumnAssignment ca)
        {
            ca = base.VisitColumnAssignment(ca);
            var expression = ca.Expression;

            if (expression is ClientParameterExpression nv)
            {
                expression = new ClientParameterExpression(nv.Name, ca.Column.QueryType, nv.Value);
            }

            return ca.Update(ca.Column, expression);
        }

        private int _iParam = 0;

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value != null && !IsNumeric(c.Value.GetType()))
            {
                ClientParameterExpression cp;
                TypeAndValue tv = new TypeAndValue(c.Type, c.Value);
                if (!_map.TryGetValue(tv, out cp))
                { // re-use same name-value if same type & value
                    string name = "p" + (_iParam++);
                    cp = new ClientParameterExpression(name, _language.TypeSystem.GetQueryType(c.Type), c);
                    _map.Add(tv, cp);
                }

                return cp;
            }

            return c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return this.GetClientParameter(p);
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            m = (MemberExpression)base.VisitMember(m);

            if (m.Expression is ClientParameterExpression nv)
            {
                var x = Expression.MakeMemberAccess(nv.Value, m.Member);
                return GetClientParameter(x);
            }

            return m;
        }

        private Expression GetClientParameter(Expression expression)
        {
            var he = new HashedExpression(expression);

            if (!_pmap.TryGetValue(he, out var nv))
            {
                var name = "p" + (_iParam++);
                nv = new ClientParameterExpression(name, _language.TypeSystem.GetQueryType(expression.Type), expression);
                _pmap.Add(he, nv);
            }

            return nv;
        }

        private bool IsNumeric(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        struct TypeAndValue : IEquatable<TypeAndValue>
        {
            private readonly Type _type;
            private readonly object? _value;
            private readonly int _hash;

            public TypeAndValue(Type type, object? value)
            {
                _type = type;
                _value = value;
                _hash = type.GetHashCode() + value?.GetHashCode() ?? 0;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is TypeAndValue))
                    return false;
                return this.Equals((TypeAndValue)obj);
            }

            public bool Equals(TypeAndValue vt)
            {
                return vt._type == _type && object.Equals(vt._value, _value);
            }

            public override int GetHashCode()
            {
                return _hash;
            }
        }

        struct HashedExpression : IEquatable<HashedExpression>
        {
            private readonly Expression _expression;
            private readonly int _hashCode;

            public HashedExpression(Expression expression)
            {
                _expression = expression;
                _hashCode = Hasher.ComputeHash(expression);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is HashedExpression))
                    return false;
                return this.Equals((HashedExpression)obj);
            }

            public bool Equals(HashedExpression other)
            {
                return _hashCode == other._hashCode 
                    && SqlExpressionComparer.Default.Equals(_expression, other._expression);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            class Hasher : SqlExpressionVisitor
            {
                int hc;

                internal static int ComputeHash(Expression expression)
                {
                    var hasher = new Hasher();
                    hasher.Visit(expression);
                    return hasher.hc;
                }

                protected override Expression VisitConstant(ConstantExpression c)
                {
                    hc = hc + ((c.Value != null) ? c.Value.GetHashCode() : 0);
                    return c;
                }
            }
        }
    }
}
