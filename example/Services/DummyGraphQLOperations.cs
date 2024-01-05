using System.Threading.Tasks;
using GraphQL;
using PipServices3.Commons.Refer;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services
{
	public class DummyGraphQLOperations: GraphQLOperations
    {
        private IDummyController _controller;

        public DummyGraphQLOperations()
        {
            _dependencyResolver.Put("controller",
                new Descriptor("pip-services3-dummies", "controller", "default", "*", "*"));
        }
        
        public new void SetReferences(IReferences references)
        {
            base.SetReferences(references);

            _controller = _dependencyResolver.GetOneRequired<IDummyController>("controller");
        }

        public async Task<object> GetDummiesAsync(IResolveFieldContext context)
        {
            var correlationId = GetCorrelationId(context);
            var filter = GetFilterParams(context);
            var paging = GetPagingParams(context);
            var sort = GetSortParams(context);
			var projection = GetProjectionParams(context);

			var result = await _controller.GetDummiesAsync(correlationId, filter, paging, sort, projection);

            return result;
        }
        
        public async Task<object> CreateDummyAsync(IResolveFieldContext context)
        {
            var correlationId = GetCorrelationId(context);
            var dummy = context.GetArgument<Dummy>("dummy");

            var result = await _controller.CreateDummyAsync(correlationId, dummy);

			return result;
        }
        
        public async Task<object> UpdateDummyAsync(IResolveFieldContext context)
        {
            var correlationId = GetCorrelationId(context);
			var dummy = context.GetArgument<Dummy>("dummy");

			var result = await _controller.UpdateDummyAsync(correlationId, dummy);

			return result;
        }
        
        public async Task<object> GetDummyAsync(IResolveFieldContext context)
        {
            var correlationId = GetCorrelationId(context);
			var projection = GetProjectionParams(context);

			var id = context.GetArgument<string>("dummyId") ?? context.GetArgument<string>("id");
            
            var result = await _controller.GetDummyAsync(correlationId, id, projection);

			return result;
        }
        
        public async Task<object> DeleteDummyAsync(IResolveFieldContext context)
        {
            var correlationId = GetCorrelationId(context);
			var id = context.GetArgument<string>("dummyId") ?? context.GetArgument<string>("id");

			var result = await _controller.DeleteDummyAsync(correlationId, id);

			return result;
        }
    }
}