using GraphQL.Utilities.Federation;

namespace PipServices3.GraphQL.Types
{
	public class AnyGraphType: AnyScalarGraphType
	{
		public AnyGraphType() 
			: base()
		{
			Name = "Any";
		}
	}
}
