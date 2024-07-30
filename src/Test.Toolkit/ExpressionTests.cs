using System.Linq.Expressions;
using IQToolkit;
using IQToolkit.Expressions;

namespace Test.Toolkit
{
    [TestClass]
    public class ExpressionTests
    {
        [TestMethod]
        public void TestExtension()
        {
            var expr = new MyExpression(Expression.Constant(100));
            var visitor = new MyVisitor();
            visitor.Visit(expr);
            Assert.IsTrue(visitor.ExtensionVisited);
            Assert.IsTrue(visitor.OtherVisited);
        }

        public class MyExpression : Expression
        {
            public Expression Other { get; }

            public MyExpression(Expression other)
            {
                this.Other = other;
            }

            public override ExpressionType NodeType => ExpressionType.Extension;
            public override Type Type => typeof(int);

            protected override Expression VisitChildren(ExpressionVisitor visitor)
            {
                var other = visitor.Visit(this.Other);
                return this;
            }
        }

        public class MyVisitor : ExpressionVisitor
        {
            public bool ExtensionVisited = false;
            public bool OtherVisited = false;

            protected override Expression VisitExtension(Expression original)
            {
                this.ExtensionVisited = true;
                return base.VisitExtension(original);
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                this.OtherVisited = true;
                return base.VisitConstant(node);
            }
        }

        [TestMethod]
        public void TestFindAll()
        {
            TestFindAll<ConstantExpression>(
                Expression.Add(Expression.Constant(1), Expression.Constant(2)),
                expectedCount: 2
                );
        }

        public void TestFindAll<TExpression>(Expression expression, Func<TExpression, bool>? fnMatch = null, int expectedCount = 1)
            where TExpression : Expression
        {
            var found = expression.FindAll(fnMatch);
            Assert.AreEqual(expectedCount, found.Count);
        }
    }
}
