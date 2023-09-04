using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PipServices3.Commons.Data;
using PipServices3.Commons.Refer;

namespace PipServices3.GraphQL.Services
{
	public class DummyCodeFirstGraphQLService : GraphQLService
    {
        public DummyCodeFirstGraphQLService() : 
            base()
        {
			_dependencyResolver.Put("controller", new Descriptor("pip-services3-dummies", "controller", "default", "default", "1.0"));
		}

		public override void Register(IRequestExecutorBuilder builder)
        {
			base.Register(builder);

			builder.AddTypeConverter<string, FilterParams>(x =>
            {
                return FilterParams.FromString(x.ToString());
            });			
        }
    }
}