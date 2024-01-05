using PipServices3.Commons.Data;
using System.Threading.Tasks;

namespace PipServices3.GraphQL.Data
{
	public interface IDummyClient
    {
        Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging, ProjectionParams projection, SortParams sort);
        Task<Dummy> GetOneByIdAsync(string correlationId, string id);
        Task<Dummy> CreateAsync(string correlationId, Dummy entity);
        Task<Dummy> UpdateAsync(string correlationId, Dummy entity);
        Task<Dummy> DeleteByIdAsync(string correlationId, string id);
        Task RaiseExceptionAsync(string correlationId);
    }
}
