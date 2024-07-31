// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit
{
    /// <summary>
    /// Represents an error.
    /// </summary>
    public class Diagnostic
    {
        public string Message { get; }

        public Diagnostic(string message)
        {
            this.Message = message;
        }
    }
}
