namespace OmniXaml.Tests.Wpf
{
    using Glass;
    using OmniXaml.Wpf;

    public class GivenAXamlXmlLoader
    {
        protected GivenAXamlXmlLoader()
        {
            XamlLoader = new WpfXamlLoader(new TypeFactory());
        }

        private IXamlLoader XamlLoader { get; }
        
        protected object LoadXaml(string xamlContent)
        {
            using (var stream = xamlContent.ToStream())
            {
                return XamlLoader.Load(stream);
            }
        }
    }
}