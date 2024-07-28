// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A batch of multiple commands/operations.
    /// </summary>
    public sealed class BatchExpression : Expression
    {
        public override Type Type { get; }
        public Expression Input { get; }
        public LambdaExpression Operation { get; }
        public Expression BatchSize { get; }
        public Expression Stream { get; }

        public BatchExpression(Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
        {
            this.Input = input;
            this.Operation = operation;
            this.BatchSize = batchSize;
            this.Stream = stream;
            this.Type = typeof(IEnumerable<>).MakeGenericType(operation.Body.Type);
        }

        public override ExpressionType NodeType => (ExpressionType)DbExpressionType.Batch;

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
    }
}
