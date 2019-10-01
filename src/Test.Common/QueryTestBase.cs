// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Test
{
    using IQToolkit.Data;
    using IQToolkit.Data.Common;

    public abstract class QueryTestBase
    {
        private bool executeQueries;

        private Dictionary<string, string> baselines;
        private XmlTextWriter baselineWriter;
        private string baselineKey;
        private string queryText;

        private DbEntityProvider provider;

        public QueryTestBase()
        {
        }

        public DbEntityProvider GetProvider()
        {
            return this.provider;
        }

        protected abstract DbEntityProvider CreateProvider();

        public virtual void Setup(string[] args)
        {
            this.provider = this.CreateProvider();
            this.provider.Connection.Open();

            if (args.Any(a => a == "-log"))
            {
                this.provider.Log = Console.Out;
            }

            this.executeQueries = this.ExecuteQueries();

            var baseLineFilePath = this.GetBaseLineFilePath();
            string newBaseLineFilePath = baseLineFilePath != null ? baseLineFilePath + ".new" : null;

            if (!string.IsNullOrEmpty(baseLineFilePath))
            {
                this.ReadBaselines(baseLineFilePath);
            }

            if (!string.IsNullOrEmpty(newBaseLineFilePath))
            {
                baselineWriter = new XmlTextWriter(newBaseLineFilePath, Encoding.UTF8);
                baselineWriter.Formatting = Formatting.Indented;
                baselineWriter.Indentation = 2;
                baselineWriter.WriteStartDocument();
                baselineWriter.WriteStartElement("baselines");
            }
        }

        public virtual string GetBaseLineFilePath()
        {
            return null;
        }

        public virtual bool ExecuteQueries()
        {
            return false;
        }

        private void ReadBaselines(string filename)
        {
            if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
            {
                XDocument doc = XDocument.Load(filename);
                this.baselines = doc.Root.Elements("baseline").ToDictionary(e => (string)e.Attribute("key"), e => e.Value);
            }
        }

        public virtual void Teardown()
        {
            if (this.provider != null)
            {
                this.provider.Connection.Close();
            }

            if (this.baselineWriter != null)
            {
                this.baselineWriter.Flush();
                this.baselineWriter.Close();
            }
        }

        public virtual bool CanRunTest(MethodInfo testMethod)
        {
            ExcludeProvider[] exclusions = (ExcludeProvider[])testMethod.GetCustomAttributes(typeof(ExcludeProvider), true);
            foreach (var exclude in exclusions)
            {
                if (
                    // actual name of the provider type
                    string.Compare(this.provider.GetType().Name, exclude.Provider, StringComparison.OrdinalIgnoreCase) == 0
                    // prefix of the provider type xxxQueryProvider
                    || string.Compare(this.provider.GetType().Name, exclude.Provider + "QueryProvider", StringComparison.OrdinalIgnoreCase) == 0
                    // last name of the namespace
                    || string.Compare(this.provider.GetType().Namespace.Split(new[] { '.' }).Last(), exclude.Provider, StringComparison.OrdinalIgnoreCase) == 0
                    )
                {
                    return false;
                }
            }

            return true;
        }

        public virtual void RunTest(Action testAction)
        {
            this.baselineKey = testAction.Method.Name;

            try
            {
                testAction();
            }
            catch (Exception e)
            {
                if (this.queryText != null)
                {
                    throw new TestFailureException(e.Message + "\r\n\r\n" + this.queryText);
                }
                else
                {
                    throw;
                }
            }
        }

        protected void TestQuery(IQueryable query)
        {
            TestQuery((EntityProvider)query.Provider, query.Expression, false);
        }

        protected void TestQuery(Expression<Func<object>> query)
        {
            TestQuery(this.provider, query.Body, false);
        }

        protected void TestQueryFails(IQueryable query)
        {
            TestQuery((EntityProvider)query.Provider, query.Expression, true);
        }

        protected void TestQueryFails(Expression<Func<object>> query)
        {
            TestQuery(this.provider, query.Body, true);
        }

        protected void TestQuery(EntityProvider pro, Expression query, bool expectedToFail)
        {
            if (query.NodeType == ExpressionType.Convert && query.Type == typeof(object))
            {
                query = ((UnaryExpression)query).Operand; // remove box
            }

            this.queryText = null;
            this.queryText = pro.GetQueryText(query);
            WriteBaseline(baselineKey, queryText);

            if (this.executeQueries)
            {
                Exception caught = null;
                try
                {
                    object result = pro.Execute(query);
                    IEnumerable seq = result as IEnumerable;
                    if (seq != null)
                    {
                        // iterate results
                        foreach (var item in seq)
                        {
                        }
                    }
                    else
                    {
                        IDisposable disposable = result as IDisposable;
                        if (disposable != null)
                            disposable.Dispose();
                    }
                }
                catch (Exception e) 
                {
                    caught = e;

                    if (!expectedToFail)
                    {
                        throw new TestFailureException(e.Message + "\r\n\r\n" + queryText);
                    }
                }

                if (caught == null && expectedToFail)
                {
                    throw new InvalidOperationException("Query succeeded when expected to fail");
                }
            }

            string baseline = null;
            if (this.baselines != null && this.baselines.TryGetValue(this.baselineKey, out baseline))
            {
                string trimAct = TrimExtraWhiteSpace(queryText).Trim();
                string trimBase = TrimExtraWhiteSpace(baseline).Trim();
                if (trimAct != trimBase)
                {
                    throw new InvalidOperationException(string.Format("Query translation does not match baseline:\r\n    Expected: {0}\r\n    Actual  : {1}", trimBase, trimAct));
                }
            }

            if (baseline == null && this.baselines != null)
            {
                throw new InvalidOperationException("No baseline");
            }
        }

        private void WriteBaseline(string key, string text)
        {
            if (baselineWriter != null)
            {
                baselineWriter.WriteStartElement("baseline");
                baselineWriter.WriteAttributeString("key", key);
                baselineWriter.WriteWhitespace("\r\n");
                baselineWriter.WriteString(text);
                baselineWriter.WriteEndElement();
            }
        }

        private string TrimExtraWhiteSpace(string s)
        {
            StringBuilder sb = new StringBuilder();
            bool lastWasWhiteSpace = false;
            foreach (char c in s)
            {
                bool isWS = char.IsWhiteSpace(c);
                if (!isWS || !lastWasWhiteSpace)
                {
                    if (isWS)
                        sb.Append(' ');
                    else
                        sb.Append(c);
                    lastWasWhiteSpace = isWS;
                }
            }
            return sb.ToString();
        }

        private void WriteDifferences(string s1, string s2)
        {
            int start = 0;
            bool same = true;
            for (int i = 0, n = Math.Min(s1.Length, s2.Length); i < n; i++)
            {
                bool matches = s1[i] == s2[i];
                if (matches != same)
                {
                    if (i > start)
                    {
                        Console.ForegroundColor = same ? ConsoleColor.Gray : ConsoleColor.White;
                        Console.Write(s1.Substring(start, i - start));
                    }
                    start = i;
                    same = matches;
                }
            }
            if (start < s1.Length)
            {
                Console.ForegroundColor = same ? ConsoleColor.Gray : ConsoleColor.White;
                Console.Write(s1.Substring(start));
            }
            Console.WriteLine();
        }

        protected bool ExecSilent(string commandText)
        {
            try
            {
                this.provider.ExecuteCommand(commandText);
                return true;
            }
            catch (Exception e)
            {
                var msg = e.Message;
                return false;
            }
        }
    }
}
