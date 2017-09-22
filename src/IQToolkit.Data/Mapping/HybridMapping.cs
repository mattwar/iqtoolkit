// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace IQToolkit.Data.Mapping
{
    using Common;

    /// <summary>
    /// A <see cref="HybridMapping"/> is a mapping that primarily an attribute based mapping, but if no
    /// attributes are specified it falls back to an alternate <see cref="BasicMapping"/>.
    /// </summary>
    public class HybridMapping : AttributeMapping
    {
        private readonly BasicMapping alternate;

        /// <summary>
        /// Construct a <see cref="HybridMapping"/>
        /// </summary>
        /// <param name="contextType">An optional context type that may supply mapping attributes in its properties.</param>
        /// <param name="alternateMapping">An alternate mapping. If not specified defaults to <see cref="ImplicitMapping"/>.</param>
        public HybridMapping(Type contextType = null, BasicMapping alternateMapping = null)
            : base(contextType)
        {
            this.alternate = alternateMapping ?? new ImplicitMapping();
        }

        protected override IEnumerable<MappingAttribute> GetMappingAttributes(Type elementType, string rootEntityId)
        {
            // get the attributes specified on the context or entity type.
            var list = base.GetMappingAttributes(elementType, rootEntityId).ToList();

            // alternate mapping entity
            var entity = this.alternate.GetEntity(elementType);

            // if no table attribute is mentioned, infer one based on the alternate mapping
            if (!list.OfType<TableAttribute>().Any())
            {
                list.Add(new TableAttribute { Name = this.alternate.GetTableName(entity), EntityType = entity.EntityType });
            }

            var memberAlreadyMapped = new HashSet<string>(list.OfType<MemberAttribute>().Select(m => m.Member));

            // for all the members the alternate mapping considers mapped that do not already have mapping info
            // supplied by attributes, infer mapping attributes based on the alternate mapping.
            foreach (var member in this.alternate.GetMappedMembers(this.alternate.GetEntity(elementType)))
            {
                if (!memberAlreadyMapped.Contains(member.Name))
                {
                    if (this.alternate.IsColumn(entity, member))
                    {
                        list.Add(new ColumnAttribute
                        {
                            Member = member.Name,
                            Name = this.alternate.GetColumnName(entity, member),
                            IsPrimaryKey = this.alternate.IsPrimaryKey(entity, member),
                            IsComputed = this.alternate.IsComputed(entity, member),
                            IsGenerated = this.alternate.IsGenerated(entity, member),
                            IsReadOnly = this.alternate.IsReadOnly(entity, member)
                        });
                    }

                    if (this.alternate.IsRelationship(entity, member))
                    {
                        var relatedEntity = this.alternate.GetRelatedEntity(entity, member);

                        list.Add(new AssociationAttribute
                        {
                            Member = member.Name,
                            KeyMembers = string.Join(", ", this.alternate.GetAssociationKeyMembers(entity, member).Select(m => m.Name)),
                            RelatedEntityType = relatedEntity.EntityType,
                            RelatedEntityId = relatedEntity.EntityId,
                            RelatedKeyMembers = string.Join(", ", this.alternate.GetAssociationRelatedKeyMembers(entity, member).Select(m => m.Name)),
                            IsForeignKey = this.alternate.IsRelationshipSource(entity, member)
                        });
                    }
                }
            }

            return list;
        }
    }
}