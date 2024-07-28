using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Access;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.Odbc;
using System.Data.Odbc;

namespace Test
{
    [TestClass]
    public class SanityTests
    {
        [TestMethod]
        public void TestCreateForFilePath()
        {
            var provider = EntityProvider.CreateForFilePath("Northwind.mdb");
            Assert.IsNotNull(provider);

            var provider2 = EntityProvider.CreateForFilePath("Northwind.accdb");
            Assert.IsNotNull(provider2);
        }

        [TestMethod]
        public void TestExecuteOdbcSelect()
        {
            var connectionString = AccessConnection.GetOdbcConnectionString("Northwind.mdb");
            var connection = new OdbcConnection(connectionString);

            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT [ContactName] FROM [Customers] WHERE [CustomerID] = ?";
            command.Parameters.Add(new OdbcParameter(null, "ALFKI"));

            var reader = command.ExecuteReader();
            Dump(reader);

            connection.Close();
        }

        [TestMethod]
        public void TestExecuteOdbcSelect_Parameterized()
        {
            var connectionString = AccessConnection.GetOdbcConnectionString("Northwind.mdb");
            var connection = new OdbcConnection(connectionString);

            connection.Open();

            var command2 = connection.CreateCommand();
            command2.CommandText = "SELECT [ContactName] FROM [Customers] WHERE [CustomerID] = ?";
            command2.Parameters.Add(new OdbcParameter(null, "ALFKI"));

            var reader2 = command2.ExecuteReader();
            Dump(reader2);

            connection.Close();
        }

        [TestMethod]
        public void TestExecuteOdbcSelect_Parameterized_TooFewParameters()
        {
            var connectionString = AccessConnection.GetOdbcConnectionString("Northwind.mdb");
            var connection = new OdbcConnection(connectionString);

            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT TOP 1 t0.[CustomerID], t0.[OrderDate], t0.[OrderID]
                FROM [Orders] AS t0
                WHERE t0.[OrderDate] = ? AND Day(DateAdd('d',2,t0.[OrderDate])) = 27
                """;
            var parameter = new OdbcParameter();
            parameter.OdbcType = OdbcType.DateTime;
            parameter.Value = new DateTime(1997, 8, 25);
            command.Parameters.Add(parameter);

            try
            {
                var reader2 = command.ExecuteReader();
                Dump(reader2);
            }
            catch (Exception)
            {
                throw;
            }

            connection.Close();
        }


        [TestMethod]
        public void TestExecuteOdbcSelect_SubqueryInSelect()
        {
            var connectionString = AccessConnection.GetOdbcConnectionString("Northwind.mdb");
            var connection = new OdbcConnection(connectionString);

            connection.Open();

            var command2 = connection.CreateCommand();
            command2.CommandText = 
                """
                SELECT t0.[CustomerID], (SELECT COUNT(*)
                    FROM [Orders] AS t1
                    WHERE (t1.[CustomerID] = t0.[CustomerID]) AND ((t1.[OrderID] MOD 2) = 0)
                    ) AS [c0]
                FROM [Customers] AS t0
                WHERE t0.[CustomerID] = ?
                """;
            command2.Parameters.Add(new OdbcParameter(null, "ALFKI"));

            var reader2 = command2.ExecuteReader();
            Dump(reader2);

            connection.Close();
        }

        private static void Dump(OdbcDataReader reader)
        {
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (i > 0)
                        Console.Write(", ");
                    Console.Write($"{reader.GetName(i)}: {reader.GetValue(i)}");
                }

                Console.WriteLine();
            }

            reader.Close();
        }
    }
}