using HotChocolate.Types;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyParamType: ObjectType<DummyParam>
	{
		protected override void Configure(IObjectTypeDescriptor<DummyParam> descriptor)
		{
			descriptor.Field(t => t.Name).Type<StringType>();
			descriptor.Field(t => t.Value).Type<FloatType>();
		}
	}
}
