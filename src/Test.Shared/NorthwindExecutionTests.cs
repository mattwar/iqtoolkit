using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using IQToolkit.Entities;
using IQToolkit.Entities.Mapping;
using IQToolkit;

namespace Test
{
#if !SHAREDLIB  // so this library does not show up as a test library
    [TestClass]
#endif
    public partial class NorthwindExecutionTests
    {
        private static object _dbLock = new object();
        private static IDbConnection? _connection;

        protected void TestProvider(
            Action<EntityProvider> fnTest,
            EntityMapping mapping)
        {
            // force serialization of integration tests
            lock (_dbLock)
            {
                if (_connection == null)
                {
                    _connection = TestProviders.CreateConnection();
                    // keep connection open because rapid open-close of connection slows down tests
                    _connection.Open();
                }

                var provider = TestProviders.CreateProvider(_connection, mapping);
                fnTest(provider);
            }
        }

        private static readonly EntityMapping _defaultNorthwindMapping =
            new AttributeEntityMapping(typeof(NorthwindWithAttributes));

        protected void TestNorthwind(
            Action<Northwind> fnTest,
            EntityMapping? mapping = null)
        {
            TestProvider(
                provider => fnTest(new Northwind(provider)),
                mapping ?? _defaultNorthwindMapping
                );
        }

        #region Query Compiler

        [TestMethod]
        public void TestCompiler_Where()
        {
            TestNorthwind(db =>
            {
                var fn = QueryCompiler.Compile(
                    (string id) => 
                        db.Customers.Where(c => c.CustomerID == id)
                    );

                var items = fn("ALKFI").ToList();
            });
        }

        [TestMethod]
        public void TestCompiler_SingleOrDefault()
        {
            TestNorthwind(db =>
            {
                var fn = QueryCompiler.Compile(
                    (string id) => 
                        db.Customers.SingleOrDefault(c => c.CustomerID == id)
                    );

                var cust = fn("ALFKI");
                Assert.IsNotNull(cust);
            });
        }

        [TestMethod]
        public void TestCompiler_Count()
        {
            TestNorthwind(db =>
            {
                var fn = QueryCompiler.Compile(
                    (string id) => 
                        db.Customers.Count(c => c.CustomerID == id)
                    );

                int n = fn("ALFKI");
                Assert.AreEqual(1, n);
            });
        }

        [TestMethod]
        public void TestCompiler_Parameterized()
        {
            TestNorthwind(db =>
            {
                var fn = QueryCompiler.Compile(
                    (Northwind n, string id) => 
                        n.Customers
                        .Where(c => c.CustomerID == id));

                var items = fn(db, "ALFKI").ToList();
                Assert.AreEqual(1, items.Count);
            });
        }

        [TestMethod]
        public void TestCompiler_ParameterizedWithHeirarchy()
        {
            TestNorthwind(db =>
            {
                var fn = QueryCompiler.Compile(
                        (Northwind n, string id) => 
                            n.Customers
                            .Where(c => c.CustomerID == id)
                            .Select(c => n.Orders.Where(o => o.CustomerID == c.CustomerID))
                        );

                var items = fn(db, "ALFKI").ToList();
                Assert.AreEqual(1, items.Count);
            });
        }

        #endregion

        #region Query Operators

        [TestMethod]
        public void TestOperator_ToList()
        {
            // returns all entities from table
            TestNorthwind(db =>
            {
                var list = db.Customers.ToList();

                Assert.AreEqual(91, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Where_SimplePredicate()
        {
            // simple predicate
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City == "London")
                    .ToList();

                Assert.AreEqual(6, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Where_True()
        {
            // true literal
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => true)
                    .ToList();

                Assert.AreEqual(91, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Where_Entity_Equals()
        {
            // entity equal
            TestNorthwind(db =>
            {
                var alfki = new Customer { CustomerID = "ALFKI" };

                var list = db.Customers
                    .Where(c => c == alfki)
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual("ALFKI", list[0].CustomerID);
            });
        }

        [TestMethod]
        public void TestOperator_Where_Entity_NotEquals()
        {
            // entity not equal
            TestNorthwind(db =>
            {
                var alfki = new Customer { CustomerID = "ALFKI" };

                var list = db.Customers
                    .Where(c => c != alfki)
                    .ToList();

                Assert.AreEqual(90, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Where_Constructed_Equals()
        {
            // constructed values equal
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => new { x = c.City } == new { x = "London" })
                    .ToList();

                Assert.AreEqual(6, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Where_Constructed_Equals_Multiple()
        {
            // constructed values with multiple properties equal
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => new { x = c.City, y = c.Country } == new { x = "London", y = "UK" })
                    .ToList();

                Assert.AreEqual(6, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Where_Constructed_NotEquals()
        {
            // constructed values with multiple properties not equal
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => new { x = c.City, y = c.Country } != new { x = "London", y = "UK" })
                    .ToList();

                Assert.AreEqual(85, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Where_Relationship_Count()
        {
            // relationship count
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.Orders.Count > 0)
                    .ToList();

                Assert.AreEqual(89, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Select()
        {
            // one column
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City == "London")
                    .Select(c => c.City)
                    .ToList();

                Assert.AreEqual(6, list.Count);
                Assert.AreEqual("London", list[0]);
                Assert.IsTrue(list.All(x => x == "London"));
            });

            // literal value
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Select(c => 10)
                    .ToList();

                Assert.AreEqual(91, list.Count);
                Assert.IsTrue(list.All(x => x == 10));
            });

            // literal null string
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Select(c => (string?)null)
                    .ToList();

                Assert.AreEqual(91, list.Count);
                Assert.IsTrue(list.All(x => x == null));
            });


            // constructed value with one column
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City == "London")
                    .Select(c => new { c.City })
                    .ToList();

                Assert.AreEqual(6, list.Count);
                Assert.AreEqual("London", list[0].City);
                Assert.IsTrue(list.All(x => x.City == "London"));
            });

            // constructed value from two columns
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City == "London")
                    .Select(c => new { c.City, c.Phone })
                    .ToList();

                Assert.AreEqual(6, list.Count);
                Assert.AreEqual("London", list[0].City);
                Assert.IsTrue(list.All(x => x.City == "London"));
                Assert.IsTrue(list.All(x => x.Phone != null));
            });

            // constructed with entity member
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City == "London")
                    .Select(c => new { c.City, c })
                    .ToList();

                Assert.AreEqual(6, list.Count);
                Assert.AreEqual("London", list[0].City);
                Assert.IsTrue(list.All(x => x.City == "London"));
                Assert.IsTrue(list.All(x => x.c.City == x.City));
            });

