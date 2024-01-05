using Newtonsoft.Json;
using PipServices3.Commons.Data;
using System;
using System.Collections.Generic;

namespace PipServices3.GraphQL.Data
{
    public class Dummy : IStringIdentifiable
    {
        public Dummy()
        {
        }

        public Dummy(string id, string key, string content, bool flag = true)
        {
            Id = id;
            Key = key;
            Content = content;
            Flag = flag;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("flag")]
        public bool Flag { get; set; }

        [JsonProperty("param")]
        public DummyParam Param { get; set; }

        [JsonProperty("items")]
        public List<DummyItem> Items { get; set; } = new List<DummyItem>();

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("date")]
        public DateTime Date { get; set; } = DateTime.MinValue.ToUniversalTime();

		[JsonProperty("startTime")]
		public TimeSpan StartTime { get; set; } = DateTime.Now - DateTime.MinValue.ToUniversalTime();

		[JsonProperty("anyField")]
        public object AnyField { get; set; }

		[JsonProperty("dummyType")]
		public DummyTypes DummyType { get; set; }
	}
}
