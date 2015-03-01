// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;

namespace Test
{
    public class TestFailureException : Exception
    {
        public TestFailureException(string message)
            : base(message)
        {
        }
    }
}

