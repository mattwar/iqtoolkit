// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Data;
using IQToolkit.Data.Access;
using IQToolkit.Data.Dynamic;
using IQToolkit.Data.Mapping;

namespace Test
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new TestRunner(args, System.Reflection.Assembly.GetEntryAssembly()).RunTests();
        }

        private static DbEntityProvider CreateNorthwindProvider()
        {
            return new AccessQueryProvider("Northwind.mdb", new AttributeMapping(typeof(Test.NorthwindWithAttributes)));
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

            // some API tests
            public void TestCreateFromDatabaseFile()
            {
                var provider = DynamicProvider.Create("Northwind.mdb");
                Assert.NotEqual(null, provider);
                Assert.Equal(true, provider is AccessQueryProvider);
            }

            public void TestCreateFromProviderName()
            {
                var provider = DynamicProvider.Create("IQToolkit.Data.Access", "Northwind.mdb");
                Assert.NotEqual(null, provider);
                Assert.Equal(true, provider is AccessQueryProvider);
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
