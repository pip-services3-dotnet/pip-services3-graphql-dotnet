using PipServices3.GraphQL.Services.Types;

namespace PipServices3.GraphQL.Services
{
	public class DummyCodeFirstGraphQLService : GraphQLService
	{
		public DummyCodeFirstGraphQLService() :
			base(new DummySchema())
		{
		}
	}
}