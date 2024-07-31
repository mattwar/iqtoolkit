// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;
using IQToolkit.Expressions;

namespace IQToolkit.Expressions.Sql
{
    using Utils;

    /// <summary>
    /// A join that will happen on the client.
    /// </summary>
    public sealed class ClientJoinExpression : SqlExpression
    {
        /// <summary>
        /// The projection of the joined data.
        /// </summary>
        public ClientProjectionExpression Projection { get; }

        /// <summary>
        /// The key in the containing query's data (will be client data at time it is executed).
        /// </summary>
        public IReadOnlyList<Expression> OuterKey { get; }

        /// <summary>
        /// The key in joined data (reference into the 'Projection')
        /// </summary>
        public IReadOnlyList<Expression> InnerKey { get; }

        public ClientJoinExpression(
            ClientProjectionExpression projection, 
            IEnumerable<Expression> outerKey, 
            IEnumerable<Expression> innerKey)
            : base(projection.Type)
        {
            this.OuterKey = outerKey.ToReadOnly();
            this.InnerKey = innerKey.ToReadOnly();
            this.Projection = projection;
        }

        public ClientJoinExpression Update(
            ClientProjectionExpression projection, 
            IEnumerable<Expression> outerKey, 
            IEnumerable<Expression> innerKey)
        {
            if (projection != this.Projection 
                || outerKey != this.OuterKey 
                || innerKey != this.InnerKey)
            {
                return new ClientJoinExpression(projection, outerKey, innerKey);
            }
            else
            {
                return this;
            }
        }

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is SqlExpressionVisitor dbVisitor)
                return dbVisitor.VisitClientJoin(this);
            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var projection = (ClientProjectionExpression)visitor.Visit(this.Projection);
            var outerKey = this.OuterKey.Rewrite(visitor);
            var innerKey = this.InnerKey.Rewrite(visitor);
            return this.Update(projection, outerKey, innerKey);
        }
    }
}
