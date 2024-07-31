
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

using IQToolkit;
using IQToolkit.AnsiSql;
using IQToolkit.Data;
using IQToolkit.Entities;
using IQToolkit.Entities.Mapping;
using IQToolkit.Utils;

namespace Test
{
    public class TestQueryExecutor : QueryExecutor
    {
        private readonly DataSet _results;

        public override TypeConverter Converter { get; }
        public override QueryTypeSystem TypeSystem { get; }
        public override TextWriter? Log { get; }

        private TestQueryExecutor(
            TypeConverter? converter,
            QueryTypeSystem? typeSystem,
            TextWriter? log,
            DataSet results)
        {
            this.Converter = converter ?? TypeConverter.Default;
            this.TypeSystem = typeSystem ?? AnsiSqlTypeSystem.Singleton;
            this.Log = log;
            _results = results;
        }

        public TestQueryExecutor(
            string queryResults)
            : this(null, null, null, LoadFromXmlText(queryResults))
        {
        }

        private static DataSet LoadFromXmlText(string xmlText)
        {
            var ds = new DataSet();
            ds.ReadXml(xmlText);
            return ds;
        }

        public new TestQueryExecutor WithConverter(TypeConverter converter) =>
            (TestQueryExecutor)base.WithConverter(converter);

        public new TestQueryExecutor WithTypeSystem(QueryTypeSystem typeSystem) =>
            (TestQueryExecutor)base.WithTypeSystem(typeSystem);

        public new TestQueryExecutor WithLog(TextWriter? log) =>       
            (TestQueryExecutor)base.WithLog(log);

        protected override QueryExecutor Construct(TypeConverter converter, QueryTypeSystem typeSystem, TextWriter? log)
        {
            return new TestQueryExecutor(converter, typeSystem, log, _results);
        }

        /// <summary>
        /// Executes the command once and and projects the rows of the resulting rowset into a sequence of values.
        /// </summary>
        public override IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
        {
            var dataReader = _results.CreateDataReader();
            var fieldReader = new DbFieldReader(this.Converter, dataReader);
            while (dataReader.Read())
            {
                yield return fnProjector(fieldReader);
            }
        }

        /// <summary>
        /// Executes the command over a series of parameter sets, and returns the total number of rows affected.
        /// </summary>
        public override IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute the same command over a series of parameter sets, and produces a sequence of values, once per execution.
        /// </summary>
        public override IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<FieldReader, T> fnProjector, MappingEntity entity, int batchSize, bool stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Produces an <see cref="IEnumerable{T}"/> that will execute the command when enumerated.
        /// </summary>
        public override IEnumerable<T> ExecuteDeferred<T>(QueryCommand query, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute a single command with the specified parameter values and return the number of rows affected.
        /// </summary>
        public override int ExecuteCommand(QueryCommand query, object?[]? paramValues = null)
        {
            throw new NotImplementedException();
        }
    }
}