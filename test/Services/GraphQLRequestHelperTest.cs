using Xunit;

namespace PipServices3.GraphQL.Services
{
	[Collection("Sequential")]
	public class GraphQLRequestHelperTest
	{
		[Fact]
		public void It_Should_Format_Name()
		{
			var nameInCamelCase = "displayName";
			var nameInSnakeCase = "display_name";

			Assert.Equal(nameInSnakeCase, GraphQLRequestHelper.ConvertCamelToSnake(nameInCamelCase));
			Assert.Equal(nameInCamelCase, GraphQLRequestHelper.ConvertSnakeToCamel(nameInSnakeCase));

			// leave as is
			Assert.Equal(nameInSnakeCase, GraphQLRequestHelper.ConvertCamelToSnake(nameInSnakeCase));
			Assert.Equal(nameInCamelCase, GraphQLRequestHelper.ConvertSnakeToCamel(nameInCamelCase));
		}
	}
}
