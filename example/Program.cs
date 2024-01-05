using System;
using System.Threading.Tasks;
using PipServices3.Commons.Config;
using PipServices3.Commons.Data;
using PipServices3.Commons.Refer;
using PipServices3.Components.Log;
using PipServices3.GraphQL.Clients;
using PipServices3.GraphQL.Data;
using PipServices3.GraphQL.Logic;
using PipServices3.GraphQL.Services;
using PipServices3.Rpc.Services;

namespace PipServices3.GraphQL
{
	class Program
    {
		private static Dummy dummy1 = new Dummy
		{
			Id = IdGenerator.NextLong(),
			Key = "key1",
			Date = DateTime.UtcNow.AddDays(-1),
			Flag = true,
			Content = "value1",
			AnyField = true,
			DummyType = DummyTypes.None
		};

		private static Dummy dummy2 = new Dummy
		{
			Id = IdGenerator.NextLong(),
			Key = "key2",
			Date = DateTime.UtcNow,
			Flag = true,
			Content = "value2",
			AnyField = 1,
			DummyType = DummyTypes.Type2
		};

		private static Dummy dummy3 = new Dummy
		{
			Id = IdGenerator.NextLong(),
			Key = "key3",
			Date = DateTime.UtcNow.AddDays(1),
			Flag = false,
			Content = "value3",
			AnyField = "some string",
			DummyType = DummyTypes.Type1
		};

		static void Main(string[] args)
		{
			DummyProcessAsync().Wait();
		}

		private static async Task DummyProcessAsync()
		{
			var httpEnpoint = new HttpEndpoint();
			var controller = new DummyController();
			var service = new DummySchemaFirstGraphQLServiceV2();

			var logger = new ConsoleLogger();

			var httpConfig = ConfigParams.FromTuples(
				"connection.protocol", "http",
				"connection.host", "localhost",
				"connection.port", 3000
			);

			httpEnpoint.Configure(httpConfig);
			
			service.Configure(ConfigParams.FromTuples(
				"base_route", "/graphql",
				"allow_introspection", "true",
				"max_execution_depth", 5,
				"enable_tool", "true",
				"debug", "true"
			));

			var references = References.FromTuples(
				new Descriptor("pip-services3", "endpoint", "http", "default", "1.0"), httpEnpoint,
				new Descriptor("pip-services3-dummies", "controller", "default", "default", "1.0"), controller,
				new Descriptor("pip-services3-dummies", "service", "graphql", "default", "1.0"), service,
				new Descriptor("pip-services3-commons", "logger", "console", "default", "1.0"), logger
			);

			service.SetReferences(references);

			await httpEnpoint.OpenAsync(null);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Task.Run(() => ClientSideAsync(httpConfig, logger));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

			Console.WriteLine("Press ENTER to exit...");
			Console.ReadLine();
		}

		private static async Task ClientSideAsync(ConfigParams config, ILogger logger)
		{
			try
			{
				var clientConfig = ConfigParams.FromTuples(
					"base_route", "/graphql"
				);

				clientConfig.Append(config);
				var client = new DummyGraphQLClient();
				client.Configure(clientConfig);

				await client.OpenAsync(null);

				Dummy dummy;

				dummy = await client.CreateAsync(IdGenerator.NextLong(), dummy1);
				dummy = await client.CreateAsync(IdGenerator.NextLong(), dummy2);
				dummy = await client.CreateAsync(IdGenerator.NextLong(), dummy3);

				var page = await client.GetPageByFilterAsync(null, FilterParams.FromTuples("key", dummy1.Key), new PagingParams(0, 1, true), new ProjectionParams
				{
					"total",
					"data.id",
					"data.key",
					"data.content",
					"data.param.name"
				}, new SortParams(new[] { new SortField("key", false) }));
			}
			catch (Exception ex)
			{
				logger.Error(null, ex);
			}
		}
	}
}
