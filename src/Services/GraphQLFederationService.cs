using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PipServices3.Commons.Config;
using PipServices3.GraphQL.Common;

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
	public abstract class GraphQLFederationService : GraphQLService
	{
		private readonly Dictionary<string, (string, bool)> _subgraphs = new();

		public GraphQLFederationService()
			: base(null)
		{ }

		public GraphQLFederationService(string extensionFile)
            : base(extensionFile)
        {
			_debugName = "GraphQLFederationService";
		}

		public override void Configure(ConfigParams config)
		{
			base.Configure(config);

			var subgraphs = config.GetSection("subgraphs");
			foreach (var subgraphName in subgraphs.GetSectionNames())
			{
				var section = subgraphs.GetSection(subgraphName);

				var uri = section.GetAsNullableString("uri");
				if (string.IsNullOrWhiteSpace(uri))
				{
					var protocol = section.GetAsStringWithDefault("protocol", "http");
					var host = section.GetAsStringWithDefault("host", "0.0.0.0");
					var port = section.GetAsIntegerWithDefault("port", 8080);
					var baseRoute = section.GetAsStringWithDefault("base_route", "/graphql");

					uri = $"{protocol}://{host}:{port}{baseRoute}";
				}

				var ignoreRootTypes = section.GetAsBooleanWithDefault("ignore_root_types", true);

				_subgraphs.Add(subgraphName, (uri, ignoreRootTypes));
			}
		}

		public override void ConfigureServices(IServiceCollection services)
		{
			base.ConfigureServices(services);
		}

		public override void ConfigureApplication(IApplicationBuilder applicationBuilder)
		{
			base.ConfigureApplication(applicationBuilder);
		}

		public override void Register(IRequestExecutorBuilder builder)
		{
			if (!_allowIntrospection) builder.AddIntrospectionAllowedRule();
			if (_maxExecutionDepth > 0) builder.AddMaxExecutionDepthRule(_maxExecutionDepth, true);

			RegisterSchema(builder);

			if (_enableQuery) RegisterQuery(builder, _queryTypeName);
			if (_enableMutation) RegisterMutation(builder, _mutationTypeName);

			RegisterSubGraphs(builder);

			if (_authorization) builder.AddAuthorization();

			RegisterInterceptor(builder);
		}

		protected new void RegisterSchema(IRequestExecutorBuilder builder)
		{
			if (!string.IsNullOrWhiteSpace(_schemaFile))
			{
				var schemaContent = ResourceHelper.LoadEmbeddedFile(_schemaFile, GetType());
				builder.AddTypeExtensionsFromString(schemaContent);

				_logger.Debug("GraphQLFederationService", "Registered type extensions file");
			}
		}

		protected void RegisterQuery(IRequestExecutorBuilder builder, string typeName = "Query")
		{
			builder.AddQueryType(d => d.Name(typeName));
		}

		protected void RegisterMutation(IRequestExecutorBuilder builder, string typeName = "Mutation")
		{
			builder.AddMutationType(d => d.Name(typeName));
		}

		protected void RegisterSubGraphs(IRequestExecutorBuilder builder)
		{
			foreach (var subgraph in _subgraphs)
			{
				builder.Services.AddHttpClient(subgraph.Key, c => c.BaseAddress = new Uri(subgraph.Value.Item1));
				builder.AddRemoteSchema(subgraph.Key, subgraph.Value.Item2);

				_logger.Debug("GraphQLFederationService", $"{subgraph.Key} -> {subgraph.Value.Item1}");
			}

			_logger.Debug("GraphQLFederationService", $"Registered {_subgraphs.Count} subgraphs");
		}

		protected override GraphQLInterceptor CreateInterceptor(GraphQLInterceptor nextAction)
		{
			return base.CreateInterceptor(nextAction);
		}
	}
}
