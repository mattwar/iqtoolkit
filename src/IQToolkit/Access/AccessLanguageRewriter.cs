// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq.Expressions;

namespace IQToolkit.Access
{
    using Entities;
    using Entities.Translation;

    internal class AccessLanguageRewriter : QueryLanguageRewriter
    {
        public AccessLanguageRewriter(QueryTranslator translator, QueryLanguage language)
            : base(translator, language)
        {
        }

        public override Expression Rewrite(Expression expression)
        {
            var simplified = expression.SimplifyQueries();

            // fix up any order-by's
            var moved = simplified.MoveOrderByToOuterSelect(this.Language);

            // do default language rewrites
            var rewritten = base.Rewrite(moved);

            var isolated = rewritten.IsolateCrossJoins();

            var merged = isolated.MergeSubqueries();
            var skipped = merged.ConvertSkipTakeToTop(this.Language);

            var movedAgain = expression = skipped.MoveOrderByToOuterSelect(this.Language);

            var result = movedAgain.SimplifyQueries();

            return result;
        }
    }
}