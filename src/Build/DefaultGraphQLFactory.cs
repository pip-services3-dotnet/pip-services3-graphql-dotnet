using PipServices3.Commons.Refer;
using PipServices3.Components.Build;
using PipServices3.GraphQL.Services;

namespace PipServices3.GraphQL.Build
{
    /// <summary>
    /// Creates GraphQL components by their descriptors.
    /// </summary>
    public class DefaultGraphQLFactory : Factory
    {
        public static Descriptor Descriptor = new Descriptor("pip-services", "factory", "graphql", "default", "1.0");
        public static Descriptor Descriptor3 = new Descriptor("pip-services3", "factory", "graphql", "default", "1.0");

        /// <summary>
        /// Create a new instance of the factory.
        /// </summary>
        public DefaultGraphQLFactory()
        {
        }
    }
}
