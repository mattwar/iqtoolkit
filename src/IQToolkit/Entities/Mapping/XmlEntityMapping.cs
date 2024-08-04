// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace IQToolkit.Entities.Mapping
{
    using Utils;

#if false
    /// <summary>
    /// A <see cref="EntityMapping"/> stored in XML elements.
    /// </summary>
    public class XmlEntityMapping : AttributeEntityMapping
    {
        private readonly IReadOnlyList<Assembly> _assemblies;
        private readonly Dictionary<string, XElement> _entities;

        private static readonly XName EntityElementName = XName.Get("Entity");
        private static readonly XName NestedEntityElementName = XName.Get("NestedEntity");
        private static readonly XName EntityIdPropertyName = XName.Get(nameof(EntityAttribute.Id));
        private static readonly XName NestedEntityMemberName = XName.Get(nameof(MemberAttribute.Member));

        /// <summary>
        /// Constructs a new instance of <see cref="XmlEntityMapping"/>
        /// </summary>
        /// <param name="root">The root node of the xml mapping tree that contains the entity elements.</param>
        /// <param name="assemblies">A list of zero or more assemblies that will be used to find types mentioned in the mapping.</param>
        public XmlEntityMapping(XElement root, IEnumerable<Assembly> assemblies)
            : base(contextType: null)
        {
            _assemblies = assemblies.ToReadOnly();
            _entities = root.Descendants()
                .Where(e => e.Name == EntityElementName || e.Name == NestedEntityElementName)
                .ToDictionary(GetEntityId);
        }

        private static string GetEntityId(XElement element)
        {
            // get elements involved in the id, skip the root element of the doc.
            var elements = element.AncestorsAndSelf().Reverse().Skip(1);
            var id = string.Join(".", elements.Select(GetEntityIdPart));
            return id;
        }

        private static string? GetEntityIdPart(XElement element)
        {
            if (element.Name == EntityElementName)
            {
                return (string)element.Attribute(EntityIdPropertyName);
            }
            else if (element.Name == NestedEntityElementName)
            {
                return (string)element.Attribute(NestedEntityMemberName);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a <see cref="XmlEntityMapping"/> from xml text.
        /// </summary>
        /// <param name="xml">The text of the xml mapping.</param>
        /// <param name="assemblies">A list of zero or more assemblies that will be used to find types mentioned in the mapping.</param>
        public static XmlEntityMapping FromXml(string xml, IEnumerable<Assembly> assemblies)
        {
            return new XmlEntityMapping(XElement.Parse(xml), assemblies.ToReadOnly());
        }

        /// <summary>
        /// Creates a <see cref="XmlEntityMapping"/> from xml text.
        /// </summary>
        /// <param name="xml">The text of the xml mapping.</param>
        /// <param name="assemblies">A list of zero or more assemblies that will be used to find types mentioned in the mapping.</param>
        public static XmlEntityMapping FromXml(string xml, params Assembly[] assemblies)
        {
            return FromXml(xml, (IEnumerable<Assembly>)assemblies);
        }

        protected override void GetDeclaredMappingAttributes(Type entityType, string entityId, ParentEntity? parent, List<MappingAttribute> list)
        {
            XElement root;

            if (_entities.TryGetValue(entityId, out root))
            {
                if (root.Name == EntityElementName
                    && this.GetMappingAttribute(root) is { } rootAttr)
                {
                    list.Add(rootAttr);
                }

                foreach (var elem in root.Elements())
                {
                    if (elem != null
                        && this.GetMappingAttribute(elem) is { } attr)
                    {
                        list.Add(attr);
                    }
                }
            }
        }

        private MappingAttribute? GetMappingAttribute(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "Entity":
                    return this.GetMappingAttribute(typeof(EntityAttribute), element);                
                case "Table":
                    return this.GetMappingAttribute(typeof(TableAttribute), element);
                case "ExtensionTable":
                    return this.GetMappingAttribute(typeof(ExtensionTableAttribute), element);
                case "Column":
                    return this.GetMappingAttribute(typeof(ColumnAttribute), element);
                case "Association":
                    return this.GetMappingAttribute(typeof(AssociationAttribute), element);
                case "NestedEntity":
                    return this.GetMappingAttribute(typeof(NestedEntityAttribute), element);
                default:
                    return null;
            }
        }

        private MappingAttribute GetMappingAttribute(Type attrType, XElement element)
        {
            var ma = (MappingAttribute)Activator.CreateInstance(attrType);

            foreach (var prop in attrType.GetDeclaredFieldsAndProperties().OfType<PropertyInfo>())
            {
                var xa = element.Attribute(prop.Name);
                if (xa != null)
                {
                    if (prop.PropertyType == typeof(Type))
                    {
                        prop.SetValue(ma, this.FindType(xa.Value), null);
                    }
                    else
                    {
                        prop.SetValue(ma, Convert.ChangeType(xa.Value, prop.PropertyType), null);
                    }
                }
            }
            return ma;
        }

        private Type? FindType(string name)
        {
            foreach (var assembly in _assemblies)
            {
                Type type = assembly.GetType(name);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
#endif
}