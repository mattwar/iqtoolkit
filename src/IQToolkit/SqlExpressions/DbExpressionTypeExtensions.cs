// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.SqlExpressions
{
    public static class DbExpressionTypeExtensions
    {
        public static bool IsDbExpression(this ExpressionType et)
        {
            return ((int)et) >= 1000;
        }

        public static string GetNodeTypeName(this Expression e)
        {
            if (e is DbExpression d)
            {
                return d.DbNodeType.ToString();
            }
            else
            {
                return e.NodeType.ToString();
            }
        }
    }
}