            // constructed w/ literal value
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City == "London")
                    .Select(c => new { X = 10 })
                    .ToList();

                Assert.AreEqual(6, list.Count);
                Assert.IsTrue(list.All(x => x.X == 10));
            });

            // local value
            TestNorthwind(db =>
            {
                int x = 10;
                var list = db.Customers
                    .Select(c => x)
                    .ToList();

                Assert.AreEqual(91, list.Count);
                Assert.IsTrue(list.All(y => y == 10));
            });

            // with manually joined collection
            TestNorthwind(db =>
            {
                var list = (
                    from c in db.Customers
                    where c.CustomerID == "ALFKI"
                    select db.Orders
                             .Where(o => o.CustomerID == c.CustomerID)
                             .Select(o => o.OrderID)
                    ).ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0].Count());
            });

            // constructed value with manually joined collection
            TestNorthwind(db =>
            {
                var list = (
                    from c in db.Customers
                    where c.CustomerID == "ALFKI"
                    select new
                    {
                        Foos = db.Orders
                               .Where(o => o.CustomerID == c.CustomerID)
                               .Select(o => o.OrderID)
                               .ToList()
                    }
                    ).ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0].Foos.Count);
            });
        }

        [TestMethod]
        public void TestOperator_SelectMany()
        {
            // joined manually
            TestNorthwind(db =>
            {
                var cods = db.Customers
                    .SelectMany(
                        c => db.Orders.Where(o => o.CustomerID == c.CustomerID),
                        (c, o) => new {c.ContactName, o.OrderDate })
                    .ToList();

                Assert.AreEqual(830, cods.Count);
            });

            // joined manually - default if empty
            TestNorthwind(db =>
            {
                var cods = db.Customers
                    .SelectMany(
                        c => db.Orders.Where(o => o.CustomerID == c.CustomerID).DefaultIfEmpty(),
                        (c, o) => new { c.ContactName, o.OrderDate }
                        )
                    .ToList();

                Assert.AreEqual(832, cods.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Join_SingleKey()
        {
            // manual join customers and orders
            TestNorthwind(db =>
            {
                var list = (
                    from c in db.Customers
                    where c.CustomerID == "ALFKI"
                    join o in db.Orders on c.CustomerID equals o.CustomerID
                    select new { c.ContactName, o.OrderID }
                    ).ToList();

                Assert.AreEqual(6, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Join_MultiKey()
        {
            // multi-key join
            TestNorthwind(db =>
            {
                var list = (
                    from c in db.Customers
                    where c.CustomerID == "ALFKI"
                    join o in db.Orders on new { a = c.CustomerID, b = c.CustomerID } equals new { a = o.CustomerID, b = o.CustomerID }
                    select new { c, o }
                    ).ToList();

                Assert.AreEqual(6, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Join_Into_Count()
        {
            // into collection, count in select
            TestNorthwind(db =>
            {
                var list = (
                    from c in db.Customers
                    where c.CustomerID == "ALFKI"
                    join o in db.Orders on c.CustomerID equals o.CustomerID into ords
                    select new { cust = c, ords = ords.Count() }
                    ).ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0].ords);
            });
        }

        [TestMethod]
        public void TestOperator_Join_Into_DefaultIfEmtpy()
        {
            // into collection, DefaultIfEmpty in selectmany
            TestNorthwind(db =>
            {
                var list = (
                    from c in db.Customers
                    where c.CustomerID == "PARIS"
                    join o in db.Orders on c.CustomerID equals o.CustomerID into ords
                    from o in ords.DefaultIfEmpty()
                    select new { c, o }
                    ).ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(null, list[0].o);
            });
        }

        [TestMethod]
        public void TestOperator_Join_Implicit()
        {
            // implicit join with multiple join conditions in where
            TestNorthwind(db =>
            {
                // this should reduce to inner joins
                var list = (
                    from c in db.Customers
                    from o in db.Orders
                    from d in db.OrderDetails
                    where o.CustomerID == c.CustomerID && o.OrderID == d.OrderID
                    where c.CustomerID == "ALFKI"
                    select d
                    ).ToList();

                Assert.AreEqual(12, list.Count);
            });
        }

#if !MYSQL
        [TestMethod]
        public void TestOperator_Join_Implicit_NoCondition()
        {
            // implicit join w/ missing join condition
            TestNorthwind(db =>
            {
                // this should force a naked cross join
                var list = (
                    from c in db.Customers
                    from o in db.Orders
                    from d in db.OrderDetails
                    where o.CustomerID == c.CustomerID /*&& o.OrderID == d.OrderID*/
                    where c.CustomerID == "ALFKI"
                    select d
                    ).ToList();

                Assert.AreEqual(12930, list.Count);
            });
        }
#endif

        [TestMethod]
        public void TestOperator_OrderBy()
        {
            // one orderby
            TestNorthwind(db =>
            {
                var dbSorted = db.Customers
                    .OrderBy(c => c.CustomerID)
                    .Select(c => c.CustomerID)
                    .ToList();

                var localSorted = dbSorted
                    .OrderBy(c => c)
                    .ToList();

                Assert.AreEqual(91, dbSorted.Count);
                Assert.IsTrue(Enumerable.SequenceEqual(dbSorted, localSorted));
            });

            // multiple orderbys
            TestNorthwind(db =>
            {
                var dbSorted = db.Customers
                    .OrderBy(c => c.Phone)
                    .OrderBy(c => c.CustomerID)
                    .ToList();

                var localSorted = dbSorted
                    .OrderBy(c => c.CustomerID)
                    .ToList();

                Assert.AreEqual(91, dbSorted.Count);
                Assert.IsTrue(Enumerable.SequenceEqual(dbSorted, localSorted));
            });

            // before thenby
            TestNorthwind(db =>
            {
                var dbSorted = db.Customers
                    .OrderBy(c => c.CustomerID)
                    .ThenBy(c => c.Phone)
                    .ToList();

                var localSorted = dbSorted
                    .OrderBy(c => c.CustomerID)
                    .ThenBy(c => c.Phone)
                    .ToList();

                Assert.AreEqual(91, dbSorted.Count);
                Assert.IsTrue(Enumerable.SequenceEqual(dbSorted, localSorted));
            });

            // descending
            TestNorthwind(db =>
            {
                var dbSorted = db.Customers
                    .OrderByDescending(c => c.CustomerID)
                    .ToList();

                var localSorted = dbSorted
                    .OrderByDescending(c => c.CustomerID)
                    .ToList();

                Assert.AreEqual(91, dbSorted.Count);
                Assert.IsTrue(Enumerable.SequenceEqual(dbSorted, localSorted));
            });

            // descending - before thenby
            TestNorthwind(db =>
            {
                var dbSorted = db.Customers
                    .OrderByDescending(c => c.CustomerID)
                    .ThenBy(c => c.Country)
                    .ToList();

                var localSorted = dbSorted
                    .OrderByDescending(c => c.CustomerID)
                    .ThenBy(c => c.Country)
                    .ToList();

                Assert.AreEqual(91, dbSorted.Count);
                Assert.IsTrue(Enumerable.SequenceEqual(dbSorted, localSorted));
            });

            // descending - before thenby descending
            TestNorthwind(db =>
            {
                var dbSorted = db.Customers
                    .OrderByDescending(c => c.CustomerID)
                    .ThenByDescending(c => c.Country)
                    .ToList();

                var localSorted = dbSorted
                    .OrderByDescending(c => c.CustomerID)
                    .ThenByDescending(c => c.Country)
                    .ToList();

                Assert.AreEqual(91, dbSorted.Count);
                Assert.IsTrue(Enumerable.SequenceEqual(dbSorted, localSorted));
            });

            // also orderby in cross join
            TestNorthwind(db =>
            {
                var dbSorted = (
                    from c in db.Customers.OrderBy(c => c.CustomerID)
                    join o in db.Orders.OrderBy(o => o.OrderID) on c.CustomerID equals o.CustomerID
                    select new { c.CustomerID, o.OrderID }
                    ).ToList();

                var localSorted = dbSorted
                    .OrderBy(x => x.CustomerID)
                    .ThenBy(x => x.OrderID)
                    .ToList();

                Assert.IsTrue(Enumerable.SequenceEqual(dbSorted, localSorted));
            });

            // also orderby in selectmany
            TestNorthwind(db =>
            {
                var dbSorted = (
                    from c in db.Customers.OrderBy(c => c.CustomerID)
                    from o in db.Orders.OrderBy(o => o.OrderID)
                    where c.CustomerID == o.CustomerID
                    select new { c.CustomerID, o.OrderID }
                    ).ToList();

                var localSorted = dbSorted
                    .OrderBy(x => x.CustomerID)
                    .ThenBy(x => x.OrderID)
                    .ToList();

                Assert.IsTrue(Enumerable.SequenceEqual(dbSorted, localSorted));
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_OneColumn()
        {
            // one column
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .GroupBy(c => c.City)
                    .ToList();

                Assert.AreEqual(69, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_AfterWhere()
        {
            // one column - after where
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City == "London")
                    .GroupBy(c => c.City)
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0].Count());
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_SelectMany()
        {
            // before SelectMany
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .GroupBy(c => c.City)
                    .SelectMany(g => g).
                    ToList();

                Assert.AreEqual(91, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_Select_Sum()
        {
            // select w/ group sum
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID)
                    .Select(g => g.Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1)))
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0]);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_Select_Count()
        {
            // select w/ group count
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID)
                    .Select(g => g.Count())
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0]);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_Select_LongCount()
        {
            // selct w/ group long-count
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID)
                    .Select(g => g.LongCount())
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6L, list[0]);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_Select_MultipleAggregates()
        {
            // select w/ multiple aggregates
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID)
                    .Select(g => new
                    {
                        Sum = g.Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1)),
                        Min = g.Min(o => o.OrderID),
                        Max = g.Max(o => o.OrderID),
                        Avg = g.Average(o => o.OrderID)
                    })
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0].Sum);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_ElementSelector()
        {
            // selector
            TestNorthwind(db =>
            {
                // note: groups are retrieved through a separately executed subquery per row
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1))
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0].Count());
                Assert.AreEqual(6, list[0].Sum());
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_ElementSelector_Select_Sum()
        {
            // selector and select w/ group sum
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1))
                    .Select(g => g.Sum())
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0]);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_ResultSelector_MultipleAggregates()
        {
            // selector w/ multiple group aggregates
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID, (k, g) =>
                        new
                        {
                            Sum = g.Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1)),
                            Min = g.Min(o => o.OrderID),
                            Max = g.Max(o => o.OrderID),
                            Avg = g.Average(o => o.OrderID)
                        })
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0].Sum);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_ElementSelector_Select()
        {
            // selector and select with multiple group aggregates
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1))
                    .Select(g => new { Sum = g.Sum(), Max = g.Max() })
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0].Sum);
                Assert.AreEqual(1, list[0].Max);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_Select_Constructed()
        {
            // selector with constructed value
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID, o => new { X = (o.CustomerID == "ALFKI" ? 1 : 1) })
                    .Select(g => g.Sum(x => x.X))
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(6, list[0]);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_MultiPartKey()
        {
            // constructed multi-part key
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => new { o.CustomerID, o.OrderDate })
                    .Select(g => g.Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1)))
                    .ToList();

                Assert.AreEqual(6, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_AfterWhere_JoinedCollectionCount()
        {
            // previous joined collection count in where
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(a => a.Orders.Count() > 15)
                    .GroupBy(a => a.City)
                    .ToList();

                Assert.AreEqual(9, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_GroupBy_AfterOrderBy()
        {
            // previous orderby
            TestNorthwind(db =>
            {
                // note: order-by is lost when group-by is applied (the sequence of groups is not ordered)
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .OrderBy(o => o.OrderID)
                    .GroupBy(o => o.CustomerID)
                    .ToList();

                Assert.AreEqual(1, list.Count);
                var grp = list[0].ToList();
                var sorted = grp.OrderBy(o => o.OrderID);

                Assert.AreEqual(true, Enumerable.SequenceEqual(grp, sorted));
            });
        }

            [TestMethod]
        public void TestOperator_GroupBy_AfterWhereAndOrderBy()
        {
            // before selectmany - after where and orderby
            TestNorthwind(db =>
            {
                var dbSorted = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .OrderBy(o => o.OrderID)
                    .GroupBy(o => o.CustomerID)
                    .SelectMany(g => g)
                    .ToList();

                var clientSorted = dbSorted.OrderBy(o => o.OrderID).ToList();

                Assert.AreEqual(6, dbSorted.Count);
                Assert.AreEqual(true, Enumerable.SequenceEqual(dbSorted, clientSorted));
            });

        }

        [TestMethod]
        public void TestOperator_Sum()
        {
            // on query
            TestNorthwind(db =>
            {
                var sum = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .Select(o => (o.CustomerID == "ALFKI" ? 1 : 1))
                    .Sum();

                Assert.AreEqual(6, sum);
            });

            // on query with predicte
            TestNorthwind(db =>
            {
                var sum = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1));

                Assert.AreEqual(6, sum);
            });
        }

        [TestMethod]
        public void TestOperator_Count()
        {
            // on query
            TestNorthwind(db =>
            {
                var count = db.Orders.Count();

                Assert.AreEqual(830, count);
            });

            // with predicate
            TestNorthwind(db =>
            {
                var count = db.Orders.Count(o => o.CustomerID == "ALFKI");

                Assert.AreEqual(6, count);
            });
        }

        [TestMethod]
        public void TestOperator_Distinct()
        {
            // entire entity distinct
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Distinct()
                    .ToList();

                Assert.AreEqual(91, list.Count);
            });

            // one column
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Select(c => c.City)
                    .Distinct()
                    .ToList();

                Assert.AreEqual(69, list.Count);
            });

            // two columns
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Select(c => c.CustomerID == "ALFKI" ? 1 : 0)
                    .Distinct()
                    .ToList();

                Assert.AreEqual(2, list.Count);
            });

