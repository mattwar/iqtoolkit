// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace IQToolkit.Entities
{
    using Utils;

    public class FormattedQuery
    {
        /// <summary>
        /// The formatted text of the query.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// A list of parameter expressions each time they are referenced in the formatted query,
        /// in the order they are referenced.
        /// </summary>
        public IReadOnlyList<Expression> ParameterReferences { get; }

        /// <summary>
        /// Any diagnostics determined during formatting.
        /// </summary>
        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        public FormattedQuery(
            string text,
            IEnumerable<Expression> parameterReferences,
            IEnumerable<Diagnostic> diagnostics)
        {
            this.Text = text;
            this.ParameterReferences = parameterReferences.ToReadOnly();
            this.Diagnostics = diagnostics.ToReadOnly();
        }
    }
}
