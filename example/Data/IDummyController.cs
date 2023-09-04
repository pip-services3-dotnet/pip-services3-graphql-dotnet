using PipServices3.Commons.Data;
using System.Threading.Tasks;

namespace PipServices3.GraphQL.Data
{
    public interface IDummyController
    {
        Task<DataPage<Dummy>> GetDummiesAsync(string correlationId, FilterParams filter, PagingParams paging);
        Task<Dummy> GetDummyAsync(string correlationId, string id);
        Task<Dummy> CreateDummyAsync(string correlationId, Dummy entity);
        Task<Dummy> UpdateDummyAsync(string correlationId, Dummy entity);
        Task<Dummy> DeleteDummyAsync(string correlationId, string id);
        Task<bool?> RaiseExceptionAsync(string correlationId);

        Task<bool> PingAsync();
    }
}
