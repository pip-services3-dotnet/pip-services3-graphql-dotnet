using System;

using PipServices3.Commons.Config;
using PipServices3.Commons.Refer;
using PipServices3.GraphQL.Logic;
using PipServices3.GraphQL.Services;
using Xunit;

namespace PipServices3.GraphQL.Clients
{
    public sealed class DummyGraphQLClientTest : IDisposable
    {
        private static readonly ConfigParams GraphQLConfig = ConfigParams.FromTuples(
			"connection.protocol", "http",
			"connection.host", "localhost",
			"connection.port", 3000,
			"base_route", "/graphql"
		);

        private readonly DummyController _ctrl;
        private readonly DummyGraphQLClient _client;
        private readonly DummyClientFixture _fixture;

        private readonly DummySchemaFirstGraphQLService _service;

        public DummyGraphQLClientTest()
        {
            _ctrl = new DummyController();

            _service = new DummySchemaFirstGraphQLService();

            _client = new DummyGraphQLClient();

            var references = References.FromTuples(
                new Descriptor("pip-services3-dummies", "controller", "default", "default", "1.0"), _ctrl,
                new Descriptor("pip-services3-dummies", "service", "graphql", "default", "1.0"), _service,
                new Descriptor("pip-services3-dummies", "client", "graphql", "default", "1.0"), _client
            );
            _service.Configure(GraphQLConfig);
            _client.Configure(GraphQLConfig);

            _client.SetReferences(references);
            _service.SetReferences(references);

            _service.OpenAsync(null).Wait();

            _fixture = new DummyClientFixture(_client);

            _client.OpenAsync(null).Wait();
        }

        [Fact]
        public void TestCrudOperations()
        {
            var task = _fixture.TestCrudOperations();
            task.Wait();
        }

        [Fact]
        public void TestExceptionPropagation()
        {
            try
            {
                _client.RaiseExceptionAsync("123").Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            var task = _client.CloseAsync(null);
            task.Wait();

            task = _service.CloseAsync(null);
            task.Wait();
        }
    }
}
