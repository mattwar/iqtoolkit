// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// A kind of SQL join
    /// </summary>
    public enum JoinType
    {
        CrossJoin,
        InnerJoin,
        CrossApply,
        OuterApply,
        LeftOuterJoin,
        SingletonLeftOuterJoin
    }
}
