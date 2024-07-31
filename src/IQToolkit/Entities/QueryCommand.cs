// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;

namespace IQToolkit.Entities
{
    using Utils;

    /// <summary>
    /// A command to be issued against the database.
    /// </summary>
    public class QueryCommand
    {
        /// <summary>
        /// The text of the command in the database's language.
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// The parameters the command requires when executed.
        /// </summary>
        public IReadOnlyList<QueryParameter> Parameters { get; }

        public QueryCommand(string commandText, IEnumerable<QueryParameter>? parameters = null)
        {
            this.CommandText = commandText;
            this.Parameters = parameters.ToReadOnly();
        }
    }
}
