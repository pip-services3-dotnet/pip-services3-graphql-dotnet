using HotChocolate.Types;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyType : ObjectType<Dummy>
	{
		protected override void Configure(IObjectTypeDescriptor<Dummy> descriptor)
		{
			descriptor.Field(t => t.Id).Type<NonNullType<StringType>>();
			descriptor.Field(t => t.Key).Type<StringType>();
			descriptor.Field(t => t.Content).Type<StringType>();
			descriptor.Field(t => t.Flag).Type<BooleanType>();
			descriptor.Field(t => t.Param).Type<DummyParamType>();
			descriptor.Field(t => t.Items).Type<ListType<NonNullType<DummyItemType>>>();
			descriptor.Field(t => t.Tags).Type<ListType<NonNullType<StringType>>>();
			descriptor.Field(t => t.Date).Type<DateTimeType>();
		}
	}
}
