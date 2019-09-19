// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using IQToolkit.Data;
using IQToolkit.Data.Access;
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
    }
}
