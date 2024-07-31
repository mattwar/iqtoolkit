using System;
using System.Data;
using System.Data.Odbc;
using IQToolkit.Data;
using IQToolkit.Data.Access;
using IQToolkit.Data.Odbc;


namespace Test
{
    public static class TestProviders
    {
        public static IDbConnection CreateConnection()
        {
            var connectionString = AccessConnection.GetOdbcConnectionString("Northwind.accdb");
            return new OdbcConnection(connectionString);
        }

        public static EntityProvider CreateProvider(IDbConnection connection, EntityMapping mapping)
        {
            return new OdbcEntityProvider((OdbcConnection)connection)
                .WithLanguage(AccessLanguage.Singleton)
                .WithMapping(mapping);
        }
    }
}