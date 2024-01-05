using PipServices3.Commons.Data;
using System.Threading.Tasks;

namespace PipServices3.GraphQL.Data
{
    public interface IDummyController
    {
        Task<DataPage<Dummy>> GetDummiesAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort, ProjectionParams projection);
        Task<Dummy> GetDummyAsync(string correlationId, string id, ProjectionParams projection);
        Task<Dummy> CreateDummyAsync(string correlationId, Dummy dummy);
        Task<Dummy> UpdateDummyAsync(string correlationId, Dummy dummy);
        Task<Dummy> DeleteDummyAsync(string correlationId, string id);
        Task<bool?> RaiseExceptionAsync(string correlationId);

        Task<bool> PingAsync();
    }
}
