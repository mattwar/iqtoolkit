// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Returns a list of <see cref="SelectExpression"/> accessible from the source expression
    /// </summary>
    public class SelectGatherer : DbExpressionVisitor
    {
        List<SelectExpression> selects = new List<SelectExpression>();

        public static ReadOnlyCollection<SelectExpression> Gather(Expression expression)
        {
            var gatherer = new SelectGatherer();
            gatherer.Visit(expression);
            return gatherer.selects.AsReadOnly();
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            this.selects.Add(select);
            return select; // don't visit sub-queries
        }
    }
}