#if false
            // ordering doesn't make sense here: Distinct operator is not guaranteed to retain ordering.
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City.StartsWith("P"))
                    .OrderBy(c => c.City)
                    .Select(c => c.City)
                    .Distinct()
                    .ToList();

                var sorted = list.OrderBy(x => x).ToList();

                Assert.AreEqual(list[0], sorted[0]);
                Assert.AreEqual(list[list.Count - 1], sorted[list.Count - 1]);
            });
#endif

            // before orderby
            TestNorthwind(db =>
            {
                var dbSorted = db.Customers
                    .Where(c => c.City.StartsWith("P"))
                    .Select(c => c.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                var clientSorted = dbSorted.OrderBy(x => x).ToList();

                Assert.AreEqual(true, Enumerable.SequenceEqual(dbSorted, clientSorted));
            });

            // before groupby
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .Distinct()
                    .GroupBy(o => o.CustomerID)
                    .ToList();

                Assert.AreEqual(1, list.Count);
            });

            // after groupby
            TestNorthwind(db =>
            {
                // distinct after group-by should not do anything
                var list = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID)
                    .Distinct()
                    .ToList();

                Assert.AreEqual(1, list.Count);
            });

            // count
            TestNorthwind(db =>
            {
                var cnt = db.Customers.Distinct().Count();
                Assert.AreEqual(91, cnt);
            });

            // after select - before count
            TestNorthwind(db =>
            {
                var cnt = db.Customers
                    .Select(c => c.City)
                    .Distinct()
                    .Count();

                Assert.AreEqual(69, cnt);
            });

            // after multiple selects - before count
            TestNorthwind(db =>
            {
                var cnt = db.Customers
                    .Select(c => c.City)
                    .Select(c => c)
                    .Distinct()
                    .Count();

                Assert.AreEqual(69, cnt);
            });

            // before count w/ predicate
            TestNorthwind(db =>
            {
                var cnt = db.Customers
                    .Select(c => new { c.City, c.Country })
                    .Distinct()
                    .Count(c => c.City == "London");

                Assert.AreEqual(1, cnt);
            });

            // before sum w/ arg
            TestNorthwind(db =>
            {
                var sum = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .Distinct()
                    .Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1));

                Assert.AreEqual(6, sum);
            });

            // before sum w/o arg
            TestNorthwind(db =>
            {
                var sum = db.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .Select(o => o.OrderID)
                    .Distinct()
                    .Sum();

                Assert.AreEqual(64835, sum);
            });
        }

        [TestMethod]
        public void TestOperator_Take()
        {
            // no ordering
            TestNorthwind(db =>
            {
                var list = db.Orders
                    .Take(5)
                    .ToList();

                Assert.AreEqual(5, list.Count);
            });

            // after ordering and before distinct
            TestNorthwind(db =>
            {
                // distinct must be forced to apply after top has been computed
                var list = db.Orders
                    .OrderBy(o => o.CustomerID)
                    .Select(o => o.CustomerID)
                    .Take(5)
                    .Distinct()
                    .ToList();

                Assert.AreEqual(1, list.Count);
            });

            // after ordering and distinct
            TestNorthwind(db =>
            {
                // top must be forced to apply after distinct has been computed
                var list = db.Orders
                    .OrderBy(o => o.CustomerID)
                    .Select(o => o.CustomerID)
                    .Distinct()
                    .Take(5)
                    .ToList();

                Assert.AreEqual(5, list.Count);
            });

#if !ACCESS
            // after distance and order by w/ count
            TestNorthwind(db =>
            {
                var cnt = db.Orders
                    .Distinct()
                    .OrderBy(o => o.CustomerID)
                    .Select(o => o.CustomerID)
                    .Take(5)
                    .Count();

                Assert.AreEqual(5, cnt);
            });
#endif

            // after orderby and before distinct and count
            TestNorthwind(db =>
            {
                var cnt = db.Orders
                    .OrderBy(o => o.CustomerID)
                    .Select(o => o.CustomerID)
                    .Take(5)
                    .Distinct()
                    .Count();

                Assert.AreEqual(1, cnt);
            });
        }

        [TestMethod]
        public void TestOperator_First()
        {
            // no predicate - after orderby
            TestNorthwind(db =>
            {
                var first = db.Customers
                    .OrderBy(c => c.ContactName)
                    .First();

                Assert.IsNotNull(first);
                Assert.AreEqual("ROMEY", first.CustomerID);
            });

            // with predicate - after order by
            TestNorthwind(db =>
            {
                var first = db.Customers
                    .OrderBy(c => c.ContactName)
                    .First(c => c.City == "London");

                Assert.IsNotNull(first);
                Assert.AreEqual("EASTC", first.CustomerID);
            });

            // no predicate - after where
            TestNorthwind(db =>
            {
                var first = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Where(c => c.City == "London")
                    .First();

                Assert.IsNotNull(first);
                Assert.AreEqual("EASTC", first.CustomerID);
            });

            // no match
            TestNorthwind(db =>
            {
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    var first = db.Customers
                        .OrderBy(c => c.ContactName)
                        .First(c => c.City == "SpongeBob");
                });
            });
        }

        [TestMethod]
        public void TestOperator_FirstOrDefault()
        {
            // no predicate - after order by 
            TestNorthwind(db =>
            {
                var first = db.Customers
                    .OrderBy(c => c.ContactName)
                    .FirstOrDefault();

                Assert.IsNotNull(first);
                Assert.AreEqual("ROMEY", first.CustomerID);
            });

            // with predicte - after orderby
            TestNorthwind(db =>
            {
                var first = db.Customers
                    .OrderBy(c => c.ContactName)
                    .FirstOrDefault(c => c.City == "London");

                Assert.IsNotNull(first);
                Assert.AreEqual("EASTC", first.CustomerID);
            });

            // no predicate - after where
            TestNorthwind(db =>
            {
                var first = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Where(c => c.City == "London")
                    .FirstOrDefault();

                Assert.IsNotNull(first);
                Assert.AreEqual("EASTC", first.CustomerID);
            });

            // no match
            TestNorthwind(db =>
            {
                var first = db.Customers
                    .OrderBy(c => c.ContactName)
                    .FirstOrDefault(c => c.City == "SpongeBob");

                Assert.IsNull(first);
            });
        }

        [TestMethod]
        public void TestOperator_Reverse()
        {
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Reverse()
                    .ToList();

                Assert.AreEqual(91, list.Count);
                Assert.AreEqual("WOLZA", list[0].CustomerID);
                Assert.AreEqual("ROMEY", list[90].CustomerID);
            });

            // reversed again
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Reverse()
                    .Reverse()
                    .ToList();

                Assert.AreEqual(91, list.Count);
                Assert.AreEqual("ROMEY", list[0].CustomerID);
                Assert.AreEqual("WOLZA", list[90].CustomerID);
            });

            // reversed again after where
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Reverse()
                    .Where(c => c.City == "London")
                    .Reverse()
                    .ToList();

                Assert.AreEqual(6, list.Count);
                Assert.AreEqual("EASTC", list[0].CustomerID);
                Assert.AreEqual("BSBEV", list[5].CustomerID);
            });

            // reversed again after take
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Reverse()
                    .Take(5)
                    .Reverse()
                    .ToList();

                Assert.AreEqual(5, list.Count);
                Assert.AreEqual("CHOPS", list[0].CustomerID);
                Assert.AreEqual("WOLZA", list[4].CustomerID);
            });

            // reversed again after where and take
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Reverse()
                    .Where(c => c.City == "London")
                    .Take(5)
                    .Reverse()
                    .ToList();

                Assert.AreEqual(5, list.Count);
                Assert.AreEqual("CONSH", list[0].CustomerID);
                Assert.AreEqual("BSBEV", list[4].CustomerID);
            });
        }

        [TestMethod]
        public void TestOperator_Last()
        {
            TestNorthwind(db =>
            {
                var last = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Last();

                Assert.IsNotNull(last);
                Assert.AreEqual("WOLZA", last.CustomerID);
            });

            // with predicate
            TestNorthwind(db =>
            {
                var last = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Last(c => c.City == "London");

                Assert.IsNotNull(last);
                Assert.AreEqual("BSBEV", last.CustomerID);
            });

            // after where
            TestNorthwind(db =>
            {
                var last = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Where(c => c.City == "London")
                    .Last();

                Assert.IsNotNull(last);
                Assert.AreEqual("BSBEV", last.CustomerID);
            });

            // no matches
            TestNorthwind(db =>
            {
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    var last = db.Customers
                        .OrderBy(c => c.ContactName)
                        .Last(c => c.City == "SpongeBob");
                });
            });
        }

        [TestMethod]
        public void TestOperator_LastOrDefault()
        {
            TestNorthwind(db =>
            {
                var last = db.Customers
                    .OrderBy(c => c.ContactName)
                    .LastOrDefault();

                Assert.IsNotNull(last);
                Assert.AreEqual("WOLZA", last.CustomerID);
            });

            // with predicate
            TestNorthwind(db =>
            {
                var last = db.Customers
                    .OrderBy(c => c.ContactName)
                    .LastOrDefault(c => c.City == "London");

                Assert.IsNotNull(last);
                Assert.AreEqual("BSBEV", last.CustomerID);
            });

            // after where
            TestNorthwind(db =>
            {
                var last = db.Customers
                    .OrderBy(c => c.ContactName)
                    .Where(c => c.City == "London")
                    .LastOrDefault();

                Assert.IsNotNull(last);
                Assert.AreEqual("BSBEV", last.CustomerID);
            });

            // no matches
            TestNorthwind(db =>
            {
                var last = db.Customers
                    .OrderBy(c => c.ContactName)
                    .LastOrDefault(c => c.City == "SpongeBob");

                Assert.IsNull(last);
            });
        }


        [TestMethod]
        public void TestOperator_Single()
        {
            TestNorthwind(db =>
            {
                var single = db.Customers
                    .Single(c => c.CustomerID == "ALFKI");

                Assert.IsNotNull(single);
                Assert.AreEqual("ALFKI", single.CustomerID);
            });

            // after where
            TestNorthwind(db =>
            {
                var single = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Single();

                Assert.IsNotNull(single);
                Assert.AreEqual("ALFKI", single.CustomerID);
            });

            // too many matches
            TestNorthwind(db =>
            {
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    var single = db.Customers.Single();
                });
            });

            // no matches
            TestNorthwind(db =>
            {
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    var single = db.Customers
                        .Single(c => c.City == "SpongeBob");
                });
            });
        }

        [TestMethod]
        public void TestOperator_SingleOrDefault()
        {
            // with predicate
            TestNorthwind(db =>
            {
                var single = db.Customers
                    .SingleOrDefault(c => c.CustomerID == "ALFKI");

                Assert.IsNotNull(single);
                Assert.AreEqual("ALFKI", single.CustomerID);
            });

            // after where - no predicate
            TestNorthwind(db =>
            {
                var single = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .SingleOrDefault();

                Assert.IsNotNull(single);
                Assert.AreEqual("ALFKI", single.CustomerID);
            });

            // too many matches
            TestNorthwind(db =>
            {
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    var single = db.Customers.SingleOrDefault();
                });
            });

            // no matches
            TestNorthwind(db =>
            {
                var single = db.Customers
                    .SingleOrDefault(c => c.CustomerID == "SpongeBob");

                Assert.IsNull(single);
            });
        }

        [TestMethod]
        public void TestOperaotr_Any()
        {
            // on query
            TestNorthwind(db =>
            {
                var any = db.Customers.Any();
                Assert.AreEqual(true, any);
            });

            // in where
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.Orders.Any(o => o.CustomerID == "ALFKI"))
                    .ToList();

                Assert.AreEqual(1, list.Count);
            });

            // in where - no predicate
            TestNorthwind(db =>
            {
                // customers with at least one order
                var list = db.Customers
                    .Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).Any())
                    .ToList();

                Assert.AreEqual(89, list.Count);
            });

            // in where - on local collection
            TestNorthwind(db =>
            {
                // get customers for any one of these IDs
                string[] ids = new[] { "ALFKI", "WOLZA", "NOONE" };
                var list = db.Customers
                    .Where(c => ids.Any(id => c.CustomerID == id))
                    .ToList();

                Assert.AreEqual(2, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_All()
        {
            // on query - with predicate
            TestNorthwind(db =>
            {
                // all customers have name length > 0?
                var all = db.Customers
                    .All(c => c.ContactName.Length > 0);

                Assert.AreEqual(true, all);
            });

            // in where - with predicate
            TestNorthwind(db =>
            {
                // includes customers w/ no orders
                var list = db.Customers
                    .Where(c => c.Orders.All(o => o.CustomerID == "ALFKI"))
                    .ToList();

                Assert.AreEqual(3, list.Count);
            });

            // in where - on local collection
            TestNorthwind(db =>
            {
                // get all customers with a name that contains both 'm' and 'd'  (don't use vowels since these often depend on collation)
                var patterns = new[] { "m", "d" };

                var list = db.Customers
                    .Where(c => patterns.All(p => c.ContactName.Contains(p))).Select(c => c.ContactName)
                    .ToList();

                var local = db.Customers.AsEnumerable()
                    .Where(c => patterns.All(p => c.ContactName.ToLower().Contains(p)))
                    .Select(c => c.ContactName)
                    .ToList();

                Assert.AreEqual(local.Count, list.Count);
            });

            // no matches
            TestNorthwind(db =>
            {
                // all customers have name with 'a' -- not true
                var all = db.Customers
                    .All(c => c.ContactName.Contains("a"));

                Assert.AreEqual(false, all);
            });
        }

        [TestMethod]
        public void TestOperator_Contains() 
        {
            // on query
            TestNorthwind(db =>
            {
                var contains = db.Customers
                    .Select(c => c.CustomerID)
                    .Contains("ALFKI");

                Assert.AreEqual(true, contains);
            });

            // in where - on joined collection
            TestNorthwind(db =>
            {
                // this is the long-way to determine all customers that have at least one order
                var list = db.Customers
                    .Where(c => db.Orders.Select(o => o.CustomerID).Contains(c.CustomerID))
                    .ToList();

                Assert.AreEqual(89, list.Count);
            });

            // in where - on local collection
            TestNorthwind(db =>
            {
                string[] ids = new[] { "ALFKI", "WOLZA", "NOONE" };

                var list = db.Customers
                    .Where(c => ids.Contains(c.CustomerID))
                    .ToList();

                Assert.AreEqual(2, list.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Skip_Take()
        {
            // with take
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .OrderBy(c => c.CustomerID)
                    .Skip(5)
                    .Take(10)
                    .ToList();

                Assert.AreEqual(10, list.Count);
                Assert.AreEqual("BLAUS", list[0].CustomerID);
                Assert.AreEqual("COMMI", list[9].CustomerID);
            });
        }

        [TestMethod]
        public void TestOperator_Skip_Take_AfterOrderBy()
        {
            // with take - after distinct
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Select(c => c.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .Skip(5)
                    .Take(10)
                    .ToList();

                Assert.AreEqual(10, list.Count);

                // prove they are actually all distinct
                var hs = new HashSet<string>(list);
                Assert.AreEqual(10, hs.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Coalesce()
        {
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Select(c => new { City = (c.City == "London" ? null : c.City), Country = (c.CustomerID == "EASTC" ? null : c.Country) })
                    .Where(x => (x.City ?? "NoCity") == "NoCity")
                    .ToList();

                Assert.AreEqual(6, list.Count);
                Assert.IsNull(list[0].City);
            });

            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Select(c => new { City = (c.City == "London" ? null : c.City), Country = (c.CustomerID == "EASTC" ? null : c.Country) })
                    .Where(x => (x.City ?? x.Country ?? "NoCityOrCountry") == "NoCityOrCountry")
                    .ToList();

                Assert.AreEqual(1, list.Count);
                Assert.IsNull(list[0].City);
                Assert.IsNull(list[0].Country);
            });
        }
        #endregion

        #region Math Operators

        [TestMethod]
        public void TestOperator_LessThan_Int()
        {
            TestNorthwind(db =>
            {
                var alfki = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length < 6);
                var alfkiN = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length < 5);
                Assert.IsNotNull(alfki);
                Assert.IsNull(alfkiN);
            });
        }

        [TestMethod]
        public void TestOperator_LessThan_Decimal()
        {
            TestNorthwind(db =>
            {
                // prove that decimals are treated normally with respect to normal comparison operators
                var alfki = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1.0m : 3.0m) < 2.0m);
                Assert.IsNotNull(alfki);
            });
        }

        [TestMethod]
        public void TestOperator_LessThanOrEqual_Int()
        {
            TestNorthwind(db =>
            {
                var alfki = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length <= 5);
                var alfki2 = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length <= 6);
                var alfkiN = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length <= 4);
                Assert.IsNotNull(alfki);
                Assert.IsNotNull(alfki2);
                Assert.IsNull(alfkiN);
            });
        }

        [TestMethod]
        public void TestOperator_GreaterThan_Int()
        {
            TestNorthwind(db =>
            {
                var alfki = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length > 4);
                var alfkiN = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length > 5);
                Assert.IsNotNull(alfki);
                Assert.IsNull(alfkiN);
            });
        }

        [TestMethod]
        public void TestOperator_GreaterThanOrEqual_Int()
        {
            TestNorthwind(db =>
            {
                var alfki = db.Customers.Single(c => c.CustomerID == "ALFKI" && c.CustomerID.Length >= 4);
                var alfki2 = db.Customers.Single(c => c.CustomerID == "ALFKI" && c.CustomerID.Length >= 5);
                var alfkiN = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length >= 6);
                Assert.IsNotNull(alfki);
                Assert.IsNotNull(alfki2);
                Assert.IsNull(alfkiN);
            });
        }

        [TestMethod]
        public void TestOperator_Equal_Int()
        {
            TestNorthwind(db =>
            {
                var alfki = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length == 5);
                var alfkiN = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length == 8);
                Assert.IsNotNull(alfki);
                Assert.IsNull(alfkiN);
            });
        }

        [TestMethod]
        public void TestOperator_NotEqual_Int()
        {
            TestNorthwind(db =>
            {
                var alfki = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length != 8);
                var alfkiN = db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && c.CustomerID.Length != 5);
                Assert.IsNotNull(alfki);
                Assert.IsNull(alfkiN);
            });
        }

        [TestMethod]
        public void TestOperator_Add_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length + 2)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(7, n);
            });
        }

        [TestMethod]
        public void TestOperator_Subtract_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length - 2)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(3, n);
            });
        }

        [TestMethod]
        public void TestOperator_Multiply_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length * 3)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(15, n);
            });
        }

        [TestMethod]
        public void TestOperator_Divide_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length / 2)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(2, n);
            });
        }

        [TestMethod]
        public void TestOperator_Remainder_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length % 2)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(1, n);
            });
        }

        [TestMethod]
        public void TestOperator_LeftShift_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length << 1)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(10, n);
            });
        }

        [TestMethod]
        public void TestOperator_RightShift_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length >> 1)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(2, n);
            });
        }

        [TestMethod]
        public void TestOperator_BitwiseAnd_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length & 3)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(1, n);
            });
        }

        [TestMethod]
        public void TestOperator_BitwiseOr_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length | 2)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(7, n);
            });
        }

        [TestMethod]
        public void TestOperator_BitwiseXor_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID.Length ^ 1)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(4, n);
            });
        }

        [TestMethod]
        public void TestOperator_BitwiseNot_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => ~c.CustomerID.Length)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(~5, n);
            });
        }

        [TestMethod]
        public void TestOperator_Negate_Int()
        {
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => -c.CustomerID.Length)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(-5, n);
            });
        }

        [TestMethod]
        public void TestOperator_LogicalAnd()
        {
            TestNorthwind(db =>
            {
                var custs = db.Customers
                    .Where(c => c.Country == "USA" && c.City.StartsWith("A"))
                    .Select(c => c.City)
                    .ToList();

                Assert.AreEqual(2, custs.Count);
                Assert.AreEqual(true, custs.All(c => c.StartsWith("A")));
            });
        }

