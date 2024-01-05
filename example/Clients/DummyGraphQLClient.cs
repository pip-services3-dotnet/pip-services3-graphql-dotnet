using System.Linq;
using System.Threading.Tasks;
using PipServices3.Commons.Data;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Clients
{
    public sealed class DummyGraphQLClient : GraphQLClient, IDummyClient
    {
        public DummyGraphQLClient() : base("client.graphql")
        { }

        public Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging, ProjectionParams projection, SortParams sort)
        {
            filter ??= new FilterParams();
            paging ??= new PagingParams();

            var variables = new
            {
                correlationId,
                filter = filter.ToString(),
                paging = new
                {
                    skip = paging.Skip,
                    take = paging.Take,
                    total = paging.Total
                },
                sort = sort.Select(x => new 
                {
                    name = x.Name,
                    ascending = x.Ascending
                }).ToArray()
			};

            return ExecuteOperationAsync<DataPage<Dummy>>(correlationId, "dummies", variables, projection);
        }

        public Task<Dummy> GetOneByIdAsync(string correlationId, string dummy_id)
        {
            var variables = new
            {
                correlationId,
                id = dummy_id
            };

            return ExecuteOperationAsync<Dummy>(correlationId, "dummy", variables);
        }

        public Task<Dummy> CreateAsync(string correlationId, Dummy dummy)
        {
            var variables = new
            {
                correlationId,
                dummy
            };

            return ExecuteOperationAsync<Dummy>(correlationId, "createDummy", variables);
        }

        public Task<Dummy> UpdateAsync(string correlationId, Dummy dummy)
        {
            var variables = new
            {
                correlationId,
                dummy
            };

            return ExecuteOperationAsync<Dummy>(correlationId, "updateDummy", variables);
        }

        public Task<Dummy> DeleteByIdAsync(string correlationId, string dummy_id)
        {
            var variables = new
            {
                correlationId,
                id = dummy_id
            };

            return ExecuteOperationAsync<Dummy>(correlationId, "deleteDummy", variables);
        }

        public Task RaiseExceptionAsync(string correlationId)
        {
            return ExecuteOperationAsync<object>("raise_exception", correlationId, null);
        }
    }
}
