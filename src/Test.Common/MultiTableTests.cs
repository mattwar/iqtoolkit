using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using IQToolkit;
using IQToolkit.Data;

namespace Test
{
    public abstract class MultiTableTests : QueryTestBase
    {
        protected MultiTableContext db;

        private void CleaupDatabase()
        {
            ExecSilent("DELETE FROM TestTable3");
            ExecSilent("DELETE FROM TestTable2");
            ExecSilent("DELETE FROM TestTable1");
        }

        public override void Setup(string[] args)
        {
            base.Setup(args);

            this.db = new MultiTableContext(this.GetProvider());

            ExecSilent("DROP TABLE TestTable3");
            ExecSilent("DROP TABLE TestTable2");
            ExecSilent("DROP TABLE TestTable1");
            ExecSilent("CREATE TABLE TestTable1 (ID int IDENTITY(1,1) PRIMARY KEY, Value1 VARCHAR(10))");
            ExecSilent("CREATE TABLE TestTable2 (ID int PRIMARY KEY REFERENCES TestTable1(ID), Value2 VARCHAR(10))");
            ExecSilent("CREATE TABLE TestTable3 (ID int PRIMARY KEY REFERENCES TestTable1(ID), Value3 VARCHAR(10))");
        }

        public override void Teardown()
        {
            ExecSilent("DROP TABLE TestTable3");
            ExecSilent("DROP TABLE TestTable2");
            ExecSilent("DROP TABLE TestTable1");
        }

        public override void RunTest(Action testAction)
        {
            this.CleaupDatabase();
            base.RunTest(testAction);
        }

        public void TestInsert()
        {
            int id = 
                db.MultiTableEntities.Insert(
                    new MultiTableEntity
                    {
                        Value1 = "ABC",
                        Value2 = "DEF",
                        Value3 = "GHI"
                    },
                    m => m.ID
                );

            var entity = db.MultiTableEntities.SingleOrDefault(m => m.ID == id);
            Assert.Equal(true, entity != null);
            Assert.Equal("ABC", entity.Value1);
            Assert.Equal("DEF", entity.Value2);
            Assert.Equal("GHI", entity.Value3);
        }

        public void TestInsertReturnId()
        {
            var id = 
                db.MultiTableEntities.Insert(
                    new MultiTableEntity
                    {
                        Value1 = "ABC",
                        Value2 = "DEF",
                        Value3 = "GHI"
                    },
                    m => m.ID
                );

            var entity = db.MultiTableEntities.SingleOrDefault(m => m.ID == id);
            Assert.Equal(true, entity != null);
            Assert.Equal("ABC", entity.Value1);
            Assert.Equal("DEF", entity.Value2);
            Assert.Equal("GHI", entity.Value3);
        }

        public void TestInsertBatch()
        {
            var ids =
                db.MultiTableEntities.Batch(
                    new[] {
                        new MultiTableEntity
                        {
                            Value1 = "ABC",
                            Value2 = "DEF",
                            Value3 = "GHI"
                        },
                        new MultiTableEntity
                        {
                            Value1 = "123",
                            Value2 = "456",
                            Value3 = "789"
                        }
                    },
                    (u, m) => u.Insert(m, x => x.ID)
                ).ToList();

            var entity1 = db.MultiTableEntities.SingleOrDefault(m => m.ID == ids[0]);
            Assert.Equal(true, entity1 != null);
            Assert.Equal("ABC", entity1.Value1);
            Assert.Equal("DEF", entity1.Value2);
            Assert.Equal("GHI", entity1.Value3);

            var entity2 = db.MultiTableEntities.SingleOrDefault(m => m.ID == ids[1]);
            Assert.Equal(true, entity2 != null);
            Assert.Equal("123", entity2.Value1);
            Assert.Equal("456", entity2.Value2);
            Assert.Equal("789", entity2.Value3);
        }

        public void TestUpdate()
        {
            var id = 
                db.MultiTableEntities.Insert(
                    new MultiTableEntity
                    {
                        Value1 = "ABC",
                        Value2 = "DEF",
                        Value3 = "GHI"
                    },
                    m => m.ID
                );

            var nUpdated = 
                db.MultiTableEntities.Update(
                    new MultiTableEntity
                    {
                        ID = id,
                        Value1 = "123",
                        Value2 = "456",
                        Value3 = "789"
                    }
                    );

            Assert.Equal(true, nUpdated == 3);

            var entity = db.MultiTableEntities.SingleOrDefault(m => m.ID == id);
            Assert.Equal(true, entity != null);
            Assert.Equal("123", entity.Value1);
            Assert.Equal("456", entity.Value2);
            Assert.Equal("789", entity.Value3);
        }

