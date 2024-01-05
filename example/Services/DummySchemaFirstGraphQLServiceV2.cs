using PipServices3.Commons.Refer;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services
{
	public class DummySchemaFirstGraphQLServiceV2 : GraphQLService
	{
		public DummySchemaFirstGraphQLServiceV2() :
			base("schema.graphql")
		{
			_dependencyResolver.Put("controller",
				new Descriptor("pip-services3-dummies", "controller", "default", "*", "*"));
		}

		public override void Register()
		{
			base.Register();
			RegisterEnum<DummyTypes>();
		}
	}
}