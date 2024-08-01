using System;
using System.Data.Odbc;

namespace IQToolkit.Odbc
{
    using Entities;
    using Entities.Factories;

    internal class OdbcProviderFactory : EntityProviderFactory
    {
        public OdbcProviderFactory()
        {
        }

        public static void Register() =>
            EntityProviderFactoryRegistry.Singleton.Register(typeof(OdbcProviderFactory)); 

        public override string Name => "Odbc";

        public override bool TryCreateProviderForConnection(string connectionString, out IEntityProvider provider)
        {
            // looks like an ODBC connection string?
            if (connectionString.Contains("Driver=", StringComparison.OrdinalIgnoreCase))
            {
                var connection = new OdbcConnection(connectionString);
                provider = new OdbcEntityProvider(connection);
                return true;
            }

            provider = default!;
            return false;
        }
    }
}
