// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    public class SelfContainedReferencer : TableAliasScopeTracker
    {
        private bool _isSelfContained;

        public SelfContainedReferencer(IEnumerable<TableAlias>? declaredAliases)
            : base(declaredAliases)
        {
            _isSelfContained = true;
        }

        public static bool IsSelfContained(Expression expression, IEnumerable<TableAlias>? validAliases)
        {
            var referencer = new SelfContainedReferencer(validAliases);
            referencer.Rewrite(expression);
            return referencer._isSelfContained;
        }

        protected override Expression RewriteColumn(ColumnExpression original)
        {
            _isSelfContained |= this.IsInScope(original.Alias);
            return original;
        }
    }
}
