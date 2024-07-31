using System;
using System.Data;
using IQToolkit.Data;

namespace Test
{
    public static class TestProviders
    {
        public static IDbConnection CreateConnection()
        {
            throw new NotImplementedException();
        }

        public static EntityProvider CreateProvider(IDbConnection connection, EntityMapping mapping)
        {
            throw new NotImplementedException();
        }
    }
}