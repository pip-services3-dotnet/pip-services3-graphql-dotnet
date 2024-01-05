using GraphQL.Types;
using PipServices3.Commons.Data;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class DataPageOfDummyType: ObjectGraphType<DataPage<Dummy>>
	{
		public DataPageOfDummyType() 
		{
			Field(x => x.Total, nullable: true);
			Field<ListGraphType<DummyType>>("data").Resolve(x => x.Source?.Data);
		}
	}
}
