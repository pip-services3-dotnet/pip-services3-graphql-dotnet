using Newtonsoft.Json;

namespace PipServices3.GraphQL.Data
{
	public class DummyItem
	{
		[JsonProperty("name")] public string Name { get; set; }

		[JsonProperty("count")]	public int Count { get; set; }
	}
}
