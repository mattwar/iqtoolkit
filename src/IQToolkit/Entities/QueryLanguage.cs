// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Entities
{
    /// <summary>
    /// Defines the language rules for a query provider.
    /// </summary>
    public abstract class QueryLanguage
    {
        /// <summary>
        /// The type system used by the language.
        /// </summary>
        public abstract QueryTypeSystem TypeSystem { get; }

        /// <summary>
        /// Gets the execution plan for the query expression.
        /// </summary>
        public abstract QueryPlan GetQueryPlan(
            Expression query, 
            IEntityProvider provider
            );

        /// <summary>
        /// Determines whether a given expression can be evaluated on the client. 
        /// It contains no parts that must be evaluated on the server.
        /// </summary>
        public virtual bool CanBeEvaluatedLocally(Expression expression)
        {
            // any operation on a query can't be done locally
            if (expression is ConstantExpression cex
                && cex.Value is IQueryable query
                && query.Provider == this)
            {
                return false;
            }

            if (expression is MethodCallExpression mc
                && (mc.Method.DeclaringType == typeof(Enumerable)
                    || mc.Method.DeclaringType == typeof(Queryable)
                    || mc.Method.DeclaringType == typeof(Updatable)))
            {
                return false;
            }

            if (expression.NodeType == ExpressionType.Convert
                && expression.Type == typeof(object))
            {
                return true;
            }

            return expression.NodeType != ExpressionType.Parameter
                && expression.NodeType != ExpressionType.Lambda;
        }
    }
}