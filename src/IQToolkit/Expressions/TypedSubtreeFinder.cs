// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Expressions
{
    /// <summary>
    /// Finds the first sub-expression that is of a specified type
    /// </summary>
    public class TypedSubtreeFinder : ExpressionRewriter
    {
        private readonly Type _type;
        private Expression? _found;

        private TypedSubtreeFinder(Type type)
        {
            _type = type;
        }

        public static Expression? Find(Expression expression, Type type)
        {
            TypedSubtreeFinder finder = new TypedSubtreeFinder(type);
            finder.Rewrite(expression);
            return finder._found;
        }

        public override Expression Rewrite(Expression exp)
        {
            var node = base.Rewrite(exp);

            // remember the first sub-expression that is of an appropriate type
            if (_found == null && _type.IsAssignableFrom(node.Type))
            {
                _found = node;
            }

            return node;
        }
    }
}