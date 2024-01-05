using System;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Http;

using PipServices3.Commons.Config;
using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Components.Count;
using PipServices3.Components.Log;

namespace PipServices3.GraphQL.Services
{
    public class GraphQLOperations : IConfigurable, IReferenceable
    {
        /// <summary>
        /// The logger.
        /// </summary>
        protected CompositeLogger _logger = new CompositeLogger();

        /// <summary>
        /// The performance counters.
        /// </summary>
        protected CompositeCounters _counters = new CompositeCounters();

        /// <summary>
        /// The dependency resolver.
        /// </summary>
        protected DependencyResolver _dependencyResolver = new DependencyResolver();

        public virtual void Configure(ConfigParams config)
        {
            _dependencyResolver.Configure(config);
        }

        public virtual void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _counters.SetReferences(references);
            _dependencyResolver.SetReferences(references);
        }

        protected string GetCorrelationId(IResolveFieldContext context)
        {
            return GraphQLRequestHelper.GetCorrelationId(context);
        }

        protected FilterParams GetFilterParams(IResolveFieldContext context)
        {
            return GraphQLRequestHelper.GetFilterParams(context);
        }

        protected PagingParams GetPagingParams(IResolveFieldContext context)
        {
            return GraphQLRequestHelper.GetPagingParams(context);
        }

        protected SortParams GetSortParams(IResolveFieldContext context)
        {
            return GraphQLRequestHelper.GetSortParams(context);
        }

		protected ProjectionParams GetProjectionParams(IResolveFieldContext context)
		{
			return GraphQLRequestHelper.GetProjectionParams(context);
		}

		protected virtual void HandleError(string correlationId, string methodName, Exception ex)
        {
            _logger.Error(correlationId, ex, $"Failed to execute {methodName}");
        }

        protected virtual CounterTiming Instrument(string correlationId, string methodName, string message = "")
        {
            _logger.Trace(correlationId, $"Executed {methodName} {message}");
            return _counters.BeginTiming(methodName + ".exec_time");
        }
    }
}