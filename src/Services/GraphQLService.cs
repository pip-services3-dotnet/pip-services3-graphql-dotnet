using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.SystemTextJson;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Utilities.Federation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PipServices3.Commons.Config;
using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Commons.Validate;
using PipServices3.Components.Count;
using PipServices3.Components.Log;
using PipServices3.GraphQL.Common;
using PipServices3.GraphQL.Types;
using PipServices3.Rpc.Services;
using Schema = GraphQL.Types.Schema;

namespace PipServices3.GraphQL.Services
{
	/// <summary>
	/// Abstract service that receives remove calls via HTTP/GraphQL protocol.
	/// 
	/// ### Configuration parameters ###
	/// 
	/// - base_route:              base route for remote URI
	/// 
	/// dependencies:
	/// - endpoint:              override for HTTP Endpoint dependency
	/// - controller:            override for Controller dependency
	/// 
	/// connection(s):
	/// - discovery_key:         (optional) a key to retrieve the connection from <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a>
	/// - protocol:              connection protocol: http or https
	/// - host:                  host name or IP address
	/// - port:                  port number
	/// - uri:                   resource URI or connection string with all parameters in it
	/// 
	/// ### References ###
	/// 
	/// - *:logger:*:*:1.0         (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_log_1_1_i_logger.html">ILogger</a> components to pass log messages
	/// - *:counters:*:*:1.0         (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_count_1_1_i_counters.html">ICounters</a> components to pass collected measurements
	/// - *:discovery:*:*:1.0        (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services to resolve connection
	/// - *:endpoint:http:*:1.0          (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-GraphQL-dotnet/class_pip_services_1_1_GraphQL_1_1_services_1_1_http_endpoint.html">HttpEndpoint</a> reference
	/// </summary>
	/// <example>
	/// <code>
	/// class MyGraphQLService: GraphQLService 
	/// {
	///     private IMyController _controller;
	///     ...
	///     public MyGraphQLService()
	///     {
	///         base();
	///         this._dependencyResolver.put(
	///         "controller", new Descriptor("mygroup", "controller", "*", "*", "1.0"));
	///     }
	///     
	///     public void SetReferences(IReferences references)
	///     {
	///         base.SetReferences(references);
	///         this._controller = this._dependencyResolver.getRequired<IMyController>("controller");
	///     }
	///     
	///     public void register()
	///     {
	///         ...
	///     }
	/// }
	/// 
	/// var service = new MyGraphQLService();
	/// service.Configure(ConfigParams.fromTuples(
	/// "connection.protocol", "http",
	/// "connection.host", "localhost",
	/// "connection.port", 8080 ));
	/// 
	/// service.SetReferences(References.fromTuples(
	/// new Descriptor("mygroup","controller","default","default","1.0"), controller ));
	/// 
	/// service.Open("123");
	/// Console.Out.WriteLine("The GraphQL service is running on port 8080");
	/// </code>
	/// </example>
	public abstract class GraphQLService : IOpenable, IConfigurable, IReferenceable, IUnreferenceable, IRegisterable, IInitializable
    {
        private static readonly ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "base_route", "",
            "dependencies.endpoint", "*:endpoint:http:*:1.0"
        );

        /// <summary>
        /// The HTTP endpoint that exposes this service.
        /// </summary>
        protected HttpEndpoint _endpoint;
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
        protected DependencyResolver _dependencyResolver = new DependencyResolver(_defaultConfig);
        /// <summary>
        /// The base route.
        /// </summary>
        protected string _baseRoute;
		/// <summary>
		/// The schema
		/// </summary>
		protected Schema _schema;

		protected ConfigParams _config;
        private IReferences _references;
        private bool _localEndpoint;
        private bool _opened;
		private IGraphQLTextSerializer _serializer;
		private DefaultFieldResolver _fieldResolver;

		protected string _queryTypeName = "Query";
		protected string _mutationTypeName = "Mutation";
		protected bool _enableTool = false;
		protected bool _debug = false;

        public GraphQLService()
        { 
        }

		public GraphQLService(string schemaFile)
        {
            _schema = CreateSchema(schemaFile);
            Intitialize();
		}
		
        public GraphQLService(Schema schema)
		{
			_schema = schema ?? throw new ArgumentNullException(nameof(schema));
            Intitialize();
		}

        private void Intitialize()
        {
			_serializer = new GraphQLSerializer();
			_fieldResolver = new DefaultFieldResolver(_schema);
		}

        public void SetSchema(Schema schema)
        {
			_schema = schema ?? throw new ArgumentNullException(nameof(schema));
			Intitialize();
		}

