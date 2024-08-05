// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Access
{
    using Entities;
    using IQToolkit.Entities.Translation;

    /// <summary>
    /// Microsoft Access SQL <see cref="QueryLanguage"/>
    /// </summary>
    public sealed class AccessLanguage : SqlQueryLanguage
    {
        protected override LanguageTranslator Linguist { get; }

        private AccessLanguage()
        {
            this.Linguist = new AccessLinguist(this);
        }

        public static readonly AccessLanguage Singleton =
            new AccessLanguage();

        public override QueryTypeSystem TypeSystem => 
            AccessTypeSystem.Singleton;
    }
}