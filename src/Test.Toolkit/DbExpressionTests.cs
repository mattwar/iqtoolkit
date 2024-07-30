using System.Linq.Expressions;
using IQToolkit;
using IQToolkit.Expressions;
using IQToolkit.Data.Expressions;

namespace Test.Toolkit
{
    [TestClass]
    public class DbExpressionTests
    {
        [TestMethod]
        public void TestFindAll()
        {
            // verify that FindAll sees through DbExpressions
            TestFindAll<ConstantExpression>(
                new DbBinaryExpression(
                    typeof(int),
                    false,
                    Expression.Constant(1),
                    "+",
                    Expression.Constant(2)),
                expectedCount: 2
                );
        }

        public void TestFindAll<TExpression>(Expression expression, Func<TExpression, bool>? fnMatch = null, int expectedCount = 1)
            where TExpression : Expression
        {
            var found = expression.FindAll(fnMatch);
            Assert.AreEqual(expectedCount, found.Count);
        }

        [TestMethod]
        public void TestReplace()
        {
            TestReplace(
                new DbBinaryExpression(
                    typeof(int),
                    false,
                    Expression.Constant(1),
                    "+",
                    Expression.Constant(2)),
                exp => exp is ConstantExpression c && c.Value is int iv && iv == 1 ? Expression.Constant(3) : exp
                );
        }

        public void TestReplace<TExpression>(
            TExpression expression, 
            Func<Expression, Expression> fnReplacer, 
            Action<TExpression>? validator = null)
            where TExpression : Expression
        {
            var replaced = expression.Replace(fnReplacer);
            Assert.AreNotSame(expression, replaced);
            if (validator != null)
            {
                validator(replaced);
            }
        }
    }
}