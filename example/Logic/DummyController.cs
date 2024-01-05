using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.GraphQL.Data;
using PipServices3.GraphQL.Services;

namespace PipServices3.GraphQL.Logic
{
	public sealed class DummyController : IDummyController
    {
        private readonly object _lock = new object();
        private readonly IList<Dummy> _entities = new List<Dummy>();

        public async Task<DataPage<ExpandoObject>> GetDummiesExpandoAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort, ProjectionParams projection)
        {
            var page = await GetDummiesAsync(correlationId, filter, paging, sort, projection);
            return new DataPage<ExpandoObject>(page.Data.Select(x => ConvertToExpandoObject(x)).ToList(), page.Total);
        }

		static ExpandoObject ConvertToExpandoObject(object obj)
		{
			var expandoObject = new ExpandoObject();
			var expandoDict = (IDictionary<string, object>)expandoObject;

			foreach (var property in obj.GetType().GetProperties())
			{
                var name = GraphQLRequestHelper.ConvertCamelToSnake(property.Name);
                var value = property.GetValue(obj);
                
                expandoDict[name] = value;
			}

			return expandoObject;
		}

		public async Task<DataPage<Dummy>> GetDummiesAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort, ProjectionParams projection)
        {
            filter ??= new FilterParams();
            sort ??= new SortParams();
			paging ??= new PagingParams();
            projection ??= new ProjectionParams();  

			var key = filter.GetAsNullableString("key");

            var skip = paging.GetSkip(0);
            var take = paging.GetTake(100);

            var result = new List<Dummy>();
            IEnumerable<Dummy> entities = _entities;

            if (key != null)
            {
                entities = entities.Where(x => key.Equals(x.Key));
			}
            
            if (sort.Count > 0)
            { 
                var keySort = sort.FirstOrDefault(x => string.Equals(x.Name, "key", StringComparison.InvariantCultureIgnoreCase));
                if (keySort != null)
                {
                    if (keySort.Ascending) entities = entities.OrderBy(x => x.Key);
                    else entities = entities.OrderByDescending(x => x.Key);
				}
            }

			lock (_lock)
            {
                foreach (var entity in entities)
                {
                    skip--;
                    if (skip >= 0) continue;

                    take--;
                    if (take < 0) break;

                    result.Add(entity);
                }
            }

            return await Task.FromResult(new DataPage<Dummy>(result, paging.Total ? _entities.Count : null));
        }

		public async Task<Dummy> GetDummyAsync(string correlationId, string id, ProjectionParams projection)
        {
            await Task.Delay(0);

            lock (_lock)
            {
                foreach (var entity in _entities)
                {
                    if (entity.Id.Equals(id))
                        return entity;
                }
            }

            return null;
        }

        public async Task<Dummy> CreateDummyAsync(string correlationId, Dummy dummy)
        {
            await Task.Delay(0);

            lock (_lock)
            {
                if (dummy.Id == null)
                    dummy.Id = IdGenerator.NextLong();

                _entities.Add(dummy);
            }
            return dummy;
        }

        public async Task<Dummy> UpdateDummyAsync(string correlationId, Dummy dummy)
        {
            await Task.Delay(0);

            lock (_lock)
            {
                for (int index = 0; index < _entities.Count; index++)
                {
                    var entity = _entities[index];
                    if (entity.Id.Equals(dummy.Id))
                    {
                        _entities[index] = dummy;
                        return dummy;
                    }
                }
            }
            return null;
        }

        public async Task<Dummy> DeleteDummyAsync(string correlationId, string id)
        {
            await Task.Delay(0);

            lock (_lock)
            {
                for (int index = 0; index < _entities.Count; index++)
                {
                    var entity = _entities[index];
                    if (entity.Id.Equals(id))
                    {
                        _entities.RemoveAt(index);
                        return entity;
                    }
                }
            }
            return null;
        }

        public Task<bool?> RaiseExceptionAsync(string correlationId)
        {
            throw new NotFoundException(correlationId, "TEST_ERROR", "Dummy error in controller!");
        }

        public async Task<bool> PingAsync()
        {
            return await Task.FromResult(true);
        }
    }
}
