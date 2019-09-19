// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Data;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SqlClient;
using System;

namespace Test
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new TestRunner(args, System.Reflection.Assembly.GetEntryAssembly()).RunTests();
        }

        private static DbEntityProvider CreateNorthwindProvider(Type contextType = null)
        {
            return new SqlQueryProvider("Northwind.mdf", new AttributeMapping(contextType ?? typeof(Test.NorthwindWithAttributes)));
        }

        public class NorthwindMappingTests : Test.NorthwindMappingTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
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

        public class MultiTableTests : Test.MultiTableTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider(typeof(Test.MultiTableContext));
            }
        }

#if !DEBUG
        public class NorthwindPerfTests : Test.NorthwindPerfTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
        }
#endif
    }
}
