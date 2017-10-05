// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace IQToolkit.Data.Mapping
{
    using Common;

    /// <summary>
    /// A <see cref="QueryMapping"/> stored in XML.
    /// </summary>
    public class XmlMapping : AttributeMapping
    {
        private readonly Dictionary<string, XElement> entities;
        private static readonly XName EntityElementName = XName.Get("Entity");
        private static readonly XName NestedEntityElementName = XName.Get("NestedEntity");
        private static readonly XName EntityIdPropertyName = XName.Get(nameof(EntityAttribute.Id));
        private static readonly XName NestedEntityMemberName = XName.Get(nameof(MemberAttribute.Member));
        
        public XmlMapping(XElement root)
            : base(contextType: null)
        {
            this.entities = root.Descendants()
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

        private static string GetEntityIdPart(XElement element)
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
        /// Creates a <see cref="XmlMapping"/> from xml text.
        /// </summary>
        public static XmlMapping FromXml(string xml)
        {
            return new XmlMapping(XElement.Parse(xml));
        }

        protected override void GetDeclaredMappingAttributes(Type entityType, string entityId, ParentEntity parent, List<MappingAttribute> list)
        {
            XElement root;

            if (this.entities.TryGetValue(entityId, out root))
            {
                if (root.Name == EntityElementName)
                {
                    list.Add(this.GetMappingAttribute(root));
                }

                foreach (var elem in root.Elements())
                {
                    if (elem != null)
                    {
                        list.Add(this.GetMappingAttribute(elem));
                    }
                }
            }
        }

        private MappingAttribute GetMappingAttribute(XElement element)
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
            foreach (var prop in attrType.GetProperties())
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

        private Type FindType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}