		/// <summary>
		/// Configures component by passing configuration parameters.
		/// </summary>
		/// <param name="config">configuration parameters to be set.</param>
		public virtual void Configure(ConfigParams config)
        {
            _config = config.SetDefaults(_defaultConfig);
            _dependencyResolver.Configure(config);

            _baseRoute = config.GetAsStringWithDefault("base_route", _baseRoute);
			_enableTool = config.GetAsBooleanWithDefault("enable_tool", _enableTool);
			_debug = config.GetAsBooleanWithDefault("debug", _debug);
		}

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies.</param>
        public virtual void SetReferences(IReferences references)
        {
            _references = references;

            _logger.SetReferences(references);
            _counters.SetReferences(references);
            _dependencyResolver.SetReferences(references);

            // Get endpoint
            _endpoint = _dependencyResolver.GetOneOptional("endpoint") as HttpEndpoint;
            _localEndpoint = _endpoint == null;

            // Or create a local one
            if (_endpoint == null) _endpoint = CreateLocalEndpoint();

            // Add registration callback to the endpoint
            _endpoint.Register(this);
			_endpoint.Initialize(this);

			if (_schema is IReferenceable schema) 
                schema.SetReferences(references);
		}

        /// <summary>
        /// Unsets (clears) previously set references to dependent components.
        /// </summary>
        public virtual void UnsetReferences()
        {
            // Remove registration callback from endpoint
            if (_endpoint != null)
            {
                _endpoint.Unregister(this);
				_endpoint.Uninitialize(this);
				_endpoint = null;
            }
        }

        private HttpEndpoint CreateLocalEndpoint()
        {
            var endpoint = new HttpEndpoint();

            if (_config != null)
                endpoint.Configure(_config);

            if (_references != null)
                endpoint.SetReferences(_references);

            return endpoint;
        }

        /// <summary>
        /// Adds instrumentation to log calls and measure call time. It returns a CounterTiming
        /// object that is used to end the time measurement.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="methodName">a method name.</param>
        /// <returns>CounterTiming object to end the time measurement.</returns>
        protected CounterTiming Instrument(string correlationId, string methodName)
        {
            _logger.Trace(correlationId, "Executing {0} method", methodName);
            _counters.IncrementOne(methodName + ".exec_count");
            return _counters.BeginTiming(methodName + ".exec_time");
        }

        /// <summary>
        /// Adds instrumentation to error handling.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="methodName">a method name.</param>
        /// <param name="ex">Error that occured during the method call</param>
        /// <param name="rethrow">True to throw the exception</param>
        protected void InstrumentError(string correlationId, string methodName, Exception ex, bool rethrow = false)
        {
            _logger.Error(correlationId, ex, "Failed to execute {0} method", methodName);
            _counters.IncrementOne(methodName + ".exec_errors");

            if (rethrow)
                throw ex;
        }

        /// <summary>
        /// Checks if the component is opened.
        /// </summary>
        /// <returns>true if the component has been opened and false otherwise.</returns>
        public bool IsOpen()
        {
            return _opened;
        }

        /// <summary>
        /// Opens the component.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <returns></returns>
        public async virtual Task OpenAsync(string correlationId)
        {
			if (IsOpen()) return;

			if (_endpoint == null)
            {
                _endpoint = CreateLocalEndpoint();
                _endpoint.Register(this);
				_endpoint.Initialize(this);
				_localEndpoint = true;
            }

            if (_localEndpoint)
            {
                await _endpoint.OpenAsync(correlationId).ContinueWith(task =>
                {
                    _opened = task.Exception == null;
                });
            }
            else
            {
                _opened = true;
            }
        }

        /// <summary>
        /// Closes component and frees used resources.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <returns></returns>
        public virtual Task CloseAsync(string correlationId)
        {
            if (IsOpen())
            {
                if (_endpoint == null)
                {
                    throw new InvalidStateException(correlationId, "NO_ENDPOINT", "GraphQL endpoint is missing");
                }

                if (_localEndpoint)
                {
                    _endpoint.CloseAsync(correlationId);
                }

                _opened = false;
            }

            return Task.Delay(0);
        }

		public virtual void ConfigureServices(IServiceCollection services)
		{
		}

		public virtual void ConfigureApplication(IApplicationBuilder applicationBuilder)
		{
            if (_enableTool)
            { 
                applicationBuilder.UseGraphQLPlayground(_baseRoute);
			}
		}

		public virtual void Register()
        {
			RegisterTypes();
			RegisterController();
			RegisterFieldMiddleware();

			RegisterRoute("post", "", async (HttpRequest request, HttpResponse response, RouteData routeData) =>
            {
                try
                {
					var graphQLRequest = GraphQLRequestHelper.GetRequest(request, _serializer);

                    if (_debug) _logger.Debug("GraphQLService", "{0}, {1}", graphQLRequest.Query, graphQLRequest.Variables);

                    IDocumentExecuter executer = new DocumentExecuter();
                    var result = await executer.ExecuteAsync(_ =>
                    {
                        _.Schema = _schema;
                        _.Query = graphQLRequest.Query;
                        _.Variables = graphQLRequest.Variables;
                        _.ThrowOnUnhandledException = true;
                        _.EnableMetrics = false;
						_.UserContext = new Dictionary<string, object>
						{
							{ "HttpRequest", request },
							{ "GraphQLRequest", graphQLRequest }
						};
                    });

                    if (result.Executed) await GraphQLResponseSender.SendResultAsync(response, _serializer, result);
                    else if (result.Errors.Count > 0) throw result.Errors.First();
                    else await GraphQLResponseSender.SendErrorAsync(response, _serializer, result);
				}
                catch (Exception ex) 
                {
					if (_debug) _logger.Error("GraphQLService", ex);
					await GraphQLResponseSender.SendErrorAsync(response, ex);
                }
			});
        }

