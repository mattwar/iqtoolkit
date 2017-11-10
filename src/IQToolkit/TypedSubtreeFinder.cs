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

namespace IQToolkit
{
    /// <summary>
    /// Finds the first sub-expression that is of a specified type
    /// </summary>
    public class TypedSubtreeFinder : ExpressionVisitor
    {
        private readonly Type type;
        private Expression found;

        private TypedSubtreeFinder(Type type)
        {
            this.type = type;
        }

        public static Expression Find(Expression expression, Type type)
        {
            TypedSubtreeFinder finder = new TypedSubtreeFinder(type);
            finder.Visit(expression);
            return finder.found;
        }

        protected override Expression Visit(Expression exp)
        {
            Expression node = base.Visit(exp);

            // remember the first sub-expression that is of an appropriate type
            if (this.found == null && node != null && this.type.GetTypeInfo().IsAssignableFrom(node.Type.GetTypeInfo()))
            {
                this.found = node;
            }

            return node;
        }
    }
}