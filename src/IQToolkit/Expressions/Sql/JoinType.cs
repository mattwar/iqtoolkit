// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Expressions.Sql
{
    /// <summary>
    /// The kinds of SQL join operations.
    /// </summary>
    public enum JoinType
    {
        CrossApply,
        CrossJoin,
        InnerJoin,
        LeftOuterJoin,
        OuterApply,
        SingletonLeftOuterJoin
    }
}