#if !MYSQL
        [TestMethod]
        public void TestOperator_LogicalOr()
        {
            TestNorthwind(db =>
            {
                var custs = db.Customers
                    .Where(c => c.Country == "USA" || c.City.StartsWith("A"))
                    .Select(c => new { c.Country, c.City })
                    .ToList();

                Assert.AreEqual(14, custs.Count);
            });
        }
#endif

        [TestMethod]
        public void TestOperator_LogicalNot()
        {
            TestNorthwind(db =>
            {
                var custs = db.Customers
                    .Where(c => !(c.Country == "USA"))
                    .Select(c => c.Country)
                    .ToList();

                Assert.AreEqual(78, custs.Count);
            });
        }

        [TestMethod]
        public void TestOperator_Equal_Null()
        {
            TestNorthwind(db =>
            {
                var query = db.Customers
                    .Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID)
                    .Where(x => x == null);

                var plan = db.Provider.GetQueryPlan(query.Expression);
                Assert.AreEqual(true, plan.QueryText.Contains("IS NULL"));

                var n = query.Count();
                Assert.AreEqual(1, n);
            });

            TestNorthwind(db =>
            {
                var query = db.Customers
                    .Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID)
                    .Where(x => null == x);

                var plan = db.Provider.GetQueryPlan(query.Expression);
                Assert.AreEqual(true, plan.QueryText.Contains("IS NULL"));

                var n = query.Count();
                Assert.AreEqual(1, n);
            });
        }

        [TestMethod]
        public void TestOperator_NotEqual_Null()
        {
            TestNorthwind(db =>
            {
                var query = db.Customers
                    .Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID)
                    .Where(x => x != null);

                var plan = db.Provider.GetQueryPlan(query.Expression);
                Assert.AreEqual(true, plan.QueryText.Contains("IS NOT NULL"));

                var n = query.Count();
                Assert.AreEqual(90, n);
            });

            TestNorthwind(db =>
            {
                var query = db.Customers
                    .Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID)
                    .Where(x => null != x);

                var plan = db.Provider.GetQueryPlan(query.Expression);
                Assert.AreEqual(true, plan.QueryText.Contains("IS NOT NULL"));

                var n = query.Count();
                Assert.AreEqual(90, n);
            });
        }

        [TestMethod]
        public void TestOperator_Equal_Null_Relationship()
        {
            TestNorthwind(db =>
            {
                var query = db.Orders.Where(o => o.Customer == null);

                var plan = db.Provider.GetQueryPlan(query.Expression);
                Assert.AreEqual(true, plan.QueryText.Contains("IS NULL"));

                var n = query.Count();
                Assert.AreEqual(0, n);
            });

            TestNorthwind(db =>
            {
                var query = db.Orders.Where(o => null == o.Customer);

                var plan = db.Provider.GetQueryPlan(query.Expression);
                Assert.AreEqual(true, plan.QueryText.Contains("IS NULL"));

                var n = query.Count();
                Assert.AreEqual(0, n);
            });
        }

        [TestMethod]
        public void TestOperator_NotEqual_Null_Relationship()
        {
            TestNorthwind(db =>
            {
                var query = db.Orders.Where(o => o.Customer != null);

                var plan = db.Provider.GetQueryPlan(query.Expression);
                Assert.AreEqual(true, plan.QueryText.Contains("IS NOT NULL"));

                var n = query.Count();
                Assert.AreEqual(830, n);
            });

            TestNorthwind(db =>
            {
                var query = db.Orders.Where(o => null != o.Customer);

                var plan = db.Provider.GetQueryPlan(query.Expression);
                Assert.AreEqual(true, plan.QueryText.Contains("IS NOT NULL"));

                var n = query.Count();
                Assert.AreEqual(830, n);
            });
        }

        [TestMethod]
        public void TestOperator_Conditional()
        {
            // in select
            TestNorthwind(db =>
            {
                var value = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.CustomerID == "ALFKI" ? "A" : "B")
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual("A", value);
            });

            // in select - chained
            TestNorthwind(db =>
            {
                var value = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => 
                        c.CustomerID == "UNKNOWN" ? "A" 
                        : c.CustomerID == "ALFKI" ? "B"
                        : "C")
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual("B", value);
            });

            // in select - predictes
            TestNorthwind(db =>
            {
                bool value = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => (c.CustomerID == "ALFKI" 
                        ? string.Compare(c.CustomerID, "POTATO") < 0 
                        : string.Compare(c.CustomerID, "POTATO") > 0))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(true, value);
            });

            // always default case
            TestNorthwind(db =>
            {
                bool? hasOrders = null;

                var query = db.Customers.Select(r => 
                    new
                    {
                        CustomerID = r.CustomerID,
                        HasOrders = 
                            hasOrders != null
                                ? (bool)hasOrders
                                : db.Orders.Any(o => o.CustomerID.Equals(r.CustomerID))
                    });

                var test = query.ToList();
                Assert.AreEqual(91, test.Count());
            });
        }

        #endregion

        #region Framework Functions

        [TestMethod]
        public void TestFunction_String_Length()
        {
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.City.Length == 7)
                    .ToList();

                Assert.AreEqual(9, list.Count);
            });
        }

        [TestMethod]
        public void TestFunction_String_StartsWith()
        {
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.ContactName.StartsWith("M"))
                    .ToList();

                Assert.AreEqual(12, list.Count);
            });

            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.ContactName.StartsWith(c.ContactName))
                    .ToList();

                Assert.AreEqual(91, list.Count);
            });
        }

        [TestMethod]
        public void TestFunction_String_EndsWith()
        {
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.ContactName.EndsWith("s"))
                    .ToList();

                Assert.AreEqual(9, list.Count);
            });

            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.ContactName.EndsWith(c.ContactName))
                    .ToList();

                Assert.AreEqual(91, list.Count);
            });
        }

        [TestMethod]
        public void TestFunction_String_Contains()
        {
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.ContactName.Contains("nd"))
                    .Select(c => c.ContactName)
                    .ToList();

                var local = db.Customers
                    .AsEnumerable()
                    .Where(c => c.ContactName.ToLower()
                    .Contains("nd"))
                    .Select(c => c.ContactName)
                    .ToList();

                Assert.AreEqual(local.Count, list.Count);
            });

            // w/ another column name
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.ContactName.Contains(c.ContactName))
                    .ToList();

                Assert.AreEqual(91, list.Count);
            });
        }

        [TestMethod]
        public void TestFunction_String_Concat()
        {
            // implicit 2 args
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => c.ContactName + "X" == "Maria AndersX")
                    .ToList();

                Assert.AreEqual(1, list.Count);
            });

            // explicit 2 args
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => string.Concat(c.ContactName, "X") == "Maria AndersX")
                    .ToList();

                Assert.AreEqual(1, list.Count);
            });

            // explicit 3 args
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => string.Concat(c.ContactName, "X", c.Country) == "Maria AndersXGermany")
                    .ToList();

                Assert.AreEqual(1, list.Count);
            });

            // explicit n-args
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Where(c => string.Concat(new string[] { c.ContactName, "X", c.Country }) == "Maria AndersXGermany")
                    .ToList();

                Assert.AreEqual(1, list.Count);
            });
        }

        [TestMethod]
        public void TestFunction_String_IsNullOrEmpty()
        {
            TestNorthwind(db =>
            {
                var list = db.Customers
                    .Select(c => c.City == "London" ? null : c.CustomerID)
                    .Where(x => string.IsNullOrEmpty(x))
                    .ToList();

                Assert.AreEqual(6, list.Count);
            });
        }

        [TestMethod]
        public void TestFunction_String_ToUpper()
        {
            TestNorthwind(db =>
            {
                var str = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => (c.CustomerID == "ALFKI" ? "abc" : "abc").ToUpper())
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual("ABC", str);
            });
        }

        [TestMethod]
        public void TestFunction_String_ToLower()
        {
            TestNorthwind(db =>
            {
                var str = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => (c.CustomerID == "ALFKI" ? "ABC" : "ABC").ToLower())
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual("abc", str);
            });
        }

        [TestMethod]
        public void TestFunction_String_Substring()
        {
            // start and length
            TestNorthwind(db =>
            {
                var customer = db.Customers
                    .Where(c => c.City.Substring(0, 4) == "Seat")
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual("Seattle", customer.City);
            });

            // start only
            TestNorthwind(db =>
            {
                var customer = db.Customers
                    .Where(c => c.City.Substring(4) == "tle")
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual("Seattle", customer.City);
            });
        }

