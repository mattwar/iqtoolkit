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
    public static class Program
    {
        public static void Main(string[] args)
        {
            new TestRunner(args, System.Reflection.Assembly.GetEntryAssembly()).RunTests();
        }

        private static DbEntityProvider CreateNorthwindProvider(string mapping = "Test.NorthwindWithAttributes")
        {
            return DbEntityProvider.From(@"Northwind40.sdf", mapping);
        }

        public class NorthwindTranslationTests : Test.NorthwindTranslationTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
        }

        public class NorthwindExecutionTests : Test.NorthwindExecutionTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
        }

        public class NorthwindCUDTests : Test.NorthwindCUDTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
        }
    }
}
