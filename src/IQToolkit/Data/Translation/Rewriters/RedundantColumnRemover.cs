// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data.Translation
{
    using Expressions;

    /// <summary>
    /// Removes duplicate column declarations that refer to the same underlying column
    /// </summary>
    public class RedundantColumnRemover : DbExpressionVisitor
    {
        private readonly Dictionary<ColumnExpression, ColumnExpression> _map;

        public RedundantColumnRemover()
        {
            _map = new Dictionary<ColumnExpression, ColumnExpression>();
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            if (_map.TryGetValue(column, out var mapped))
            {
                return mapped;
            }

            return column;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression) base.VisitSelect(select);

            // look for redundant column declarations
            var cols = select.Columns.OrderBy(c => c.Name).ToList();
            var removed = new BitArray(select.Columns.Count);
            var anyRemoved = false;

            for (int i = 0, n = cols.Count; i < n - 1; i++)
            {
                var ci = cols[i];
                var cix = ci.Expression as ColumnExpression;
                var qt = cix != null ? cix.QueryType : ci.QueryType;
                var cxi = new ColumnExpression(ci.Expression.Type, qt, select.Alias, ci.Name);

                for (int j = i + 1; j < n; j++)
                {
                    if (!removed.Get(j))
                    {
                        ColumnDeclaration cj = cols[j];
                        if (SameExpression(ci.Expression, cj.Expression))
                        {
                            // any reference to 'j' should now just be a reference to 'i'
                            ColumnExpression cxj = new ColumnExpression(cj.Expression.Type, qt, select.Alias, cj.Name);
                            _map.Add(cxj, cxi);
                            removed.Set(j, true);
                            anyRemoved = true;
                        }
                    }
                }
            }

            if (anyRemoved)
            {
                var newDecls = new List<ColumnDeclaration>();
                
                for (int i = 0, n = cols.Count; i < n; i++)
                {
                    if (!removed.Get(i))
                    {
                        newDecls.Add(cols[i]);
                    }
                }

                select = select.WithColumns(newDecls);
            }

            return select;
        }

        bool SameExpression(Expression a, Expression b)
        {
            return 
                a == b
                || (a is ColumnExpression ca
                    && b is ColumnExpression cb
                    && ca.Alias == cb.Alias 
                    && ca.Name == cb.Name);
        }
    }
}