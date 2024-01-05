using GraphQL.Types;
using PipServices3.Commons.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class PagingParamsInputType: InputObjectGraphType<PagingParams>
	{
		public PagingParamsInputType() 
		{
			Name = "PagingParams";

			Field(x => x.Total);
			Field(x => x.Skip, nullable: true);
			Field(x => x.Take, nullable: true);
		}
	}
}
