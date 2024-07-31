// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Expressions.Sql
{
    /// <summary>
    /// A SQL select query and the operations applied to the tabular results
    /// on the client to construct the output value for each row.
    /// </summary>
    public sealed class ClientProjectionExpression : SqlExpression
    {
        /// <summary>
        /// The SQL select query that is being executed.
        /// </summary>
        public SelectExpression Select { get; }

        /// <summary>
        /// The expression that constructs an output value for each row.
        /// </summary>
        public Expression Projector { get; }

        /// <summary>
        /// A option function that aggregates the results.
        /// </summary>
        public LambdaExpression? Aggregator { get; }

        public ClientProjectionExpression(
            SelectExpression source,
            Expression projector,
            LambdaExpression? aggregator = null)
            : base(
                  aggregator != null
                    ? aggregator.Body.Type
                    : typeof(IEnumerable<>).MakeGenericType(projector.Type))
        {
            this.Select = source;
            this.Projector = projector;
            this.Aggregator = aggregator;
        }

        public bool IsSingleton => 
            this.Aggregator?.Body.Type == this.Projector.Type;

        public ClientProjectionExpression Update(
            SelectExpression select, 
            Expression projector, 
            LambdaExpression? aggregator)
        {
            if (select != this.Select 
                || projector != this.Projector 
                || aggregator != this.Aggregator)
            {
                return new ClientProjectionExpression(select, projector, aggregator);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitClientProjection(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var select = (SelectExpression)visitor.Visit(this.Select);
            var projector = visitor.Visit(this.Projector);
            var aggregator = (LambdaExpression?)visitor.Visit(this.Aggregator);
            return this.Update(select, projector, aggregator);
        }
    }
}
