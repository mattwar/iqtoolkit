// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit
{
    /// <summary>
    /// Options to control formatting of query text.
    /// </summary>
    public static class FormattingOptions
    {
        public static readonly QueryOption<bool> IsOdbcOption = 
            new QueryOption<bool>(nameof(IsOdbcOption), false);

        public static readonly QueryOption<string> IndentationOption =
            new QueryOption<string>(nameof(IndentationOption), "  ");

        public static bool IsOdbc(this QueryOptions options) =>
            options.GetOption(IsOdbcOption);

        public static QueryOptions WithIsOdbc(this QueryOptions options, bool isOdbc) =>
            options.WithOption(IsOdbcOption, isOdbc);

        public static string Indentation(this QueryOptions options) =>
            options.GetOption(IndentationOption);

        public static QueryOptions WithIndentation(this QueryOptions options, string indentation) =>
            options.WithOption(IndentationOption, indentation);
    }
}
