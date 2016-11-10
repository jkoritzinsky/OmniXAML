﻿namespace OmniXaml.DefaultLoader
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Ambient;
    using Tests;

    public class XamlLoader : IXamlLoader
    {
        private readonly ObjectBuilderContext objectBuilderContext;
        private readonly ITypeDirectory directory;
        private readonly AttributeBasedMetadataProvider metadataProvider;

        public XamlLoader(IList<Assembly> assemblies)
        {
            directory = new AttributeBasedTypeDirectory(assemblies);
            metadataProvider = new AttributeBasedMetadataProvider();
            objectBuilderContext = new ObjectBuilderContext(new InstanceCreator(), new SourceValueConverter(), metadataProvider);
        }

        public ConstructionResult Load(string xaml)
        {
            var ct = Parse(xaml);
            return Construct(ct);
        }

        public ConstructionResult Load(string xaml, object intance)
        {
            throw new NotImplementedException();
        }

        public static XamlLoader FromAttributes(params Assembly[] assemblies)
        {
            return new XamlLoader(assemblies);
        }

        private ConstructionResult Construct(ConstructionNode ctNode)
        {
            
            var namescopeAnnotator = new NamescopeAnnotator(metadataProvider);
            var trackingContext = new BuildContext(namescopeAnnotator, new AmbientRegistrator(), new InstanceLifecycleSignaler());
            var objectConstructor = new ObjectBuilder(objectBuilderContext, new ContextFactory(directory, objectBuilderContext));
            var construct = objectConstructor.Create(ctNode, trackingContext);
            return new ConstructionResult(construct, namescopeAnnotator);
        }


        private ConstructionNode Parse(string xaml)
        {
            var sut = new XamlToTreeParser(directory, metadataProvider, new[] {new InlineParser(directory)});

            var tree = sut.Parse(xaml);
            return tree;
        }
    }
}