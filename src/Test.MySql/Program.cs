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
            var provider = DbEntityProvider.From("IQToolkit.Data.MySqlClient", "Northwind", "Test.MySqlNorthwind");
        }
    }

    public class MySqlNorthwind : NorthwindWithAttributes
    {
        public MySqlNorthwind(IEntityProvider provider)
            : base(provider)
        {
        }

        [Table(Name = "Order_Details")]
        public override IEntityTable<OrderDetail> OrderDetails
        {
            get { return base.OrderDetails; }
        }
    }
}
