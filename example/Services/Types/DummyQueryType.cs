using GraphQL.Types;
using PipServices3.Commons.Refer;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyQueryType : ObjectGraphType, IReferenceable
	{
		private DummyGraphQLOperations _operations = new DummyGraphQLOperations();

		public DummyQueryType()
		{
			Field<DummyType>("dummy")
				.Argument<IntGraphType>("id")
				.ResolveAsync(_operations.GetDummyAsync);

			Field<DataPageOfDummyType>("dummies")
				.Argument<StringGraphType>("correlationId")
				.Argument<StringGraphType>("filter")
				.Argument<PagingParamsInputType>("paging")
				.Argument<ListGraphType<NonNullGraphType<SortFieldType>>>("sort")
				.ResolveAsync(_operations.GetDummiesAsync);
		}

		public void SetReferences(IReferences references)
		{
			_operations.SetReferences(references);
		}
	}
}
