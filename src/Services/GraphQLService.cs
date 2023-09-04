using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PipServices3.Commons.Config;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Components.Count;
using PipServices3.Components.Log;
using PipServices3.GraphQL.Common;
using PipServices3.Rpc.Services;

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
	public abstract class GraphQLService : IOpenable, IConfigurable, IReferenceable, IUnreferenceable, IInitializable
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
        protected string _baseRoute = "/graphql";
		/// <summary>
		/// The schema
		/// </summary>
		protected string _schemaFile;

		protected ConfigParams _config;
        private IReferences _references;
        private bool _localEndpoint;
        private bool _opened;
		private bool _isSchemaFirst;

		protected bool _allowIntrospection = false;
		protected int _maxExecutionDepth = 0;
		protected bool _enableTool = false;
		protected bool _authorization = false;
		protected string _queryTypeName = "Query";
		protected string _mutationTypeName = "Mutation";
		protected bool _enableQuery = true;
		protected bool _enableMutation = true;
		protected bool _debug = false;
		protected string _debugName = "GraphQLService";

		public GraphQLService(string schemaFile = null)
        {
            _schemaFile = schemaFile;
			_isSchemaFirst = !string.IsNullOrWhiteSpace(schemaFile);
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
			_allowIntrospection = config.GetAsBooleanWithDefault("allow_introspection", _allowIntrospection);
			_maxExecutionDepth = config.GetAsIntegerWithDefault("max_execution_depth", _maxExecutionDepth);
			_enableTool = config.GetAsBooleanWithDefault("enable_tool", _enableTool);
			_authorization = config.GetAsBooleanWithDefault("authorization", _authorization);
			_queryTypeName = config.GetAsStringWithDefault("query_type_name", _queryTypeName);
			_mutationTypeName = config.GetAsStringWithDefault("mutation_type_name", _mutationTypeName);
			_enableQuery = config.GetAsBooleanWithDefault("enable_query", _enableQuery);
			_enableMutation = config.GetAsBooleanWithDefault("enable_mutation", _enableMutation);
			_debug = config.GetAsBooleanWithDefault("debug", _debug);
			_debugName = config.GetAsStringWithDefault("debug_name", _debugName);
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
			_endpoint ??= CreateLocalEndpoint();

			// Add registration callback to the endpoint
			_endpoint.Initialize(this);
			if (_debug) Console.WriteLine($"{_debugName}. Endpoint initialized");
		}

		/// <summary>
		/// Unsets (clears) previously set references to dependent components.
		/// </summary>
		public virtual void UnsetReferences()
        {
            // Remove registration callback from endpoint
            if (_endpoint != null)
            {
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

			if (_debug) Console.WriteLine($"{_debugName}. Created local endpoint");

			return endpoint;
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
				_endpoint.Initialize(this);
				_localEndpoint = true;
			}

			if (_localEndpoint)
			{
				await _endpoint.OpenAsync(correlationId).ContinueWith(task =>
				{
					_opened = task.Exception == null;

					if (_debug) Console.WriteLine($"{_debugName}. Opened");
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
                    throw new InvalidStateException(correlationId, "NO_ENDPOINT", "HTTP endpoint is missing");
                }

                if (_localEndpoint)
                {
                    _endpoint.CloseAsync(correlationId);
                }

				if (_debug) Console.WriteLine($"{_debugName}. Closed");

				_opened = false;
            }

            return Task.Delay(0);
        }

		public virtual void ConfigureServices(IServiceCollection services)
		{
			if (_authorization) services.AddAuthorization();

			var requestExecutorBuilder = services
				.AddRouting()
				.AddGraphQLServer();

			Register(requestExecutorBuilder);

			if (_debug) Console.WriteLine($"{_debugName}. Configured services");
		}

		public virtual void ConfigureApplication(IApplicationBuilder applicationBuilder)
		{
			applicationBuilder.UseExceptionHandler(exceptionHandlerApp =>
			{
				exceptionHandlerApp.Run(async context =>
				{
					var exceptionHandlerPathFeature =
						context.Features.Get<IExceptionHandlerPathFeature>();

					if (exceptionHandlerPathFeature != null)
					{
						await GraphQLResponseHelper.SendErrorAsync(context.Response, exceptionHandlerPathFeature?.Error);
					}
				});
			});

			applicationBuilder.UseRouting();

			applicationBuilder.UseEndpoints(endpoints =>
			{
				endpoints.MapGraphQL(_baseRoute).WithOptions(new GraphQLServerOptions
				{
					Tool = 
					{
						Enable = _enableTool
					}
				});
			});

			if (_authorization) applicationBuilder.UseAuthorization();

			if (_debug) Console.WriteLine($"{_debugName}. Configured application");
		}

		public virtual void Register(IRequestExecutorBuilder builder)
		{
			RegisterSchema(builder);

			var controller = RegisterController(builder);
			if (controller != null)
			{
				var controllerType = controller.GetType();
				if (_enableQuery) RegisterQuery(builder, controllerType, _queryTypeName);
				if (_enableMutation) RegisterMutation(builder, controllerType, _mutationTypeName);
			}

			if (!_allowIntrospection) builder.AddIntrospectionAllowedRule();
			if (_maxExecutionDepth > 0) builder.AddMaxExecutionDepthRule(_maxExecutionDepth, true);
			if (_authorization) builder.AddAuthorization();
			
			RegisterInterceptor(builder);
		}

		protected void RegisterSchema(IRequestExecutorBuilder builder)
		{
			if (!string.IsNullOrWhiteSpace(_schemaFile))
			{
				var schemaContent = ResourceHelper.LoadEmbeddedFile(_schemaFile, GetType());
				builder.AddDocumentFromString(schemaContent);

				if (_debug) Console.WriteLine($"{_debugName}. Schema file registered (fileName = {_schemaFile}, content.Length = {schemaContent.Length})");
				return;
			}

			if (_debug) Console.WriteLine($"{_debugName}. Schema file not registered");
		}

		protected object RegisterController(IRequestExecutorBuilder builder)
		{
			var controller = _dependencyResolver.GetOneOptional("controller");
			if (controller != null)
			{
				builder.Services.AddSingleton(controller.GetType(), (provider) => controller);
			}

			if (_debug) Console.WriteLine($"{_debugName}. Controller registered ({controller.GetType().FullName})");

			return controller;
		}

		protected void RegisterQuery(IRequestExecutorBuilder builder, Type runtimeType, string typeName = "Query")
		{
			if (_isSchemaFirst) 
				builder.BindRuntimeType(runtimeType, typeName);
			else
				builder.AddQueryType(runtimeType);

			if (_debug) Console.WriteLine($"{_debugName}. Query registered (runtimeType = {runtimeType.FullName}, typeName = {typeName})");
		}

		protected void RegisterMutation(IRequestExecutorBuilder builder, Type runtimeType, string typeName = "Mutation")
		{
			if (_isSchemaFirst) 
				builder.BindRuntimeType(runtimeType, typeName);
			else
				builder.AddMutationType(runtimeType);

			if (_debug) Console.WriteLine($"{_debugName}. Mutation registered (runtimeType = {runtimeType.FullName}, typeName = {typeName})");
		}

		protected void RegisterInterceptor(IRequestExecutorBuilder builder)
		{
			var interceptor = new HttpRequestInterceptor();
			var nextAction = CreateInterceptor(interceptor.Action);

			if (nextAction != null)
			{
				interceptor.Action = nextAction;	
				builder.AddHttpRequestInterceptor(provider => interceptor);

				if (_debug) Console.WriteLine($"{_debugName}. Interceptor registered");
			}
		}

		protected virtual GraphQLInterceptor CreateInterceptor(GraphQLInterceptor nextAction)
		{
			return null;
		}
	}
}