#if !SQLITE // no equivalent function
        [TestMethod]        
        public void TestFunction_String_IndexOf()
        {
            // string arg
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.ContactName.IndexOf("ar"))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(1, n);
            });

            // char arg
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.ContactName.IndexOf('r'))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(2, n);
            });

            // w/ start arg
            TestNorthwind(db =>
            {
                var n = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => c.ContactName.IndexOf("a", 3))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(4, n);
            });

        }
#endif

        [TestMethod]
        public void TestFunction_String_Trim()
        {
            TestNorthwind(db =>
            {
                var notrim = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => ("  " + c.City + " "))
                    .AsEnumerable()
                    .Single();

                var trim = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => ("  " + c.City + " ").Trim())
                    .AsEnumerable()
                    .Single();

                Assert.AreNotEqual(notrim, trim);
                Assert.AreEqual(notrim.Trim(), trim);
            });
        }

#if !SQLITE
        // SQLITE: no function to help build correct string representation
        [TestMethod]
        public void TestFunction_DateTime_Construct_YMD()
        {
            TestNorthwind(db =>
            {
                var dt = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(1997, dt.Year);
                Assert.AreEqual(7, dt.Month);
                Assert.AreEqual(4, dt.Day);
                Assert.AreEqual(0, dt.Hour);
                Assert.AreEqual(0, dt.Minute);
                Assert.AreEqual(0, dt.Second);
            });
        }
#endif

#if !SQLITE
        // SQLITE: no function to help build correct string representation
        [TestMethod]
        public void TestFunction_DateTime_Construct_YMDHMS()
        {
            TestNorthwind(db =>
            {
                var dt = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(1997, dt.Year);
                Assert.AreEqual(7, dt.Month);
                Assert.AreEqual(4, dt.Day);
                Assert.AreEqual(3, dt.Hour);
                Assert.AreEqual(5, dt.Minute);
                Assert.AreEqual(6, dt.Second);
            });
        }
