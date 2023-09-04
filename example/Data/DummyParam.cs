using Newtonsoft.Json;

namespace PipServices3.GraphQL.Data
{
	public class DummyParam
	{
		[JsonProperty("name")]	public string Name { get; set; }

		[JsonProperty("value")]	public double Value { get; set; }
	}
}
