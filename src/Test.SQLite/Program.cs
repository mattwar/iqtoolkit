// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Mapping;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var provider = DbEntityProvider.From("IQToolkit.Data.SQLite", @"Northwind.db3", "Test.NorthwindWithAttributes");

            //provider.Log = Console.Out;
            provider.Connection.Open();

            try
            {
                var db = new Northwind(provider);
                NorthwindExecutionTests.Run(db);
            }
            finally
            {
                provider.Connection.Close();
            }
        }
    }
}