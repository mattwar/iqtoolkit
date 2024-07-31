using System.IO;

namespace IQToolkit.Access
{
    using Entities;
    using Entities.Factories;

    internal class AccessProviderFactory : EntityProviderFactory
    {
        public AccessProviderFactory()
        {
        }

        public override string Name => "Access";

        public override bool TryCreateProviderForFilePath(string filePath, out EntityProvider provider)
        {
            var extension = Path.GetExtension(filePath);
            if (extension == ".mdb" || extension == ".accdb")
            {
                // look for known entity providers that can handle Access files.
                if (EntityProviderFactoryRegistry.Singleton.TryGetFactory("Odbc", out var odbcFactory))
                {
                    var connectionString = AccessConnection.GetOdbcConnectionString(filePath);
                    if (odbcFactory.TryCreateProviderForConnection(connectionString, out provider))
                    {
                        provider = provider.WithLanguage(AccessLanguage.Singleton);
                        return true;
                    }
                }
                else if (EntityProviderFactoryRegistry.Singleton.TryGetFactory("OleDb", out var oledbFactory))
                {
                    var connectionString = AccessConnection.GetOleDbConnectionString(filePath);
                    if (oledbFactory.TryCreateProviderForConnection(connectionString, out provider))
                    {
                        provider = provider.WithLanguage(AccessLanguage.Singleton);
                        return true;
                    }
                }
            }

            provider = default!;
            return false;
        }
    }
}