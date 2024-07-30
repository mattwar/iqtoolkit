using System;
using System.Linq.Expressions;

namespace IQToolkit.Expressions
{
    public abstract class ExtendedExpression : Expression
    {
        protected ExtendedExpression(Type type)
            : base()
        {
        }

        public static readonly ExpressionType EType = (ExpressionType)(-1);
        public override ExpressionType NodeType => EType;

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            return this.VisitChildren(visitor);
        }
    }

    public class CustomRewriter : ExpressionVisitor
    {
        private readonly Func<Expression, Expression, Expression> fnRewriter;

        public CustomRewriter(Func<Expression, Expression, Expression> fnRewriter)
        {
            this.fnRewriter = fnRewriter;
        }

        public override Expression Visit(Expression original)
        {
            if (original == null)
                return null!;

            var modified = base.Visit(original);
            return fnRewriter(modified, original);
        }
    }
}
