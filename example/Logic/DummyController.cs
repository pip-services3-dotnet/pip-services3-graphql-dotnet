using System.Collections.Generic;
using System.Threading.Tasks;

using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Logic
{
	public sealed class DummyController : IDummyController
    {
        private readonly object _lock = new object();
        private readonly IList<Dummy> _entities = new List<Dummy>();

		public async Task<DataPage<Dummy>> GetDummiesAsync(string correlationId, FilterParams filter, PagingParams paging)
        {
            filter = filter != null ? filter : new FilterParams();
            var key = filter.GetAsNullableString("key");

            paging = paging != null ? paging : new PagingParams();
            var skip = paging.GetSkip(0);
            var take = paging.GetTake(100);

            var result = new List<Dummy>();

            lock (_lock)
            {
                foreach (var entity in _entities)
                {
                    if (key != null && !key.Equals(entity.Key))
                        continue;

                    skip--;
                    if (skip >= 0) continue;

                    take--;
                    if (take < 0) break;

                    result.Add(entity);
                }
            }

            return await Task.FromResult(new DataPage<Dummy>(result, paging.Total ? _entities.Count : null));
        }

		public async Task<Dummy> GetDummyAsync(string correlationId, string id)
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
