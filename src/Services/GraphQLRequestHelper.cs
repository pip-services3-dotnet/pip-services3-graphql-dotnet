using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using PipServices3.Commons.Data;
using System.IO;

namespace PipServices3.GraphQL.Services
{
	/// <summary>
	/// Helper class that handles GraphQL-based requests.
	/// </summary>
	public static class GraphQLRequestHelper
	{
		public static string GetCorrelationId(IResolverContext context)
		{
			string correlationId;
			var parent = context;

			do
			{
				correlationId = parent.ArgumentValue<string>("correlationId");
				parent = parent.Parent<IResolverContext>();
			}
			while (parent != null && correlationId == null);

			return correlationId;
		}

		public static FilterParams GetFilterParams(IResolverContext context)
		{
			var filter = new FilterParams();
			var parser = FilterParams.FromString(context.ArgumentValue<string>("filter"));

			foreach (var filterParam in parser)
			{
				filter.Set(filterParam.Key, filterParam.Value);
			}

			return filter;
		}

		public static PagingParams GetPagingParams(IResolverContext context)
		{
			return context.ArgumentValue<PagingParams>("paging");
		}

		public static SortParams GetSortParams(IResolverContext context)
		{
			return context.ArgumentValue<SortParams>("sort");
		}
	}
}
