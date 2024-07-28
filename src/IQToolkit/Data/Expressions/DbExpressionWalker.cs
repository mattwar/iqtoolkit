using System;
using System.Linq.Expressions;

namespace IQToolkit.Data.Expressions
{
    public static class DbExpressionWalker
    {
        /// <summary>
        /// Walks the entire expression tree top-down and bottom-up.
        /// </summary>
        /// <param name="root">The expression at the top of the sub-tree.</param>
        /// <param name="fnBefore">The optional callback called for each expression when first encountered walking down.</param>
        /// <param name="fnAfter">The optional callback called for each expression when next enountered walking back up.</param>
        /// <param name="fnDescend">The optional callback used to determine if the walking should descend in the children.</param>
        public static void Walk(
            Expression root,
            Action<Expression>? fnBefore = null,
            Action<Expression>? fnAfter = null,
            Func<Expression, bool>? fnDescend = null)
        {
            var walker = new Walker(fnBefore, fnAfter, fnDescend);
            walker.Rewrite(root);
        }

        private class Walker : DbExpressionRewriter
        {
            private readonly Action<Expression>? _fnBefore;
            private readonly Action<Expression>? _fnAfter;
            private readonly Func<Expression, bool>? _fnDescend;

            public Walker(
                Action<Expression>? fnBefore,
                Action<Expression>? fnAfter,
                Func<Expression, bool>? fnDescend)
            {
                _fnBefore = fnBefore;
                _fnAfter = fnAfter;
                _fnDescend = fnDescend;
            }

            public override Expression Rewrite(Expression exp)
            {
                _fnBefore?.Invoke(exp);

                if (_fnDescend == null || _fnDescend(exp))
                {
                    base.Rewrite(exp);
                }

                _fnAfter?.Invoke(exp);

                return exp;
            }
        }
    }
}
