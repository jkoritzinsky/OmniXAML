﻿namespace OmniXaml.Parsers.ProtoParser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Typing;

    internal class SuperProtoParser
    {
        private readonly WiringContext wiringContext;
        private readonly ProtoNodeBuilder nodeBuilder;
        private XmlReader reader;

        public SuperProtoParser(WiringContext wiringContext)
        {
            this.wiringContext = wiringContext;
            nodeBuilder = new ProtoNodeBuilder(wiringContext.TypeContext);
        }

        public IEnumerable<ProtoXamlNode> Parse(string xml)
        {
            reader = XmlReader.Create(new StringReader(xml));
            reader.Read();

            return ParseElement();
        }

        private IEnumerable<ProtoXamlNode> ParseEmptyElement(XamlType xamlType)
        {
            var emptyElement = nodeBuilder.EmptyElement(xamlType.UnderlyingType, wiringContext.TypeContext.GetNamespaceForPrefix(reader.Prefix));
            foreach (var protoXamlNode in CommonNodesOfElement(xamlType, emptyElement)) yield return protoXamlNode;
        }

        private IEnumerable<ProtoXamlNode> CommonNodesOfElement(XamlType owner, ProtoXamlNode elementToInject)
        {
            var rawAttributes = GetAttributes(owner).ToList();

            foreach (var node in GetPrefixDefinitions(rawAttributes).Select(ConvertAttributeToNsPrefixDefinition)) yield return node;

            yield return elementToInject;

            foreach (var node in GetAttributes(rawAttributes).Select(ConvertAttributeToNode)) yield return node;
        }

        private IEnumerable<ProtoXamlNode> ParseExpandedElement(XamlType xamlType)
        {
            var element = nodeBuilder.NonEmptyElement(xamlType.UnderlyingType, wiringContext.TypeContext.GetNamespaceForPrefix(reader.Prefix));
            foreach (var node in CommonNodesOfElement(xamlType, element)) yield return node;

            reader.Read();

            if (reader.NodeType != XmlNodeType.EndElement)
            {
                SkipWhitespaces();

                var memberName = GetMemberName(reader.LocalName);
                yield return nodeBuilder.NonEmptyPropertyElement(xamlType.UnderlyingType, memberName, "root");

                reader.Read();

                foreach (var p in ParseElement()) yield return p;

                yield return nodeBuilder.Text();
                yield return nodeBuilder.EndTag();
            }

            yield return nodeBuilder.EndTag();
        }

        private void AssertValidElement()
        {
            if (!(reader.NodeType == XmlNodeType.Element && !reader.LocalName.Contains(".")))
            {
                throw new XamlParseException("The root should be an element.");
            }
        }

        private IEnumerable<RawAttribute> GetAttributes(IEnumerable<RawAttribute> rawAttributes)
        {
            return rawAttributes.Where(attribute => !IsPrefixDeclaration(attribute));
        }

        private static bool IsPrefixDeclaration(RawAttribute attribute)
        {
            return attribute.Descriptor.Locator.PropertyName.Contains("xmlns") || attribute.Descriptor.Locator.Prefix.Contains("xmlns");
        }

        private ProtoXamlNode ConvertAttributeToNode(RawAttribute rawAttribute)
        {
            XamlMember member;

            if (rawAttribute.Descriptor.Locator.IsDotted)
            {
                var ownerName = rawAttribute.Descriptor.Locator.Owner.PropertyName;
                var ownerPrefix = rawAttribute.Descriptor.Locator.Owner.Prefix;

                var owner = wiringContext.TypeContext.GetByPrefix(ownerPrefix, ownerName);

                member = owner.GetAttachableMember(rawAttribute.Name);
            }
            else
            {
                member = rawAttribute.ContainingType.GetMember(rawAttribute.Name);
            }
            
            return nodeBuilder.Attribute(member, rawAttribute.Value);
        }

        private IEnumerable<RawAttribute> GetPrefixDefinitions(IEnumerable<RawAttribute> rawAttributes)
        {
            return rawAttributes.Where(IsPrefixDeclaration);
        }

        private IEnumerable<RawAttribute> GetAttributes(XamlType containingType)
        {
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    var propertyOwner = GetPropertyOwner(containingType, reader.Name);
                    var propertyName = GetPropertyName(reader.Name);
                    var propLocator = PropertyLocator.Parse(reader.Name);
                    var attributeDescriptor = new AttributeDescriptor(propLocator, containingType, propertyOwner, propertyName);

                    yield return new RawAttribute(attributeDescriptor, reader.Value);
                } while (reader.MoveToNextAttribute());

                reader.MoveToElement();
            }
        }

        private string GetPropertyName(string propertyName)
        {
            if (propertyName.Contains("xmlns"))
            {
                return propertyName;
            }

            var parts = propertyName.Split('.');

            if (parts.Count() > 1)
            {
                return parts[1];
            }

            return propertyName;
        }

        private XamlType GetPropertyOwner(XamlType containingType, string propertyName)
        {
            if (propertyName.Contains("xmlns"))
            {
                return containingType;
            }

            var parts = propertyName.Split('.');

            if (parts.Count() > 1)
            {
                var typeName = parts.First();
                string prefix = string.Empty;
                return wiringContext.TypeContext.GetByPrefix(prefix, typeName);
            }

            return containingType;
        }

        private IEnumerable<ProtoXamlNode> ParseElement()
        {
            SkipWhitespaces();

            AssertValidElement();

            var childType = CurrentType;

            if (reader.IsEmptyElement)
            {
                foreach (var node in ParseEmptyElement(childType)) yield return node;
            }
            else
            {
                foreach (var node in ParseExpandedElement(childType)) yield return node;
            }
        }

        private static string GetMemberName(string propertyName)
        {
            var indexOfDot = propertyName.IndexOf(".") + 1;
            return propertyName.Substring(indexOfDot, propertyName.Length - indexOfDot);
        }

        private void SkipWhitespaces()
        {
            while (reader.NodeType == XmlNodeType.Whitespace)
            {
                reader.Read();
            }
        }

        private XamlType CurrentType => wiringContext.TypeContext.GetByPrefix(reader.Prefix, reader.LocalName);

        private ProtoXamlNode ConvertAttributeToNsPrefixDefinition(RawAttribute rawAttribute)
        {
            var value = rawAttribute.Value;
            var propName = new Property(rawAttribute.Name);
            return nodeBuilder.NamespacePrefixDeclaration(value, propName.Name);
        }
    }
}