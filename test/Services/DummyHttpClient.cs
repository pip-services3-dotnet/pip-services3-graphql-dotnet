using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using PipServices3.Commons.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PipServices3.Commons.Config;

namespace PipServices3.GraphQL.Services
{
	internal class DummyHttpClient
	{
		private readonly string _host = "localhost";
		private readonly int _port = 3003;
		private readonly string _baseRoute;

		private HttpClient _httpClient;
		private readonly Dictionary<string, string> _queries;

		public DummyHttpClient(ConfigParams config, Dictionary<string, string> queries, string baseRoute)
		{
			_httpClient = new HttpClient();

			_host = config.GetAsStringWithDefault("connection.host", _host);
			_port = config.GetAsIntegerWithDefault("connection.port", _port);

			_baseRoute = baseRoute ?? throw new ArgumentNullException(nameof(baseRoute));
			_queries = queries ?? throw new ArgumentNullException(nameof(queries));
		}

		public async Task<T> InvokeAsyc<T>(string queryName, object variables)
		{
			if (!_queries.TryGetValue(queryName, out var query))
				throw new InvalidOperationException($"Unknown query name '{queryName}'!");

			var requestEntity = new
			{
				query,
				variables
			};

			var jsonSerializerSettings = new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};

			var requestValue = JsonConvert.SerializeObject(requestEntity, jsonSerializerSettings);
			using (var content = new StringContent(requestValue, Encoding.UTF8, "application/json"))
			{
				var response = await _httpClient.PostAsync($"http://{_host}:{_port}{_baseRoute}", content);
				var responseContent = await response.Content.ReadAsStringAsync();
				HandleErrors(response, responseContent);

				var jsonObject = JsonConvert.DeserializeObject<dynamic>(responseContent);
				var fieldValue = jsonObject.SelectToken("data." + queryName)?.ToString();

				return JsonConvert.DeserializeObject<T>(fieldValue);
			}
		}

		private void HandleErrors(HttpResponseMessage response, string responseContent)
		{
			if ((int)response.StatusCode >= 400)
			{
				ErrorDescription errorObject = null;
				try
				{
					var jsonObject = JsonConvert.DeserializeObject<dynamic>(responseContent);
					var fieldValue = jsonObject.SelectToken("errors[0].message")?.ToString();
					if (fieldValue != null)
					{
						errorObject = new ErrorDescription
						{
							Message = fieldValue,
						};
					}
					else
					{
						errorObject = PipServices3.Commons.Convert.JsonConverter.FromJson<ErrorDescription>(responseContent);
					}
				}
				finally
				{
					if (errorObject == null)
					{
						errorObject = ErrorDescriptionFactory.Create(new UnknownException(null, $"UNKNOWN_ERROR with result status: '{response.StatusCode}'", responseContent));
					}
				}

				throw ApplicationExceptionFactory.Create(errorObject);
			}
		}
	}
}
