// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit
{
    /// <summary>
    /// Options to control formatting of query text.
    /// </summary>
    public static class FormattingOptions
    {
        public static readonly Option<bool> IsOdbcOption = 
            new Option<bool>(nameof(IsOdbcOption), false);

        public static readonly Option<string> IndentationOption =
            new Option<string>(nameof(IndentationOption), "  ");

        public static bool IsOdbc(this Options options) =>
            options.GetOption(IsOdbcOption);

        public static Options WithIsOdbc(this Options options, bool isOdbc) =>
            options.WithOption(IsOdbcOption, isOdbc);

        public static string Indentation(this Options options) =>
            options.GetOption(IndentationOption);

        public static Options WithIndentation(this Options options, string indentation) =>
            options.WithOption(IndentationOption, indentation);
    }
}
