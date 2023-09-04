using HotChocolate.Types;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class QueryType : ObjectType<IDummyController>
	{
		protected override void Configure(IObjectTypeDescriptor<IDummyController> descriptor)
		{
			//descriptor.Field(x => x.GetDummiesAsync()).Type<DataPageType>();
		}
	}
}