#endif

        [TestMethod]
        public void TestFunction_DateTime_Day()
        {
            TestNorthwind(db =>
            {
                var v = db.Orders
                    .Where(o => o.OrderDate == new DateTime(1997, 8, 25))
                    .Take(1)
                    .Select(o => o.OrderDate.Day)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(25, v);
            });
        }

        [TestMethod]
        public void TestFunction_DateTime_Month()
        {
            TestNorthwind(db =>
            {
                var v = db.Orders
                    .Where(o => o.OrderDate == new DateTime(1997, 8, 25))
                    .Take(1)
                    .Select(o => o.OrderDate.Month)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(8, v);
            });
        }

        [TestMethod]
        public void TestFunction_DateTime_Year()
        {
            TestNorthwind(db =>
            {
                var v = db.Orders
                    .Where(o => o.OrderDate == new DateTime(1997, 8, 25))
                    .Take(1)
                    .Select(o => o.OrderDate.Year)
                    .AsEnumerable()
                    .Single();
                
                Assert.AreEqual(1997, v);
            });
        }


#if !SQLITE
        // SQLITE: not able to test via construction
        [TestMethod]
        public void TestFunction_DateTime_Hour()
        {
            TestNorthwind(db =>
            {
                var hour = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Hour)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(3, hour);
            });
        }
#endif

#if !SQLITE
        // SQLITE: not able to test via construction
        [TestMethod]
        public void TestFunction_DateTime_Minute()
        {
            TestNorthwind(db =>
            {
                var minute = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Minute)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(5, minute);
            });
        }
#endif

#if !SQLITE
        // SQLITE: not able to test via construction
        [TestMethod]
        public void TestFunction_DateTime_Second()
        {
            TestNorthwind(db =>
            {
                var second = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Second)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(6, second);
            });
        }
#endif

        [TestMethod]
        public void TestFunction_DateTime_DayOfWeek()
        {
            TestNorthwind(db =>
            {
                var dow = db.Orders
                    .Where(o => o.OrderDate == new DateTime(1997, 8, 25))
                    .Take(1)
                    .Select(o => o.OrderDate.DayOfWeek)
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(DayOfWeek.Monday, dow);
            });
        }

#if !SQLITE
        [TestMethod]
        public void TestFunction_DateTime_AddYears()
        {
            TestNorthwind(db =>
            {
                var od = db.Orders
                    .FirstOrDefault(o => o.OrderDate == new DateTime(1997, 8, 25) && o.OrderDate.AddYears(2).Year == 1999);

                Assert.IsNotNull(od);
            });
        }
#endif

#if !SQLITE
        [TestMethod]
        public void TestFunction_DateTime_AddMonths()
        {
            TestNorthwind(db =>
            {
                var od = db.Orders
                    .FirstOrDefault(o => o.OrderDate == new DateTime(1997, 8, 25) && o.OrderDate.AddMonths(2).Month == 10);

                Assert.IsNotNull(od);
            });
        }
#endif

#if !SQLITE
        [TestMethod]
        public void TestFunction_DateTime_AddDays()
        {
            TestNorthwind(db =>
            {
                var od = db.Orders
                    .FirstOrDefault(o => o.OrderDate == new DateTime(1997, 8, 25) && o.OrderDate.AddDays(2).Day == 27);

                Assert.IsNotNull(od);
            });
        }
#endif

#if !SQLITE
        [TestMethod]
        public void TestFunction_DateTime_AddHours()
        {
            TestNorthwind(db =>
            {
                var od = db.Orders
                    .FirstOrDefault(o => o.OrderDate == new DateTime(1997, 8, 25) && o.OrderDate.AddHours(3).Hour == 3);

                Assert.IsNotNull(od);
            });
        }
#endif

#if !SQLITE
        [TestMethod]
        public void TestFunction_DateTime_AddMinutes()
        {
            TestNorthwind(db =>
            {
                var od = db.Orders
                    .FirstOrDefault(o => o.OrderDate == new DateTime(1997, 8, 25) && o.OrderDate.AddMinutes(5).Minute == 5);

                Assert.IsNotNull(od);
            });
        }
#endif

#if !SQLITE
        [TestMethod]
        public void TestFunction_DateTime_AddSeconds()
        {
            TestNorthwind(db =>
            {
                var od = db.Orders
                    .FirstOrDefault(o => o.OrderDate == new DateTime(1997, 8, 25) && o.OrderDate.AddSeconds(6).Second == 6);

                Assert.IsNotNull(od);
            });
        }
#endif

        [TestMethod]
        public void TestFunction_Math_Abs()
        {
            TestNorthwind(db =>
            {
                var neg1 = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Abs((c.CustomerID == "ALFKI") ? -1 : 0))
                    .AsEnumerable()
                    .Single();

                var pos1 = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Abs((c.CustomerID == "ALFKI") ? 1 : 0))
                    .Single();

                Assert.AreEqual(Math.Abs(-1), neg1);
                Assert.AreEqual(Math.Abs(1), pos1);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Atan()
        {
            TestNorthwind(db =>
            {
                var zero = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Atan((c.CustomerID == "ALFKI") ? 0.0 : 0.0))
                    .AsEnumerable()
                    .Single();

                var one = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Atan((c.CustomerID == "ALFKI") ? 1.0 : 1.0))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(Math.Atan(0.0), zero, 0.0001);
                Assert.AreEqual(Math.Atan(1.0), one, 0.0001);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Cos()
        {
            TestNorthwind(db =>
            {
                var zero = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Cos((c.CustomerID == "ALFKI") ? 0.0 : 0.0))
                    .AsEnumerable()
                    .Single();

                var pi = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Cos((c.CustomerID == "ALFKI") ? Math.PI : Math.PI))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(Math.Cos(0.0), zero, 0.0001);
                Assert.AreEqual(Math.Cos(Math.PI), pi, 0.0001);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Sin()
        {
            TestNorthwind(db =>
            {
                var zero = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Sin((c.CustomerID == "ALFKI") ? 0.0 : 0.0))
                    .AsEnumerable()
                    .Single();

                var pi = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Sin((c.CustomerID == "ALFKI") ? Math.PI : Math.PI))
                    .AsEnumerable()
                    .Single();

                var pi2 = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Sin(((c.CustomerID == "ALFKI") ? Math.PI : Math.PI) / 2.0))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(Math.Sin(0.0), zero);
                Assert.AreEqual(Math.Sin(Math.PI), pi, 0.0001);
                Assert.AreEqual(Math.Sin(Math.PI / 2.0), pi2, 0.0001);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Tan()
        {
            TestNorthwind(db =>
            {
                var zero = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Tan((c.CustomerID == "ALFKI") ? 0.0 : 0.0))
                    .AsEnumerable()
                    .Single();

                var pi = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Tan((c.CustomerID == "ALFKI") ? Math.PI : Math.PI))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(Math.Tan(0.0), zero, 0.0001);
                Assert.AreEqual(Math.Tan(Math.PI), pi, 0.0001);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Exp()
        {
            TestNorthwind(db =>
            {
                var zero = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Exp((c.CustomerID == "ALFKI") ? 0.0 : 0.0))
                    .AsEnumerable()
                    .Single();

                var one = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Exp((c.CustomerID == "ALFKI") ? 1.0 : 1.0))
                    .AsEnumerable()
                    .Single();

                var two = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Exp((c.CustomerID == "ALFKI") ? 2.0 : 2.0))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(Math.Exp(0.0), zero, 0.0001);
                Assert.AreEqual(Math.Exp(1.0), one, 0.0001);
                Assert.AreEqual(Math.Exp(2.0), two, 0.0001);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Log()
        {
            TestNorthwind(db =>
            {
                var one = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Log((c.CustomerID == "ALFKI") ? 1.0 : 1.0))
                    .AsEnumerable()
                    .Single();

                var e = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Log((c.CustomerID == "ALFKI") ? Math.E : Math.E))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(Math.Log(1.0), one, 0.0001);
                Assert.AreEqual(Math.Log(Math.E), e, 0.0001);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Sqrt()
        {
            TestNorthwind(db =>
            {
                var one = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 1.0 : 1.0))
                    .AsEnumerable()
                    .Single();

                var four = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 4.0 : 4.0))
                    .AsEnumerable()
                    .Single();

                var nine = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 9.0 : 9.0))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(1.0, one);
                Assert.AreEqual(2.0, four);
                Assert.AreEqual(3.0, nine);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Pow()
        {
            // 2^n
            TestNorthwind(db =>
            {
                var zero = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 0.0))
                    .AsEnumerable()
                    .Single();

                var one = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 1.0))
                    .AsEnumerable()
                    .Single();

                var two = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 2.0))
                    .AsEnumerable()
                    .Single();

                var three = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 3.0))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(1.0, zero);
                Assert.AreEqual(2.0, one);
                Assert.AreEqual(4.0, two);
                Assert.AreEqual(8.0, three);
            });
        }

        [TestMethod]
        public void TestFunction_Math_Round_Default()
        {
            TestNorthwind(db =>
            {
                var four = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Round((c.CustomerID == "ALFKI") ? 3.4 : 3.4))
                    .AsEnumerable()
                    .Single();

                var six = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Round((c.CustomerID == "ALFKI") ? 3.6 : 3.6))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(3.0, four);
                Assert.AreEqual(4.0, six);
            });
        }

#if !SQLITE && !ACCESS
        [TestMethod]
        public void TestFunction_Math_Floor()
        {
            // The difference between floor and truncate is how negatives are handled.  Floor drops the decimals and moves the
            // value to the more negative, so Floor(-3.4) is -4.0 and Floor(3.4) is 3.0.
            TestNorthwind(db =>
            {
                var four = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Floor((c.CustomerID == "ALFKI" ? 3.4 : 3.4)))
                    .AsEnumerable()
                    .Single();

                var six = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Floor((c.CustomerID == "ALFKI" ? 3.6 : 3.6)))
                    .AsEnumerable()
                    .Single();

                var nfour = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Floor((c.CustomerID == "ALFKI" ? -3.4 : -3.4)))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(Math.Floor(3.4), four);
                Assert.AreEqual(Math.Floor(3.6), six);
                Assert.AreEqual(Math.Floor(-3.4), nfour);
            });
        }