		protected virtual Schema CreateSchema(string schemaFile)
		{
			var schemaContent = ResourceHelper.LoadEmbeddedFile(schemaFile, this.GetType());
			var schema = Schema.For(schemaContent);

            return schema;
		}

		protected virtual void RegisterTypes()
		{
			_schema.RegisterType<TimeSpanGraphType>();
			_schema.RegisterType<AnyGraphType>();
		}

		protected void RegisterEnum<TEnum>()
			where TEnum : Enum
		{
			_schema.ReplaceScalar(new EnumGraphType<TEnum>());
		}

		protected void RegisterFieldMiddleware()
		{
			_schema.FieldMiddleware.Use(next =>
			{
				return async context =>
				{
					if (context?.Source is IDictionary<string, object> expandoObject)
					{
						foreach (var kvp in expandoObject)
						{
							var fieldName = GraphQLRequestHelper.ConvertCamelToSnake(context.FieldDefinition.Name);
							if (string.Equals(kvp.Key, fieldName, StringComparison.InvariantCultureIgnoreCase))
							{
								return kvp.Value;
							}
						}

						return null;
					}

					return await next(context);
				};
			});
		}

		protected void RegisterController()
        {
			var controller = _dependencyResolver.GetOneOptional("controller");
            if (controller != null)
            {
				var methods = controller.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
				foreach (MethodInfo method in methods)
				{
					var path = FormatPath(method.Name);

					if (string.IsNullOrWhiteSpace(path)) continue;

					var parameters = method.GetParameters();
					async Task<object> func(IResolveFieldContext context)
					{
						object[] args = new object[parameters.Length];

						for (int i = 0; i < parameters.Length; i++)
						{
							args[i] = GetArgumentValue(context, parameters[i]);
						}

						var task = (Task)method.Invoke(controller, args);
						await task.ConfigureAwait(false);

						return ((dynamic)task).Result;
					}

					var registered = method.Name.StartsWith("Get")
						? TryRegisterQuery(path, func)
						: TryRegisterMutation(path, func);

					if (_debug)
					{
						 if (registered) _logger.Debug("GraphQLService", $"method {method.Name} with path {path}");
						 else _logger.Debug("GraphQLService", $"method {method.Name} skipped");
					}
				}
			}
		}

		private string FormatPath(string methodName)
		{
			if (methodName == null) return null;

			var path = methodName;
			if (path.StartsWith("Get")) path = path.Substring(3);
			if (path.EndsWith("Async")) path = path.Substring(0, path.Length - 5);

			return _schema.NameConverter.NameForField(path, _schema.Query); 
		}

		private static object GetArgumentValue(IResolveFieldContext context, ParameterInfo parameterInfo)
		{
			if (parameterInfo.ParameterType == typeof(FilterParams)) return GraphQLRequestHelper.GetFilterParams(context);
			if (parameterInfo.ParameterType == typeof(PagingParams)) return GraphQLRequestHelper.GetPagingParams(context);
			if (parameterInfo.ParameterType == typeof(SortParams)) return GraphQLRequestHelper.GetSortParams(context);
			if (parameterInfo.ParameterType == typeof(ProjectionParams)) return GraphQLRequestHelper.GetProjectionParams(context);

			return context.GetArgument(parameterInfo.ParameterType, parameterInfo.Name, parameterInfo.DefaultValue);
		}

		protected void RegisterQuery(string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			_fieldResolver.RegisterQuery(_queryTypeName, path, resolverFunc);
		}

		protected void RegisterMutation(string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			_fieldResolver.RegisterMutation(_mutationTypeName, path, resolverFunc);
		}

		protected bool TryRegisterQuery(string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			return _fieldResolver.TryRegisterQuery(_queryTypeName, path, resolverFunc);
		}

		protected bool TryRegisterMutation(string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			return _fieldResolver.TryRegisterMutation(_mutationTypeName, path, resolverFunc);
		}

		/// <summary>
		/// Registers a route in HTTP endpoint.
		/// </summary>
		/// <param name="method">HTTP method: "get", "head", "post", "put", "delete"</param>
		/// <param name="route">a command route. Base route will be added to this route</param>
		/// <param name="action">an action function that is called when operation is invoked.</param>
		private void RegisterRoute(string method, string route,
			Func<HttpRequest, HttpResponse, RouteData, Task> action)
		{
			if (_endpoint == null) return;

			route = AppendBaseRoute(route);
			_endpoint.RegisterRoute(method, route, action);
		}

		private string AppendBaseRoute(string route)
		{
			if (!string.IsNullOrEmpty(_baseRoute))
			{
				var baseRoute = _baseRoute;
				if (string.IsNullOrEmpty(route))
					route = "/";
				if (route[0] != '/')
					route = "/" + route;
				if (baseRoute[0] != '/') baseRoute = '/' + baseRoute;
				route = baseRoute + route;
			}

			return route.TrimEnd('/');
		}
	}
}