        public void TestUpdateBatch()
        {
            var ids =
                db.MultiTableEntities.Batch(
                    new[] {
                        new MultiTableEntity
                        {
                            Value1 = "ABC",
                            Value2 = "DEF",
                            Value3 = "GHI"
                        },
                        new MultiTableEntity
                        {
                            Value1 = "123",
                            Value2 = "456",
                            Value3 = "789"
                        }
                    },
                    (u, m) => u.Insert(m, x => x.ID)
                ).ToList();

            var nUpdated =
                db.MultiTableEntities.Batch(
                    new[] {
                        new MultiTableEntity
                        {
                            ID = ids[0],
                            Value1 = "ABCx",
                            Value2 = "DEFx",
                            Value3 = "GHIx"
                        },
                        new MultiTableEntity
                        {
                            ID = ids[1],
                            Value1 = "123x",
                            Value2 = "456x",
                            Value3 = "789x"
                        }
                    },
                    (u, m) => u.Update(m)
                );

            var entity1 = db.MultiTableEntities.SingleOrDefault(m => m.ID == ids[0]);
            Assert.Equal(true, entity1 != null);
            Assert.Equal("ABCx", entity1.Value1);
            Assert.Equal("DEFx", entity1.Value2);
            Assert.Equal("GHIx", entity1.Value3);

            var entity2 = db.MultiTableEntities.SingleOrDefault(m => m.ID == ids[1]);
            Assert.Equal(true, entity2 != null);
            Assert.Equal("123x", entity2.Value1);
            Assert.Equal("456x", entity2.Value2);
            Assert.Equal("789x", entity2.Value3);
        }

        public void TestInsertOrUpdateNew()
        {
            int id = db.MultiTableEntities.InsertOrUpdate(
                new MultiTableEntity
                {
                    Value1 = "ABC",
                    Value2 = "DEF",
                    Value3 = "GHI"
                },
                null,
                m => m.ID
                );

            Assert.Equal(true, id > 0);

            var entity = db.MultiTableEntities.SingleOrDefault(m => m.ID == id);
            Assert.Equal(true, entity != null);
            Assert.Equal("ABC", entity.Value1);
            Assert.Equal("DEF", entity.Value2);
            Assert.Equal("GHI", entity.Value3);
        }

        public void TestInsertOrUpdateExisting()
        {
            int id = db.MultiTableEntities.InsertOrUpdate(
                new MultiTableEntity
                {
                    Value1 = "ABC",
                    Value2 = "DEF",
                    Value3 = "GHI"
                },
                null,
                m => m.ID
                );

            Assert.Equal(true, id > 0);

            db.MultiTableEntities.InsertOrUpdate(
                new MultiTableEntity
                {
                    ID = id,
                    Value1 = "123",
                    Value2 = "456",
                    Value3 = "789"
                }
                );

            var entity = db.MultiTableEntities.SingleOrDefault(m => m.ID == id);
            Assert.Equal(true, entity != null);
            Assert.Equal("123", entity.Value1);
            Assert.Equal("456", entity.Value2);
            Assert.Equal("789", entity.Value3);
        }

        public void TestDelete()
        {
            var entity =
                new MultiTableEntity
                    {
                        Value1 = "ABC",
                        Value2 = "DEF",
                        Value3 = "GHI"
                    };

            entity.ID = db.MultiTableEntities.Insert(entity, m => m.ID);

            Assert.Equal(true, db.MultiTableEntities.Any(m => m.ID == entity.ID));

            int nDeleted = db.MultiTableEntities.Delete(entity);

            Assert.Equal(3, nDeleted);

            Assert.Equal(false, db.MultiTableEntities.Any(m => m.ID == entity.ID));        
        }

        public void TestSelectSubset()
        {
            var id = 
                db.MultiTableEntities.Insert(
                    new MultiTableEntity
                    {
                        Value1 = "ABC",
                        Value2 = "DEF",
                        Value3 = "GHI"
                    },
                    m => m.ID
                );

            var data = db.MultiTableEntities
                         .Select(m => new { m.ID, m.Value1 })
                         .SingleOrDefault(m => m.ID == id);

            Assert.Equal(true, data != null);
        }
    }
}