#endif

#if !SQLITE
        [TestMethod]
        public void TestFunction_Math_Truncate()
        {
            // The difference between floor and truncate is how negatives are handled.  Truncate drops the decimals, 
            // therefore a truncated negative often has a more positive value than non-truncated (never has a less positive),
            // so Truncate(-3.4) is -3.0 and Truncate(3.4) is 3.0.
            TestNorthwind(db =>
            {
                var four = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.4 : 3.4))
                    .AsEnumerable()
                    .Single();

                var six = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.6 : 3.6))
                    .AsEnumerable()
                    .Single();

                var neg4 = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => Math.Truncate((c.CustomerID == "ALFKI") ? -3.4 : -3.4))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(Math.Truncate(3.4), four);
                Assert.AreEqual(Math.Truncate(3.6), six);
                Assert.AreEqual(Math.Truncate(-3.4), neg4);
            });
        }
#endif


        [TestMethod]
        public void TestFunction_String_CompareTo()
        {
            // as value in select
            TestNorthwind(db =>
            {
                var lt = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => c.City.CompareTo("Seattle")).AsEnumerable().Single();
                var gt = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => c.City.CompareTo("Aaa")).AsEnumerable().Single();
                var eq = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => c.City.CompareTo("Berlin")).AsEnumerable().Single();
                Assert.AreEqual(-1, lt);
                Assert.AreEqual(1, gt);
                Assert.AreEqual(0, eq);
            });

            // with <
            TestNorthwind(db =>
            {
                var cmpLT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") < 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") < 0);
                Assert.IsNotNull(cmpLT);
                Assert.IsNull(cmpEQ);
            });

            // with <=
            TestNorthwind(db =>
            {
                var cmpLE = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") <= 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") <= 0);
                var cmpGT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") <= 0);
                Assert.IsNotNull(cmpLE);
                Assert.IsNotNull(cmpEQ);
                Assert.IsNull(cmpGT);
            });

            // with >
            TestNorthwind(db =>
            {
                var cmpLT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") > 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") > 0);
                Assert.IsNotNull(cmpLT);
                Assert.IsNull(cmpEQ);
            });

            // with >=
            TestNorthwind(db =>
            {
                var cmpLE = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") >= 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") >= 0);
                var cmpGT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") >= 0);
                Assert.IsNull(cmpLE);
                Assert.IsNotNull(cmpEQ);
                Assert.IsNotNull(cmpGT);
            });

            // with ==
            TestNorthwind(db =>
            {
                var cmpLE = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") == 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") == 0);
                var cmpGT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") == 0);
                Assert.IsNull(cmpLE);
                Assert.IsNotNull(cmpEQ);
                Assert.IsNull(cmpGT);
            });

            // with !=
            TestNorthwind(db =>
            {
                var cmpLE = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") != 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") != 0);
                var cmpGT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") != 0);
                Assert.IsNotNull(cmpLE);
                Assert.IsNull(cmpEQ);
                Assert.IsNotNull(cmpGT);
            });
        }

        [TestMethod]
        public void TestFunction_String_Compare()
        {
            // as value in select
            TestNorthwind(db =>
            {
                var lt = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => string.Compare(c.City, "Seattle")).AsEnumerable().Single();
                var gt = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => string.Compare(c.City, "Aaa")).AsEnumerable().Single();
                var eq = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => string.Compare(c.City, "Berlin")).AsEnumerable().Single();
                Assert.AreEqual(-1, lt);
                Assert.AreEqual(1, gt);
                Assert.AreEqual(0, eq);
            });

            // with <
            TestNorthwind(db =>
            {
                var cmpLT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") < 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") < 0);
                Assert.IsNotNull(cmpLT);
                Assert.IsNull(cmpEQ);
            });

            // with <=
            TestNorthwind(db =>
            {
                var cmpLE = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") <= 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") <= 0);
                var cmpGT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") <= 0);
                Assert.IsNotNull(cmpLE);
                Assert.IsNotNull(cmpEQ);
                Assert.IsNull(cmpGT);
            });

            // with >
            TestNorthwind(db =>
            {
                var cmpLT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") > 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") > 0);
                Assert.IsNotNull(cmpLT);
                Assert.IsNull(cmpEQ);
            });

            // with >=
            TestNorthwind(db =>
            {
                var cmpLE = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") >= 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") >= 0);
                var cmpGT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") >= 0);
                Assert.IsNull(cmpLE);
                Assert.IsNotNull(cmpEQ);
                Assert.IsNotNull(cmpGT);
            });

            // with ==
            TestNorthwind(db =>
            {
                var cmpLE = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") == 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") == 0);
                var cmpGT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") == 0);
                Assert.IsNull(cmpLE);
                Assert.IsNotNull(cmpEQ);
                Assert.IsNull(cmpGT);
            });

            // with !=
            TestNorthwind(db =>
            {
                var cmpLE = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") != 0);
                var cmpEQ = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") != 0);
                var cmpGT = db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") != 0);
                Assert.IsNotNull(cmpLE);
                Assert.IsNull(cmpEQ);
                Assert.IsNotNull(cmpGT);
            });
        }


        [TestMethod]
        public void TestFunction_Int_CompareTo()
        {
            TestNorthwind(db =>
            {
                // prove that x.CompareTo(y) works for types other than string
                var eq = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(10)).AsEnumerable().Single();
                var gt = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(9)).AsEnumerable().Single();
                var lt = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(11)).AsEnumerable().Single();
                Assert.AreEqual(0, eq);
                Assert.AreEqual(1, gt);
                Assert.AreEqual(-1, lt);
            });
        }

        [TestMethod]
        public void TestFunction_Decimal_Compare()
        {
            TestNorthwind(db =>
            {
                // prove that type.Compare(x,y) works with decimal
                var eq = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 10m)).AsEnumerable().Single();
                var gt = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 9m)).AsEnumerable().Single();
                var lt = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 11m)).AsEnumerable().Single();
                Assert.AreEqual(0, eq);
                Assert.AreEqual(1, gt);
                Assert.AreEqual(-1, lt);
            });
        }

        [TestMethod]
        public void TestFunction_Decimal_Add()
        {
            TestNorthwind(db =>
            {
                var onetwo = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Add((c.CustomerID == "ALFKI" ? 1m : 1m), 2m)).AsEnumerable().Single();
                Assert.AreEqual(3m, onetwo);
            });
        }

        [TestMethod]
        public void TestFunction_Decimal_Subtract()
        {
            TestNorthwind(db =>
            {
                var onetwo = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Subtract((c.CustomerID == "ALFKI" ? 1m : 1m), 2m)).AsEnumerable().Single();
                Assert.AreEqual(-1m, onetwo);
            });
        }

        [TestMethod]
        public void TestFunction_Decimal_Multiply()
        {
            TestNorthwind(db =>
            {
                var onetwo = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Multiply((c.CustomerID == "ALFKI" ? 1m : 1m), 2m)).AsEnumerable().Single();
                Assert.AreEqual(2m, onetwo);
            });
        }

        [TestMethod]
        public void TestFunction_Decimal_Divide()
        {
            TestNorthwind(db =>
            {
                var onetwo = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Divide((c.CustomerID == "ALFKI" ? 1.0m : 1.0m), 2.0m)).AsEnumerable().Single();
                Assert.AreEqual(0.5m, onetwo);
            });
        }

        [TestMethod]
        public void TestFunction_Decimal_Negate()
        {
            TestNorthwind(db =>
            {
                var one = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Negate((c.CustomerID == "ALFKI" ? 1m : 1m))).AsEnumerable().Single();
                Assert.AreEqual(-1m, one);
            });
        }

        [TestMethod]
        public void TestFunction_Decimal_RoundDefault()
        {
            TestNorthwind(db =>
            {
                var four = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Round((c.CustomerID == "ALFKI" ? 3.4m : 3.4m))).AsEnumerable().Single();
                var six = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Round((c.CustomerID == "ALFKI" ? 3.5m : 3.5m))).AsEnumerable().Single();
                Assert.AreEqual(3.0m, four);
                Assert.AreEqual(4.0m, six);
            });
        }

#if !SQLITE
        [TestMethod]
        public void TestFunction_Decimal_Truncate()
        {
            // The difference between floor and truncate is how negatives are handled.  Truncate drops the decimals, 
            // therefore a truncated negative often has a more positive value than non-truncated (never has a less positive),
            // so Truncate(-3.4) is -3.0 and Truncate(3.4) is 3.0.
            TestNorthwind(db =>
            {
                var four = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => decimal.Truncate((c.CustomerID == "ALFKI") ? 3.4m : 3.4m)).AsEnumerable().Single();
                var six = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.6m : 3.6m)).AsEnumerable().Single();
                var neg4 = db.Customers.Where(c => c.CustomerID == "ALFKI").Select(c => Math.Truncate((c.CustomerID == "ALFKI") ? -3.4m : -3.4m)).AsEnumerable().Single();
                Assert.AreEqual(decimal.Truncate(3.4m), four);
                Assert.AreEqual(decimal.Truncate(3.6m), six);
                Assert.AreEqual(decimal.Truncate(-3.4m), neg4);
            });
        }
#endif

#if !SQLITE && !ACCESS
        [TestMethod]
        public void TestFunction_Decimal_Floor()
        {
            TestNorthwind(db =>
            {
                var four = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => decimal.Floor((c.CustomerID == "ALFKI" ? 3.4m : 3.4m)))
                    .AsEnumerable()
                    .Single();

                var six = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => decimal.Floor((c.CustomerID == "ALFKI" ? 3.6m : 3.6m)))
                    .AsEnumerable()
                    .Single();

                var nfour = db.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => decimal.Floor((c.CustomerID == "ALFKI" ? -3.4m : -3.4m)))
                    .AsEnumerable()
                    .Single();

                Assert.AreEqual(decimal.Floor(3.4m), four);
                Assert.AreEqual(decimal.Floor(3.6m), six);
                Assert.AreEqual(decimal.Floor(-3.4m), nfour);
            });
        }
