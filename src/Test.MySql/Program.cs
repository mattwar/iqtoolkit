using System.Linq;
using IQToolkit.Data;
using IQToolkit.Data.MySqlClient;
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
            return new MySqlQueryProvider(
                "Server=localhost;user id='root';password='mypwd';Database=Northwind", 
                new AttributeMapping(typeof(Test.NorthwindWithAttributes)));
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

            public new void TestOr()
            {
                // difference in collation (mysql is matching "A" and "Å" but the others are not)
                var custs = db.Customers.Where(c => c.Country == "USA" || c.City.StartsWith("A")).Select(c => new { c.Country, c.City }).ToList();
                Assert.Equal(15, custs.Count);
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
