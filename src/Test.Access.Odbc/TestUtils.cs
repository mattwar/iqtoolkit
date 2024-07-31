using System.Data.Odbc;
using IQToolkit.Access;
using IQToolkit.Entities;
using IQToolkit.Entities.Mapping;
using IQToolkit.Odbc;

namespace Test.Access
{
    public static class TestUtils
    {
        public static Northwind CreateNorthwind()
        {
            return new Northwind(CreateNorthwindProvider());
        }

        public static EntityProvider CreateNorthwindProvider()
        {
            var connectionString = AccessConnection.GetOdbcConnectionString("Northwind.mdb");
            var connection = new OdbcConnection(connectionString);
            return new OdbcEntityProvider(connection)
                .WithLanguage(AccessLanguage.Singleton)
                .WithMapping(new AttributeEntityMapping(typeof(NorthwindWithAttributes)));
        }
    }
}