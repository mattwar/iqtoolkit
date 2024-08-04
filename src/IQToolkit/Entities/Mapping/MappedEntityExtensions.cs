// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq;

namespace IQToolkit.Entities.Mapping
{
    public static class MappedEntityExtensions
    {
        /// <summary>
        /// Gets the member for the given name.
        /// </summary>
        public static bool TryGetMemberByName<TMember>(this IReadOnlyList<TMember> members, string name, out TMember member)
            where TMember : MappedMember
        {
            member = members.FirstOrDefault(m => m.Member.Name == name);
            return member != null;
        }

        /// <summary>
        /// Gets the member for the given name.
        /// </summary>
        public static bool TryGetTableByName(this IReadOnlyList<MappedTable> tables, string name, out MappedTable table)
        {
            table = tables.FirstOrDefault(t => t.TableName == name);
            return table != null;
        }
    }
}