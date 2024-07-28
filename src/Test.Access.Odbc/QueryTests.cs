using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Access;
using IQToolkit.Data.Mapping;

namespace Test.Access
{
    [TestClass]
    public class TestQuery : Test.TestBase
    {
#if false
        [TestMethod]
        public void TestSelectScalar()
        {
            TestQueryText(
                nw => nw.Customers.Select(e => e.City),
                """
                SELECT t0.[City]
                FROM [Customers] AS t0
                """
                );
        }

        [TestMethod]
        public void TestSelectConstructed()
        {
            // one column
            TestQueryText(
                nw => nw.Customers.Select(c => new { c.City }),
                """
                SELECT t0.[City]
                FROM [Customers] AS t0
                """
                );

            // two columns
            TestQueryText(
                nw => nw.Customers.Select(c => new { c.City, c.Phone }),
                """
                SELECT t0.[City], t0.[Phone]
                FROM [Customers] AS t0
                """
                );

            // three columns
            TestQueryText(
                nw => nw.Customers.Select(c => new { c.City, c.Phone, c.Country }),
                """
                SELECT t0.[City], t0.[Phone], t0.[Country]
                FROM [Customers] AS t0
                """
                );

            // identity
            TestQueryText(
                nw => nw.Customers.Select(c => c),
                """
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                """
                );

            // nested identity
            TestQueryText(
                nw => nw.Customers.Select(c => new { c.City, c }),
                """
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                """
                );

            // nested columns
            TestQueryText(
                nw => nw.Customers.Select(c => new { c.City, Country = new { c.Country } }),
                """
                SELECT t0.[City], t0.[Country]
                FROM [Customers] AS t0
                """
                );

            // empty
            TestQueryText(
                nw => nw.Customers.Select(c => new {  }),
                """
                SELECT 0
                FROM [Customers] AS t0
                """
                );

            // literal (same as empty)
            TestQueryText(
                nw => nw.Customers.Select(c => new { X = 10 }),
                """
                SELECT 0
                FROM [Customers] AS t0
                """
                );
        }

        [TestMethod]
        public void TestSelectConstant()
        {
            // constant number
            TestQueryText(
                nw => nw.Customers.Select(c => 10),
                """
                SELECT 0
                FROM [Customers] AS t0
                """
                );

            // constant string
            TestQueryText(
                nw => nw.Customers.Select(c => "ten"),
                """
                SELECT 0
                FROM [Customers] AS t0
                """
                );

            // constant null
            TestQueryText(
                nw => nw.Customers.Select(c => (string?)null),
                """
                SELECT 0
                FROM [Customers] AS t0
                """
                );

            // constant local
            int x = 10;
            TestQueryText(
                nw => nw.Customers.Select(c => x),
                """
                SELECT 0
                FROM [Customers] AS t0
                """
                );
        }

        [TestMethod]
        public void TestSelectTable()
        {
            TestQueryText(
                nw => nw.Customers,
                """
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                """
                );
        }

        [TestMethod]
        public void TestSelectNestedCollection()
        {
            //TestNorthwindQuery(
            //    nw => nw.Customers.Select(c => nw.Orders.Where(o => o.CustomerID == c.CustomerID).Select(o => o.OrderID)),
            //    """
            //    SELECT 0
            //    FROM [Customers] AS t0
            //    SELECT t0.[OrderID]
            //    FROM [Orders] AS t0
            //    WHERE (t0.[CustomerID] = A43022188?.[CustomerID])
            //    """
            //    );
        }


        [TestMethod]
        public void TestWhereTrueFalse()
        {
            TestQueryText(
                nw => nw.Customers.Where(c => true),
                """
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                WHERE -1 <> 0
                """
                );

            TestQueryText(
                nw => nw.Customers.Where(c => false),
                """
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                WHERE 0 <> 0
                """
                );
        }

        [TestMethod]
        public void TestWhereEquals()
        {
            TestQueryText(
                nw => nw.Customers.Where(c => c.City == "London"),
                """
                PARAMETERS p0 NVarChar;
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                WHERE (t0.[City] = p0)
                """
                );
        }

        [TestMethod]
        public void TestWhereEqualsConstructed()
        {
            var alfki = new Customer { CustomerID = "ALFK" };
            TestQueryText(
                nw => nw.Customers.Where(c => c == alfki),
                """
                PARAMETERS p0 NVarChar;
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                WHERE (t0.[CustomerID] = p0)
                """
                );

            TestQueryText(
                nw => nw.Customers.Where(c => c != alfki),
                """
                PARAMETERS p0 NVarChar;
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                WHERE NOT (t0.[CustomerID] = p0)
                """
                );

            TestQueryText(
                nw => nw.Customers.Where(c => new { x = c } == new { x = alfki }),
                """
                PARAMETERS p0 NVarChar;
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                WHERE (t0.[CustomerID] = p0)
                """
                );

            TestQueryText(
                nw => nw.Customers.Where(c => new { x = c.City, y = c.Country } == new { x = "London", y = "UK" }),
                """
                PARAMETERS p0 NVarChar, p1 NVarChar;
                SELECT t0.[City], t0.[CompanyName], t0.[ContactName], t0.[Country], t0.[CustomerID], t0.[Phone]
                FROM [Customers] AS t0
                WHERE ((t0.[City] = p0) AND (t0.[Country] = p1))
                """
                );
        }

        [TestMethod]
        public void TestWhereEqualsRelationship()
        {
            TestQueryText(
                nw => nw.Orders.Where(o => o.Customer == new Customer { CustomerID = "ALFK" }).Select(o => o.OrderID),
                """
                PARAMETERS p0 NVarChar;
                SELECT t0.[OrderID]
                FROM [Orders] AS t0
                LEFT OUTER JOIN [Customers] AS t1
                ON (t1.[CustomerID] = t0.[CustomerID])
                WHERE (t1.[CustomerID] = p0)
                """
                );

            TestQueryText(
                nw => nw.Orders.Where(o => o.Customer == null).Select(o => o.OrderID),
                """
                SELECT t0.[OrderID]
                FROM [Orders] AS t0
                LEFT OUTER JOIN [Customers] AS t1
                ON (t1.[CustomerID] = t0.[CustomerID])
                WHERE t1.[CustomerID] IS NULL
                """
                );

            TestQueryText(
                nw => nw.Orders.Where(o => null == o.Customer).Select(o => o.OrderID),
                """
                SELECT t0.[OrderID]
                FROM [Orders] AS t0
                LEFT OUTER JOIN [Customers] AS t1
                ON (t1.[CustomerID] = t0.[CustomerID])
                WHERE t1.[CustomerID] IS NULL
                """
                );

            TestQueryText(
                nw => nw.Orders.Where(o => o.Customer != null).Select(o => o.OrderID),
                """
                SELECT t0.[OrderID]
                FROM [Orders] AS t0
                LEFT OUTER JOIN [Customers] AS t1
                ON (t1.[CustomerID] = t0.[CustomerID])
                WHERE t1.[CustomerID] IS NOT NULL
                """
                );

            TestQueryText(
                nw => nw.Orders.Where(o => null != o.Customer).Select(o => o.OrderID),
                """
                SELECT t0.[OrderID]
                FROM [Orders] AS t0
                LEFT OUTER JOIN [Customers] AS t1
                ON (t1.[CustomerID] = t0.[CustomerID])
                WHERE t1.[CustomerID] IS NOT NULL
                """
                );
        }
#endif
    }
}