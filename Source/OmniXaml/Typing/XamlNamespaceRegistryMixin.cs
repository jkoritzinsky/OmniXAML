namespace OmniXaml.Typing
{
    using System.Collections.Generic;
    using System.Reflection;
    using Builder;

    public static class XamlNamespaceRegistryMixin
    {
        public static void FillFromAttributes(this IXamlNamespaceRegistry nsReg, IEnumerable<Assembly> assemblies)
        {
            var namespaces = XamlNamespace.DefinedInAssemblies(assemblies);

            foreach (var xamlNamespace in namespaces)
            {
                nsReg.AddNamespace(xamlNamespace);
            }            
        }
    }
}