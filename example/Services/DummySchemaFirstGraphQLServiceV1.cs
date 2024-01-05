using PipServices3.Commons.Config;
using PipServices3.Commons.Refer;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services
{
	public class DummySchemaFirstGraphQLServiceV1 : GraphQLService
	{
		private DummyGraphQLOperations _operations = new DummyGraphQLOperations();

		public DummySchemaFirstGraphQLServiceV1() :
			base("schema.graphql")
		{
		}

		public override void Configure(ConfigParams config)
		{
			base.Configure(config);
		}

		public override void SetReferences(IReferences references)
		{
			base.SetReferences(references);

			_operations.SetReferences(references);
		}

		public override void Register()
		{
			base.Register();

			RegisterQuery("dummies", _operations.GetDummiesAsync);
			RegisterQuery("dummy", _operations.GetDummyAsync);
			RegisterMutation("createDummy", _operations.CreateDummyAsync);
			RegisterMutation("updateDummy", _operations.UpdateDummyAsync);
			RegisterMutation("deleteDummy", _operations.DeleteDummyAsync);

			RegisterEnum<DummyTypes>();
		}
	}
}