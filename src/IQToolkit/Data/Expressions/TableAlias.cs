// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.Expressions
{
    public sealed class TableAlias
    {
        private readonly int _sequenceId;
        private static int _nextSequenceId;

        public TableAlias()
        {
            _sequenceId = ++_nextSequenceId;
        }

        public int SequenceId => _sequenceId;

        public override string ToString()
        {
            return $"A{_sequenceId}";
        }
    }
}
