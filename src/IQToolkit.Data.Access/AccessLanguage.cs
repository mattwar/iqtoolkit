// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.Access
{
    using Expressions;
    using Translation;

    /// <summary>
    /// TSQL specific QueryLanguage
    /// </summary>
    public class AccessLanguage : QueryLanguage
    {
        AccessTypeSystem typeSystem = new AccessTypeSystem();

        public override QueryTypeSystem TypeSystem
        {
            get { return this.typeSystem; }
        }

        public override string Quote(string name)
        {
            if (name.StartsWith("[") && name.EndsWith("]"))
            {
                return name;
            }
            else 
            {
                return "[" + name + "]";
            }
        }

        public override Expression GetGeneratedIdExpression(MemberInfo member)
        {
            return new FunctionExpression(TypeHelper.GetMemberType(member), "@@IDENTITY", null);
        }

        public override QueryLinguist CreateLinguist(QueryTranslator translator)
        {
            return new AccessLinguist(this, translator);
        }

        class AccessLinguist : QueryLinguist
        {
            public AccessLinguist(AccessLanguage language, QueryTranslator translator)
                : base(language, translator)
            {
            }

            public override Expression Translate(Expression expression)
            {
                // fix up any order-by's
                expression = OrderByRewriter.Rewrite(this.Language, expression);

                expression = base.Translate(expression);

                expression = CrossJoinIsolator.Isolate(expression);
                expression = SkipToNestedOrderByRewriter.Rewrite(this.Language, expression);
                expression = OrderByRewriter.Rewrite(this.Language, expression);
                expression = UnusedColumnRemover.Remove(expression);
                expression = RedundantSubqueryRemover.Remove(expression);

                return expression;
            }

            public override string Format(Expression expression)
            {
                return AccessFormatter.Format(expression);
            }
        }

        private static AccessLanguage? _default;

        public static AccessLanguage Default
        {
            get
            {
                if (_default == null)
                {
                    Interlocked.CompareExchange(ref _default, new AccessLanguage(), null);
                }
                return _default;
            }
        }
    }
}