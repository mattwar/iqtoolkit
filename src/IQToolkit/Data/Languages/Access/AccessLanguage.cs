// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.Access
{
    using Expressions;
    using IQToolkit.Data.Translation;
    using Utils;

    /// <summary>
    /// Microsoft Access SQL <see cref="QueryLanguage"/>
    /// </summary>
    public sealed class AccessLanguage : QueryLanguage, ICreateLanguageTranslator
    {
        private AccessLanguage()
        {
        }

        public static readonly AccessLanguage Singleton =
            new AccessLanguage();

        public override QueryTypeSystem TypeSystem => 
            AccessTypeSystem.Singleton;

        public override QueryFormatter Formatter =>
            AccessFormatter.Singleton;

        QueryLanguageRewriter ICreateLanguageTranslator.CreateLanguageTranslator(QueryTranslator translator) =>
            new AccessLanguageRewriter(translator, this);

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
            return new FunctionCallExpression(TypeHelper.GetMemberType(member), false, "@@IDENTITY", null);
        }
    }

}