#endif

        #endregion

        #region Mapping and Relationships

        [TestMethod]
        public void TestMapping_Association_Where_Once()
        {
            // assocation property in where
            TestNorthwind(db =>
            {
                var ords = (
                from o in db.Orders
                where o.Customer.City == "Seattle"
                select o
                ).ToList();

                Assert.AreEqual(14, ords.Count);
            });
        }

        [TestMethod]
        public void TestMapping_Association_Where_Duplicate()
        {
            // duplicate association in where
            TestNorthwind(db =>
            {
                var n = db.Orders.Where(c => c.CustomerID == "WHITC").Count();
                var ords = (
                    from o in db.Orders
                    where o.Customer.Country == "USA" && o.Customer.City == "Seattle"
                    select o
                    ).ToList();

                Assert.AreEqual(n, ords.Count);
            });
        }

        [TestMethod]
        public void TestMapping_Association_Where_MultiHop()
        {
            // multi-hop associations
            TestNorthwind(db =>
            {
                var q = from d in db.OrderDetails
                        where d.Order.Customer.CustomerID == "VINET"
                        select d;

                var ods = q.ToList();
                Assert.AreEqual(10, ods.Count);
            });
        }

        [TestMethod]
        public void TestMapping_Assocation_Select()
        {
            // select associated customer
            TestNorthwind(db =>
            {
                var custs = (
                from o in db.Orders
                where o.CustomerID == "ALFKI"
                select o.Customer
                ).ToList();

                Assert.AreEqual(6, custs.Count);
                Assert.AreEqual(true, custs.All(c => c.CustomerID == "ALFKI"));
            });

            // duplicate assocations in select
            TestNorthwind(db =>
            {
                var doubleCusts = (
                from o in db.Orders
                where o.CustomerID == "ALFKI"
                select new { A = o.Customer, B = o.Customer }
                ).ToList();

                Assert.AreEqual(6, doubleCusts.Count);
                Assert.AreEqual(true, doubleCusts.All(c => c.A.CustomerID == "ALFKI" && c.B.CustomerID == "ALFKI"));
            });

            // multi-hop
            TestNorthwind(db =>
            {
                var q = from od in db.OrderDetails
                        where od.Order != null
                        select od.Order.Customer.CompanyName;

                var result = q.ToList();
                Assert.AreEqual(2155, result.Count);
            });
        }

        [TestMethod]
        public void TestAssociation_Where_Select()
        {
            TestNorthwind(db =>
            {
                // same association in where and select
                var stuff = (
                from o in db.Orders
                where o.Customer.Country == "USA"
                where o.Customer.City != "Seattle"
                select new { A = o.Customer, B = o.Customer }
                ).ToList();

                Assert.AreEqual(108, stuff.Count);
            });
        }

        [TestMethod]
        public void TestMapping_Association_OrderBy_MultiHop()
        {
            // multi-hop
            TestNorthwind(db =>
            {
                var q = from od in db.OrderDetails
                        orderby od.Order.Customer.CompanyName
                        select od.OrderID;

                var result = q.ToList();
            });
        }

#if false
        [TestMethod]
        public void TestMapping_Association_GroupBy()
        {
            // multi-hop
            TestNorthwind(db =>
            {
                var q = from od in db.OrderDetails
                        group od by od.Order.Customer.CustomerID;

                var result = q.ToList();
            });
        }
#endif

        #endregion

        #region Policy

        [TestMethod]
        public void TestPolicy_IncludeWith_RelatedCollection()
        {
            // related collection
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .IncludeWith<Customer>(c => c.Orders);
                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers.Where(c => c.CustomerID == "ALFKI").ToList();
                Assert.AreEqual(1, custs.Count);
                Assert.IsNotNull(custs[0].Orders);
                Assert.AreEqual(6, custs[0].Orders.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_IncludeWith_MultipleNestedCollections()
        {
            // multiple nested related collections
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .IncludeWith<Customer>(c => c.Orders)
                    .IncludeWith<Order>(o => o.Details);

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .ToList();

                Assert.AreEqual(1, custs.Count);
                Assert.IsNotNull(custs[0].Orders);
                Assert.AreEqual(6, custs[0].Orders.Count);
                Assert.AreEqual(true, custs[0].Orders.Any(o => o.OrderID == 10643));
                Assert.IsNotNull(custs[0].Orders.Single(o => o.OrderID == 10643).Details);
                Assert.AreEqual(3, custs[0].Orders.Single(o => o.OrderID == 10643).Details.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_IncludeWith_EntityConstructor()
        {
            // entity created via constructor
            TestNorthwind(db =>
            {
                var mapping = new AttributeEntityMapping(typeof(NorthwindX));
                var policy = EntityPolicy.Default
                    .IncludeWith<CustomerX>(c => c.Orders);

                var nw = new NorthwindX(db.Provider.WithPolicy(policy).WithMapping(mapping));

                var custs = nw.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .ToList();

                Assert.AreEqual(1, custs.Count);
                Assert.IsNotNull(custs[0].Orders);
                Assert.AreEqual(6, custs[0].Orders.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_IncludeWith_FilteredCollection()
        {
            // filtered
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .IncludeWith<Customer>(c => c.Orders.Where(o => (o.OrderID % 2) == 0));

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .ToList();

                Assert.AreEqual(1, custs.Count);
                Assert.IsNotNull(custs[0].Orders);
                Assert.AreEqual(3, custs[0].Orders.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_IncludeWith_Deferred()
        {
            // deferred
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .IncludeWith<Customer>(c => c.Orders, true);
                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .ToList();

                Assert.AreEqual(1, custs.Count);
                Assert.IsNotNull(custs[0].Orders);
                Assert.AreEqual(6, custs[0].Orders.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_IncludeWith_FirstSingle()
        {
            // First and Single in query
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .IncludeWith<Order>(o => o.Details);

                var ndb = new Northwind(db.Provider.WithPolicy(policy));

                var q = from o in ndb.Orders
                        where o.OrderID == 10248
                        select o;

                Order so = q.Single();
                Assert.AreEqual(3, so.Details.Count);

                Order fo = q.First();
                Assert.AreEqual(3, fo.Details.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_AssociateWith_Filter()
        {
            // filtered
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .AssociateWith<Customer>(c => c.Orders.Where(o => (o.OrderID % 2) == 0));

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .Select(c => new
                    {
                        CustomerID = c.CustomerID,
                        FilteredOrdersCount = c.Orders.Count()
                    })
                    .ToList();

                Assert.AreEqual(1, custs.Count);
                Assert.AreEqual(3, custs[0].FilteredOrdersCount);
            });
        }

        [TestMethod]
        public void TestPolicy_AssociateWith_AfterIncludeWith()
        {
            // after IncludeWith
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .IncludeWith<Customer>(c => c.Orders)
                    .AssociateWith<Customer>(c => c.Orders.Where(o => (o.OrderID % 2) == 0));

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .ToList();

                Assert.AreEqual(1, custs.Count);
                Assert.IsNotNull(custs[0].Orders);
                Assert.AreEqual(3, custs[0].Orders.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_AssociateWith_BeforeIncludeWith()
        {
            // before IncludeWith
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .AssociateWith<Customer>(c => c.Orders.Where(o => (o.OrderID % 2) == 0))
                    .IncludeWith<Customer>(c => c.Orders);

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .ToList();

                Assert.AreEqual(1, custs.Count);
                Assert.IsNotNull(custs[0].Orders);
                Assert.AreEqual(3, custs[0].Orders.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_AssociateWith_GroupBy()
        {
            // GroupBy does not interfere with include
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .IncludeWith<Order>(o => o.Details);

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var list = nw.Orders
                    .Where(o => o.CustomerID == "ALFKI")
                    .GroupBy(o => o.CustomerID)
                    .ToList();

                Assert.AreEqual(1, list.Count);
                var grp = list[0].ToList();
                Assert.AreEqual(6, grp.Count);
                var o10643 = grp.SingleOrDefault(o => o.OrderID == 10643);
                Assert.IsNotNull(o10643);
                Assert.AreEqual(3, o10643.Details.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_Apply_Filter()
        {
            // filter entity
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .Apply<Customer>(seq => seq.Where(c => c.City == "London"));

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers.ToList();
                Assert.AreEqual(6, custs.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_Apply_Filter_Computed()
        {
            // computed filter
            TestNorthwind(db =>
            {
                var ci = "Lon";
                var ty = "don";

                var policy = EntityPolicy.Default
                    .Apply<Customer>(seq => seq.Where(c => c.City == ci + ty));

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .ToList();

                Assert.AreEqual(6, custs.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_Apply_Filter_Multiple()
        {
            // same entity filtered more than once
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .Apply<Customer>(seq => seq.Where(c => c.City == "London"))
                    .Apply<Customer>(seq => seq.Where(c => c.Country == "UK"));

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .ToList();

                Assert.AreEqual(6, custs.Count);
            });
        }

        [TestMethod]
        public void TestPolicy_Apply_OrderBy()
        {
            // w/ OrderBy
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .Apply<Customer>(seq => seq.OrderBy(c => c.ContactName));

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var list = nw.Customers
                    .Where(c => c.City == "London")
                    .ToList();

                Assert.AreEqual(6, list.Count);
                var sorted = list.OrderBy(c => c.ContactName).ToList();
                Assert.AreEqual(true, Enumerable.SequenceEqual(list, sorted));
            });
        }

        [TestMethod]
        public void TestPolicy_Apply_BeforeIncludeWith()
        {
            // before IncludeWith
            TestNorthwind(db =>
            {
                var policy = EntityPolicy.Default
                    .Apply<Order>(ords => ords.Where(o => o.OrderDate.Year > 0))
                    .IncludeWith<Customer>(c => c.Orders.Where(o => (o.OrderID % 2) == 0));

                var nw = new Northwind(db.Provider.WithPolicy(policy));

                var custs = nw.Customers
                    .Where(c => c.CustomerID == "ALFKI")
                    .ToList();

                Assert.AreEqual(1, custs.Count);
                Assert.IsNotNull(custs[0].Orders);
                Assert.AreEqual(3, custs[0].Orders.Count);
            });
        }

        #endregion
    }
}
