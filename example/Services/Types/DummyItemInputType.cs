using GraphQL.Types;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyItemInputType : InputObjectGraphType<DummyItem>
	{
		public DummyItemInputType() 
		{
			Name = "DummyItemInput";
			Field(x => x.Name).Description("The name of the DummyItem.");
			Field(x => x.Count).Description("The count of the DummyItem.");
		}
	}
}
