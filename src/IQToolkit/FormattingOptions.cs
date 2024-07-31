// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit
{
    /// <summary>
    /// Options to control formatting of query text.
    /// </summary>
    public class FormattingOptions
    {
        /// <summary>
        /// Parameters are formatted for ODBC
        /// </summary>
        public bool IsOdbc { get; }

        /// <summary>
        /// The formatting is intended for debugging purposes.
        /// </summary>
        public bool IsDebug { get; }

        /// <summary>
        /// The indentation text to use for each level of indentation.
        /// </summary>
        public string Indentation { get; }

        private FormattingOptions(
            bool odbc,
            bool debug,
            string indentation)
        {
            this.IsOdbc = odbc;
            this.IsDebug = debug;
            this.Indentation = indentation;
        }

        /// <summary>
        /// The default formatting options.
        /// </summary>
        public static readonly FormattingOptions Default =
            new FormattingOptions(false, false, "  ");

        /// <summary>
        /// The default formatting options with <see cref="IsDebug"/> property enabled.
        /// </summary>
        public static FormattingOptions DebugDefault =
            Default.WithIsDebug(true);

        protected FormattingOptions With(
            bool? odbc = null,
            bool? debug = null,
            string? indentation = null)
        {
            var newOdbc = odbc ?? this.IsOdbc;
            var newDebug = debug ?? this.IsDebug;
            var newIndentation = indentation ?? this.Indentation;

            if (newOdbc != this.IsOdbc
                || newDebug != this.IsDebug
                || newIndentation != this.Indentation)
            {
                return new FormattingOptions(newOdbc, newDebug, newIndentation);
            }

            return this;
        }

        /// <summary>
        /// Returns options with the <see cref="IsOdbc"/> property assigned.
        /// </summary>
        public FormattingOptions WithIsOdbc(bool enabled) =>
            With(odbc: enabled);

        /// <summary>
        /// Returns options with the <see cref="IsDebug"/> property assigned.
        /// </summary>
        public FormattingOptions WithIsDebug(bool enabled) =>
            With(debug: enabled);

        /// <summary>
        /// Returns options with the <see cref="Indentation"/> property assigned.
        /// </summary>
        public FormattingOptions WithIndenation(string indentation) =>
            With(indentation: indentation);
    }
}
