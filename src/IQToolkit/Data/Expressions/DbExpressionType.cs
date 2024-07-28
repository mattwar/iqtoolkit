// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.Expressions
{
    /// <summary>
    /// Extended node types for <see cref="DbExpression"/> nodes.
    /// </summary>
    public enum DbExpressionType
    {
        Table               = 1000, // make sure these don't overlap with ExpressionType
        ClientJoin          = 1001,
        Column              = 1002,
        Select              = 1003,
        ClientProjection    = 1004,
        Entity              = 1005,
        Join                = 1006,
        Aggregate           = 1007,
        ScalarSubquery      = 1008,
        ExistsSubquery      = 1009,
        InSubquery          = 1010,
        Grouping            = 1011,
        Tagged              = 1012,
        IsNull              = 1013,
        Between             = 1014,
        RowNumber           = 1015,
        ClientParameter     = 1016,
        OuterJoined         = 1017,
        InsertCommand       = 1018,
        UpdateCommand       = 1019,
        DeleteCommand       = 1020,
        Batch               = 1021,
        Function            = 1022,
        Block               = 1023,
        IfCommand           = 1024,
        Declaration         = 1025,
        Variable            = 1026,
        DbLiteral           = 1027,
        DbBinary            = 1028,
        DbPrefixUnary       = 1029,
        InValues            = 1030
    }
}
