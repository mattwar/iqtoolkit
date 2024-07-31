// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using IQToolkit.Expressions;

namespace IQToolkit.Entities
{
    using Expressions;
    using SqlExpressions;
    using Utils;

    /// <summary>
    /// Information about a query execution.
    /// </summary>
    public class QueryPlan
    {
        /// <summary>
        /// The transformed query expression in its executable form.
        /// </summary>
        public Expression Executor { get; }

        /// <summary>
        /// All diagnostics.
        /// </summary>
        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        public QueryPlan(
            Expression executor,
            IEnumerable<Diagnostic> diagnostics)
        {
            this.Executor = executor;
            this.Diagnostics = diagnostics.ToReadOnly();
        }

        private IReadOnlyList<QueryCommand>? _commands;

        /// <summary>
        /// The database commands that would be executed.
        /// </summary>
        public IReadOnlyList<QueryCommand> QueryCommands
        {
            get
            {
                if (_commands == null)
                {
                    var tmp = this.Executor
                        .FindAll<ConstantExpression>(c => c.Value is QueryCommand)
                        .Select(c => (QueryCommand)c.Value)
                        .ToReadOnly();

                    Interlocked.CompareExchange(ref _commands, tmp, null);
                }

                return _commands;
            }
        }

        private string? _queryText;

        /// <summary>
        /// The text of the combined database queries that would be executed.
        /// </summary>
        public string QueryText
        {
            get
            {
                if (_queryText == null)
                {
                    var tmp = string.Join("\n", this.QueryCommands.Select(c => c.CommandText));
                    Interlocked.CompareExchange(ref _queryText, tmp, null);
                }

                return _queryText;
            }
        }

#if DEBUG
        private string? _executorDebugText;

        /// <summary>
        /// The executor described via pseudo code.
        /// </summary>
        internal string ExecutorDebugText =>
            _executorDebugText ??= SqlExpressionDebugFormatter.Singleton.Format(this.Executor);
#endif
    }
}
