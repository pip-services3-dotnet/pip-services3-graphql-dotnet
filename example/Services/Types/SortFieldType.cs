using GraphQL.Types;
using PipServices3.Commons.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class SortFieldType: InputObjectGraphType<SortField>
	{
		public SortFieldType() 
		{
			Field(x => x.Name);
			Field(x => x.Ascending);
		}
	}
}
