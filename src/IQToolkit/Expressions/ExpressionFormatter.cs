// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.IO;

namespace IQToolkit.Expressions
{
    public abstract class ExpressionFormatter
    {
        /// <summary>
        /// Write the <see cref="Expression"/> to a <see cref="TextWriter"/>.
        /// </summary>
        public abstract void Format(Expression expression, TextWriter writer, string indentation = "  ");

        /// <summary>
        /// Write the <see cref="Expression"/> to a string.
        /// </summary>
        public string Format(Expression expression, string indentation = "  ")
        {
            var stringWriter = new StringWriter();
            this.Format(expression, stringWriter, indentation);
            return stringWriter.ToString();
        }
    }
}