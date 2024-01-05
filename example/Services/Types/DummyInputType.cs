using GraphQL.Types;
using PipServices3.GraphQL.Data;
using PipServices3.GraphQL.Types;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyInputType : InputObjectGraphType<Dummy>
	{
		public DummyInputType() 
		{
			Name = "DummyInput";

			Field(x => x.Id);
			Field(x => x.Key);
			Field(x => x.Content);
			Field(x => x.Flag);
			Field<DummyParamInputType>("param");
			Field<ListGraphType<NonNullGraphType<DummyItemInputType>>>("items");
			Field<ListGraphType<StringGraphType>>("tags");
			Field(x => x.Date);
			Field(x => x.StartTime);
		}
	}
}
