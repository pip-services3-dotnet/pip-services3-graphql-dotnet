using HotChocolate.Types;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyItemType: ObjectType<DummyItem>
	{
		protected override void Configure(IObjectTypeDescriptor<DummyItem> descriptor)
		{
			descriptor.Field(t => t.Name).Type<StringType>();
			descriptor.Field(t => t.Count).Type<IntType>();
		}
	}
}
