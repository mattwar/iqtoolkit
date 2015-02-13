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
            new TestRunner(args, System.Reflection.Assembly.GetEntryAssembly()).RunTests();
        }

        private static DbEntityProvider CreateNorthwindProvider(string mapping = "Test.NorthwindWithAttributes")
        {
            return DbEntityProvider.From(@"Northwind.mdf", mapping);
        }

        public class NorthwindTranslationTests : Test.NorthwindTranslationTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
        }

        public class NorthwindExecutionTests : Test.NorthwindExecutionTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
        }

        public class NorthwindCUDTests : Test.NorthwindCUDTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
        }

        public class MultiTableTests : Test.MultiTableTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider("Test.MultiTableContext");
            }
        }

#if !DEBUG
        public class NorthwindPerfTests : Test.NorthwindPerfTests
        {
            protected override DbEntityProvider CreateProvider()
            {
                return CreateNorthwindProvider();
            }
        }
#endif
    }
}
