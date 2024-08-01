// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit
{
    /// <summary>
    /// A default implementation of IQueryable for use with QueryProvider
    /// </summary>
    public class Query<T> 
        : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
    {
        private readonly IQueryProvider _provider;
        private readonly Expression _expression;

        public Query(IQueryProvider provider)
            : this(provider, null)
        {
        }

        public Query(IQueryProvider provider, Type? staticType)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("Provider");
            }

            _provider = provider;
            _expression = staticType != null ? Expression.Constant(this, staticType) : Expression.Constant(this);
        }

        public Query(QueryProvider provider, Expression expression)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("Provider");
            }

            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }

            _provider = provider;
            _expression = expression;
        }

        public Expression Expression => _expression;
        public Type ElementType => typeof(T);
        public IQueryProvider Provider => _provider;

        public IEnumerator<T> GetEnumerator() =>
            ((IEnumerable<T>)_provider.Execute(_expression)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable)_provider.Execute(_expression)).GetEnumerator();

        public override string ToString()
        {
            if (_expression.NodeType == ExpressionType.Constant &&
                ((ConstantExpression)_expression).Value == this)
            {
                return "Query(" + typeof(T) + ")";
            }
            else
            {
                return _expression.ToString();
            }
        }
    }
}
