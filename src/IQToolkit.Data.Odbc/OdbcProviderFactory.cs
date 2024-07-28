using System;
using System.Data.Odbc;

namespace IQToolkit.Data.Odbc
{
    using Factories;

    internal class OdbcProviderFactory : EntityProviderFactory
    {
        public OdbcProviderFactory()
        {
        }

        public static void Register() =>
            EntityProviderFactoryRegistry.Singleton.Register(typeof(OdbcProviderFactory)); 

        public override string Name => "Odbc";

        public override bool TryCreateProviderForConnection(string connectionString, out EntityProvider provider)
        {
            // looks like an ODBC connection string?
            if (connectionString.Contains("Driver=", StringComparison.OrdinalIgnoreCase))
            {
                var connection = new OdbcConnection(connectionString);
                provider = new OdbcQueryProvider(connection);
                return true;
            }

            provider = default!;
            return false;
        }
    }
}
