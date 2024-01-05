using GraphQL.Types;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyType: ObjectGraphType<Dummy>
	{
		public DummyType() 
		{
			Field(x => x.Id);
			Field(x => x.Key);
			Field(x => x.Content);
			Field(x => x.Flag);
			Field<DummyParamType>("param").Resolve(x => x.Source);
			Field<ListGraphType<NonNullGraphType<DummyItemType>>>("items").Resolve(x => x.Source);
			Field<ListGraphType<StringGraphType>>("tags").Resolve(x => x.Source);
			Field(x => x.Date);
		}
	}
}
