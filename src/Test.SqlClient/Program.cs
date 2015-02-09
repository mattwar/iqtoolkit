using System.Linq;
using System.Linq.Expressions;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Mapping;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var provider = DbEntityProvider.From(@"Northwind.mdf", "Test.NorthwindWithAttributes");

            //provider.Log = Console.Out;
            provider.Connection.Open();

            try
            {
                var db = new Northwind(provider);
                NorthwindExecutionTests.Run(db);
            }
            finally
            {
                provider.Connection.Close();
            }
        }
    }
}
