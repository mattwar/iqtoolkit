// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A batch of multiple commands/operations.
    /// Corresponds to the <see cref="Updatable.Batch{U, T, S}(IUpdatable{U}, IEnumerable{T}, Expression{Func{IUpdatable{U}, T, S}}, int, bool)"/> method.
    /// </summary>
    public sealed class BatchExpression : DbExpression
    {
        /// <summary>
        /// The collection of input items/instances.
        /// </summary>
        public Expression Input { get; }

        /// <summary>
        /// The operation applied to each item.
        /// </summary>
        public LambdaExpression Operation { get; }

        /// <summary>
        /// The number of operations to be batched together.
        /// </summary>
        public Expression BatchSize { get; }

        /// <summary>
        /// Boolean, whether to stream the results or buffer them.
        /// </summary>
        public Expression Stream { get; }

        public BatchExpression(
            Expression input, 
            LambdaExpression operation, 
            Expression batchSize, 
            Expression stream)
            : base(typeof(IEnumerable<>).MakeGenericType(operation.Body.Type))
        {
            this.Input = input;
            this.Operation = operation;
            this.BatchSize = batchSize;
            this.Stream = stream;
        }

        public override DbExpressionType DbNodeType => 
            DbExpressionType.Batch;

        public BatchExpression Update(
            Expression input, 
            LambdaExpression operation, 
            Expression batchSize, 
            Expression stream)
        {
            if (input != this.Input 
                || operation != this.Operation 
                || batchSize != this.BatchSize 
                || stream != this.Stream)
            {
                return new BatchExpression(input, operation, batchSize, stream);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DbExpressionVisitor dbVisitor)
                return dbVisitor.VisitBatch(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var operation = (LambdaExpression)visitor.Visit(this.Operation);
            var batchSize = visitor.Visit(this.BatchSize);
            var stream = visitor.Visit(this.Stream);
            return this.Update(this.Input, operation, batchSize, stream);
        }
    }
}
