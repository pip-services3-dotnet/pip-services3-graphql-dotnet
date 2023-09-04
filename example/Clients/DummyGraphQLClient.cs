using System.Threading.Tasks;
using PipServices3.Commons.Data;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Clients
{
    public sealed class DummyGraphQLClient : GraphQLClient, IDummyClient
    {
        public DummyGraphQLClient() : base("client.graphql")
        { }

        public Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging)
        {
            filter = filter ?? new FilterParams();
            paging = paging ?? new PagingParams();

            var variables = new
            {
                correlationId,
				filter = filter.ToString(),
                paging
            };

            return SendQueryAsync<DataPage<Dummy>>(correlationId, "dummies", variables);
        }

        public Task<Dummy> GetOneByIdAsync(string correlationId, string dummy_id)
        {
            var variables = new
            {
                correlationId,
                id = dummy_id
            };

            return SendQueryAsync<Dummy>(correlationId, "dummy", variables);
        }

        public Task<Dummy> CreateAsync(string correlationId, Dummy dummy)
        {
            var variables = new
            {
                correlationId,
                dummy
            };

            return SendQueryAsync<Dummy>(correlationId, "createDummy", variables);
        }

        public Task<Dummy> UpdateAsync(string correlationId, Dummy dummy)
        {
            var variables = new
            {
                correlationId,
                dummy
            };

            return SendQueryAsync<Dummy>(correlationId, "updateDummy", variables);
        }

        public Task<Dummy> DeleteByIdAsync(string correlationId, string dummy_id)
        {
            var variables = new
            {
                correlationId,
                id = dummy_id
            };

            return SendQueryAsync<Dummy>(correlationId, "deleteDummy", variables);
        }

        public Task RaiseExceptionAsync(string correlationId)
        {
            return SendQueryAsync<object>("raise_exception", correlationId, null);
        }
    }
}
