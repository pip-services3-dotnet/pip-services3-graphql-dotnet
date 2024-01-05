using GraphQL.Types;
using PipServices3.Commons.Refer;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyMutationType : ObjectGraphType, IReferenceable
	{
		private DummyGraphQLOperations _operations = new DummyGraphQLOperations();

		public DummyMutationType()
		{
			Field<DummyType>("createDummy")
				.Argument<StringGraphType>("correlationId")
				.Argument<NonNullGraphType<DummyInputType>>("dummy")
				.ResolveAsync(_operations.CreateDummyAsync);

			Field<DummyType>("updateDummy")
				.Argument<StringGraphType>("correlationId")
				.Argument<NonNullGraphType<DummyInputType>>("dummy")
				.ResolveAsync(_operations.UpdateDummyAsync);

			Field<DummyType>("deleteDummy")
				.Argument<StringGraphType>("correlationId")
				.Argument<NonNullGraphType<StringGraphType>>("id")
				.ResolveAsync(_operations.DeleteDummyAsync);
		}

		public void SetReferences(IReferences references)
		{
			_operations.SetReferences(references);
		}
	}
}
