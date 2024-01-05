using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using PipServices3.Commons.Config;
using PipServices3.Commons.Convert;
using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.GraphQL.Clients;
using PipServices3.GraphQL.Data;
using PipServices3.GraphQL.Logic;
using PipServices3.Rpc.Services;
using Xunit;
using System.Collections.Generic;
using static System.Net.WebRequestMethods;

namespace PipServices3.GraphQL.Services
{
    [Collection("Sequential")]
    public class DummyHttpEndpointTest : IDisposable
    {
        private const int HTTP_PORT = 3003;

        private static readonly ConfigParams GraphQLConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            "connection.host", "localhost",
            "connection.port", HTTP_PORT
        );

        private static readonly Dictionary<string, string> Queries = new()
        {
            { "createDummy", "mutation createDummy($correlationId: String, $dummy: DummyInput!) {" +
                "createDummy(correlationId: $correlationId, dummy: $dummy) { id key content	} }" },

            { "dummies", "query dummies($correlationId: String, $filter: String, $paging: PagingParams) { " +
                "dummies(correlationId: $correlationId, filter: $filter, paging: $paging) { data { id key content param { name } } total } }" },

            { "updateDummy", "mutation updateDummy($correlationId: String, $dummy: DummyInput!) { " +
                "updateDummy(correlationId: $correlationId, dummy: $dummy) { id key content } }" },

            { "deleteDummy", "mutation deleteDummy($correlationId: String, $id: String!) { " +
                "deleteDummy(correlationId: $correlationId, id: $id) { id key content } }" },

            { "dummy", "query dummy($correlationId: String, $id: String!) { " +
                "dummy(correlationId: $correlationId, id: $id) { id key content param { name } } }" },
        };

        private DummySchemaFirstGraphQLServiceV1 _service1;
        private DummySchemaFirstGraphQLServiceV1 _service2;

        private DummyHttpClient _client1;
        private DummyHttpClient _client2;

        private HttpEndpoint _httpEndpoint;

        public DummyHttpEndpointTest()
        {
            _client1 = new DummyHttpClient(GraphQLConfig, Queries, "/graphql1");
            _client2 = new DummyHttpClient(GraphQLConfig, Queries, "/graphql2");

            _httpEndpoint = new HttpEndpoint();

            _service1 = new DummySchemaFirstGraphQLServiceV1();
            _service2 = new DummySchemaFirstGraphQLServiceV1();

            var references = References.FromTuples(
                new Descriptor("pip-services3-dummies", "controller", "default", "default", "1.0"), new DummyController(),
                new Descriptor("pip-services3", "endpoint", "http", "default", "1.0"), _httpEndpoint
            );

            _service1.Configure(ConfigParams.FromTuples(
                "base_route", "/graphql1"
            ));

            _service2.Configure(ConfigParams.FromTuples(
                "base_route", "/graphql2"
            ));

            _service1.SetReferences(references);
            _service2.SetReferences(references);

            _httpEndpoint.Configure(GraphQLConfig);
            _httpEndpoint.OpenAsync(null).Wait();
        }

        public void Dispose()
        {
            _service1.CloseAsync(null).Wait();
            _service2.CloseAsync(null).Wait();
            _httpEndpoint.CloseAsync(null).Wait();
        }

        [Fact]
        public async Task It_Should_Perform_CRUD_OperationsAsync()
        {
            It_Should_Be_Opened();

            await It_Should_Create_DummyAsync(_client1);
            await It_Should_Create_Dummy2Async(_client1);
            await It_Should_Update_Dummy2Async(_client1);
            await It_Should_Get_DummyAsync(_client1);
            await It_Should_Get_DummiesAsync(_client1);
            await It_Should_Delete_DummyAsync(_client1);
        }

        private async Task It_Should_Delete_DummyAsync(DummyHttpClient client)
        {
            var existingDummy = new Dummy("1", "Key 1", "Content 1");

            var resultDummy = await client.InvokeAsyc<Dummy>("deleteDummy", new
            {
                id = existingDummy.Id
            });

            Assert.NotNull(resultDummy);
            Assert.NotNull(resultDummy.Id);
            Assert.Equal(existingDummy.Key, resultDummy.Key);
            Assert.Equal(existingDummy.Content, resultDummy.Content);

            resultDummy = await client.InvokeAsyc<Dummy>("dummy", new
            {
                id = existingDummy.Id
            });

            Assert.Null(resultDummy);
        }

        private void It_Should_Be_Opened()
        {
            Assert.True(_httpEndpoint.IsOpen());
        }

        private async Task It_Should_Create_DummyAsync(DummyHttpClient client)
        {
            var newDummy = new Dummy("1", "Key 1", "Content 1");

            var resultDummy = await client.InvokeAsyc<Dummy>("createDummy", new
            {
                dummy = newDummy
            });

            Assert.NotNull(resultDummy);
            Assert.NotNull(resultDummy.Id);
            Assert.Equal(newDummy.Key, resultDummy.Key);
            Assert.Equal(newDummy.Content, resultDummy.Content);
        }

        private async Task It_Should_Create_Dummy2Async(DummyHttpClient client)
        {
            var newDummy = new Dummy("2", "Key 2", "Content 2");

            var resultDummy = await client.InvokeAsyc<Dummy>("createDummy", new
            {
                dummy = newDummy
            });

            Assert.NotNull(resultDummy);
            Assert.NotNull(resultDummy.Id);
            Assert.Equal(newDummy.Key, resultDummy.Key);
            Assert.Equal(newDummy.Content, resultDummy.Content);
        }

        private async Task It_Should_Update_Dummy2Async(DummyHttpClient client)
        {
            var newDummy = new Dummy("2", "Key 2", "Content 3");

            var resultDummy = await client.InvokeAsyc<Dummy>("updateDummy", new
            {
                dummy = newDummy
            });

            Assert.NotNull(resultDummy);
            Assert.NotNull(resultDummy.Id);
            Assert.Equal(newDummy.Key, resultDummy.Key);
            Assert.Equal(newDummy.Content, resultDummy.Content);
        }

        private async Task It_Should_Get_DummyAsync(DummyHttpClient client)
        {
            var existingDummy = new Dummy("1", "Key 1", "Content 1");

            var resultDummy = await client.InvokeAsyc<Dummy>("dummy", new
            {
                id = existingDummy.Id
            });

            Assert.NotNull(resultDummy);
            Assert.NotNull(resultDummy.Id);
            Assert.Equal(existingDummy.Key, resultDummy.Key);
            Assert.Equal(existingDummy.Content, resultDummy.Content);
        }

        private async Task It_Should_Get_DummiesAsync(DummyHttpClient client)
        {
            var existingDummy = new Dummy("1", "Key 1", "Content 1");

            var resultDummies = await client.InvokeAsyc<DataPage<Dummy>>("dummies", new
            {
                filter = FilterParams.FromTuples("key", existingDummy.Key).ToString()
            });

            Assert.NotNull(resultDummies);
            Assert.NotNull(resultDummies.Data);
            Assert.Single(resultDummies.Data);

            resultDummies = await client.InvokeAsyc<DataPage<Dummy>>("dummies", new
            {
                filter = ""
            });

            Assert.NotNull(resultDummies);
            Assert.NotNull(resultDummies.Data);
            Assert.Equal(2, resultDummies.Data.Count());
        }
    }
}