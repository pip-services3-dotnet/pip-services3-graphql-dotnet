using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Validation;
using Newtonsoft.Json;
using PipServices3.Commons.Config;
using PipServices3.Commons.Convert;
using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Components.Count;
using PipServices3.Components.Log;
using PipServices3.GraphQL.Common;
using PipServices3.GraphQL.Services;
using PipServices3.Rpc.Connect;

namespace PipServices3.GraphQL.Clients
{
    /// <summary>
    /// Abstract client that calls remove endpoints using HTTP/GraphQL protocol.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// - base_route:              base route for remote URI
    /// 
    /// connection(s):
    /// - discovery_key:         (optional) a key to retrieve the connection from <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a>
    /// - protocol:              connection protocol: http or https
    /// - host:                  host name or IP address
    /// - port:                  port number
    /// - uri:                   resource URI or connection string with all parameters in it 
    /// 
    /// options:
    /// - retries:               number of retries(default: 3)
    /// - connect_timeout:       connection timeout in milliseconds(default: 10 sec)
    /// - timeout:               invocation timeout in milliseconds(default: 10 sec)
    /// 
    /// ### References ###
    /// 
    /// - *:logger:*:*:1.0         (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_log_1_1_i_logger.html">ILogger</a> components to pass log messages
    /// - *:counters:*:*:1.0         (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_count_1_1_i_counters.html">ICounters</a> components to pass collected measurements
    /// - *:discovery:*:*:1.0        (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services to resolve connection
    /// </summary>
    /// <example>
    /// <code>
    /// class MyGraphQLClient: GraphQLClient, IMyClient 
    /// {
    ///     ...
    /// 
    ///     public MyData GetData(string correlationId, string id)
    ///     {
    ///         var timing = this.Instrument(correlationId, 'myclient.get_data');
    ///         try
    ///         {
    ///           var result = this.ExecuteAsync<MyData>(correlationId, HttpMethod.Post, "/get_data", new MyData(id));
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///           this.InstrumentError(correlationId, "myclient.get_data", ex, true);
    ///         }
    ///         finally
    ///         {
    ///           timing.EndTiming();
    ///         }
    ///         return result;        
    ///     }
    ///     ...
    /// }
    /// 
    /// var client = new MyGraphQLClient();
    /// client.Configure(ConfigParams.fromTuples(
    /// "connection.protocol", "http",
    /// "connection.host", "localhost",
    /// "connection.port", 8080 ));
    /// 
    /// var data = client.GetData("123", "1");
    /// ...
    /// </code>
    /// </example>
    public class GraphQLClient : IOpenable, IConfigurable, IReferenceable
    {
        private static readonly ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "connection.protocol", "http",
            //"connection.host", "localhost",
            //"connection.port", 3000,

            "options.request_max_size", 1024*1024,
            "options.connect_timeout", 60000,
            "options.retries", 1,
            "options.debug", true
        );

        /// <summary>
        /// The connection resolver.
        /// </summary>
        protected HttpConnectionResolver _connectionResolver = new HttpConnectionResolver();
        /// <summary>
        /// The logger.
        /// </summary>
        protected CompositeLogger _logger = new CompositeLogger();
        /// <summary>
        /// The performance counters.
        /// </summary>
        protected CompositeCounters _counters = new CompositeCounters();
        /// <summary>
        /// The configuration options.
        /// </summary>
        protected ConfigParams _options = new ConfigParams();
        /// <summary>
        /// The base route.
        /// </summary>
        protected string _baseRoute;
        /// <summary>
        /// The number of retries.
        /// </summary>
        protected int _retries = 1;
        /// <summary>
        /// The invocation timeout (ms).
        /// </summary>
        protected int _timeout = 100000;

        /// <summary>
        /// The HTTP client.
        /// </summary>
        protected HttpClient _client;
        /// <summary>
        /// The remote service uri which is calculated on open.
        /// </summary>
        protected string _address;

        /// <summary>
        /// The default headers to be added to every request.
        /// </summary>
        protected StringValueMap _headers = new StringValueMap();

		private Dictionary<string, string> _operations = new Dictionary<string, string>();

		public GraphQLClient(string operationsFile)
        {
			_operations = ParseOperationsFile(operationsFile);
		}

		public GraphQLClient(Dictionary<string, string> operations)
		{
			_operations = operations;
		}

		/// <summary>
		/// Configures component by passing configuration parameters.
		/// </summary>
		/// <param name="config">configuration parameters to be set.</param>
		public virtual void Configure(ConfigParams config)
        {
            config = config.SetDefaults(_defaultConfig);
            _connectionResolver.Configure(config);
            _options = _options.Override(config.GetSection("options"));

            _retries = config.GetAsIntegerWithDefault("options.retries", _retries);
            _timeout = config.GetAsIntegerWithDefault("options.timeout", _timeout); ;

            _baseRoute = config.GetAsStringWithDefault("base_route", _baseRoute);
        }

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies.</param>
        public virtual void SetReferences(IReferences references)
        {
            _connectionResolver.SetReferences(references);
            _logger.SetReferences(references);
            _counters.SetReferences(references);
        }

        /// <summary>
        /// Adds instrumentation to log calls and measure call time. It returns a CounterTiming
        /// object that is used to end the time measurement.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="methodName">a method name.</param>
        /// <returns>CounterTiming object to end the time measurement.</returns>
        protected CounterTiming Instrument(string correlationId, [CallerMemberName]string methodName = null)
        {
            var typeName = GetType().Name;
            _logger.Trace(correlationId, "Calling {0} method of {1}", methodName, typeName);
            _counters.IncrementOne(typeName + "." + methodName + ".call_count");
            return _counters.BeginTiming(typeName + "." + methodName + ".call_time");
        }

        /// <summary>
        /// Adds instrumentation to error handling.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="methodName">a method name.</param>
        /// <param name="ex">Error that occured during the method call</param>
        /// <param name="rethrow">True to throw the exception</param>
        protected void InstrumentError(string correlationId, [CallerMemberName]string methodName = null, Exception ex = null, bool rethrow = false)
        {
            var typeName = GetType().Name;
            _logger.Error(correlationId, ex, "Failed to call {0} method of {1}", methodName, typeName);
            _counters.IncrementOne(typeName + "." + methodName + ".call_errors");

            if (rethrow)
                throw ex;
        }

        /// <summary>
        /// Checks if the component is opened.
        /// </summary>
        /// <returns>true if the component has been opened and false otherwise.</returns>
        public virtual bool IsOpen()
        {
            return _client != null;
        }

        /// <summary>
        /// Opens the component.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public async virtual Task OpenAsync(string correlationId)
        {
			var connection = await _connectionResolver.ResolveAsync(correlationId);

            var protocol = connection.Protocol;
            var host = connection.Host;
            var port = connection.Port;

			_address = protocol + "://" + host + ":" + port;

            _client?.Dispose();

            _client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = true,
                UseCookies = true
            });

            _client.Timeout = TimeSpan.FromMilliseconds(_timeout);
            _client.DefaultRequestHeaders.ConnectionClose = true;

			_logger.Debug(correlationId, "Connected via GraphQL to {0}", _address);
        }

        private Dictionary<string, string> ParseOperationsFile(string operationsFile)
        {
            var result = new Dictionary<string, string>();
            var content = ResourceHelper.LoadEmbeddedFile(operationsFile, GetType());

			string pattern = @"(query|mutation)\s+(\w+)\s*\([^)]*\)\s*\{(?:[^{}]|(?<open>{)|(?<close-open>}))+(?(open)(?!))\}";

			MatchCollection matches = Regex.Matches(content, pattern);

			foreach (Match match in matches)
			{
				string name = match.Groups[2].Value;
				string query = match.Value;

				result.Add(name, query);
			}

			return result;
		}

		/// <summary>
		/// Closes component and frees used resources.
		/// </summary>
		/// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
		public virtual Task CloseAsync(string correlationId)
        {
            _client?.Dispose();
            _client = null;

            _address = null;

            _logger.Debug(correlationId, "Disconnected from {0}", _address);

            return Task.CompletedTask;
        }

        protected HttpContent CreateEntityContent(object value)
        {
            if (value == null) return null;

			var content = Commons.Convert.JsonConverter.ToJson(value);
			var result = new StringContent(content, Encoding.UTF8, "application/json");
            return result;
        }

        protected Uri CreateRequestUri(string route)
        {
            var builder = new StringBuilder(_address);

            if (!string.IsNullOrEmpty(_baseRoute))
            {
                if (_baseRoute[0] != '/')
                {
                    builder.Append('/');
                }
                builder.Append(_baseRoute);
            }

            if (!string.IsNullOrWhiteSpace(route))
            {
                if (route[0] != '?' && route[0] != '/')
                {
                    builder.Append('/');
                }
                builder.Append(route);
            }

            var uri = builder.ToString();

            var result = new Uri(uri, UriKind.Absolute);

            return result;
        }
      
        private async Task<HttpResponseMessage> ExecuteRequestAsync(
            string correlationId, HttpMethod method, Uri uri, HttpContent content = null)
        {
            if (_client == null)
                throw new InvalidOperationException("GraphQL client is not configured");

            // Set headers
            foreach (var key in _headers.Keys)
            {
                if (!_client.DefaultRequestHeaders.Contains(key))
                {
                    _client.DefaultRequestHeaders.Add(key, _headers[key]);
                }
            }

            HttpResponseMessage result = null;

            var retries = Math.Min(1, Math.Max(5, _retries));
            while (retries > 0)
            {
                try
                {
                    if (method == HttpMethod.Get)
                        result = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                    else if (method == HttpMethod.Post)
                        result = await _client.PostAsync(uri, content);
                    else if (method == HttpMethod.Put)
                        result = await _client.PutAsync(uri, content);
                    else if (method == HttpMethod.Delete)
                        result = await _client.DeleteAsync(uri);
#if !NETSTANDARD2_0
                    else if (method == HttpMethod.Patch)
                        result = await _client.PatchAsync(uri, content);
#endif
                    else
                        throw new InvalidOperationException("Invalid request type");

                    retries = 0;
                }
                catch (HttpRequestException ex)
                {
                    retries--;
                    if (retries > 0)
                    {
                        throw new ConnectionException(correlationId, null, "Unknown communication problem on GraphQL client", ex);
                    }
                    else
                    {
                        _logger.Trace(correlationId, $"Connection failed to uri '{uri}'. Retrying...");
                    }
                }
            }

            if (result == null)
            {
                throw ApplicationExceptionFactory.Create(ErrorDescriptionFactory.Create(
                    new UnknownException(correlationId, $"Unable to get a result from uri '{uri}' with method '{method}'")));
            }

            if ((int)result.StatusCode >= 400)
            {
                var responseContent = await result.Content.ReadAsStringAsync();

                ErrorDescription errorObject = null;
                try
                {
                    errorObject = Commons.Convert.JsonConverter.FromJson<ErrorDescription>(responseContent);
                }
                finally
                {
                    if (errorObject == null)
                    {
                        errorObject = ErrorDescriptionFactory.Create(new UnknownException(correlationId, $"UNKNOWN_ERROR with result status: '{result.StatusCode}'", responseContent));
                    }
                }

                throw ApplicationExceptionFactory.Create(errorObject);
            }

            return result;
        }

		public static T ExtractEntity<T>(string correlationId, string json, string fieldName)
		{
			try
			{
				var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
				var fieldValue = jsonObject.SelectToken("data." + fieldName)?.ToString();

				return JsonConvert.DeserializeObject<T>(fieldValue);
			}
			catch (Exception ex)
			{
				throw new BadRequestException(correlationId, null, "Unexpected protocol format", ex);
			}
		}

		/// <summary>
		/// Safely executes a remote method via HTTP/GraphQL protocol and logs execution time.
		/// </summary>
		/// <typeparam name="T">the class type</typeparam>
		/// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
		/// <param name="method">HTTP method: "post", "put", "patch"</param>
		/// <param name="route">a command route. Base route will be added to this route</param>
		/// <param name="requestEntity">request body object.</param>
		/// <returns>result object.</returns>
		protected async Task<T> ExecuteOperationAsync<T>(string correlationId, string operationName, object variables)
            where T : class
        {
            var methodName = _baseRoute + "." + operationName;

            using (var timing = Instrument(correlationId, methodName))
            {
                try
                {
                    return await ExecuteOperationAsync<T>(correlationId, operationName, variables, null);
                }
                catch (Exception ex)
                {
                    InstrumentError(correlationId, methodName, ex);
                    throw;
                }
            }
        }

        protected string FindQueryByOperationName(string operationName)
        {
			if (!_operations.TryGetValue(operationName, out string query))
				throw new InvalidOperationException($"Query or mutation with name {operationName} not found");

            return query;
		}

		/// <summary>
		/// Executes a remote HTTP/GraphQL request.
		/// </summary>
		/// <typeparam name="T">The expected response type</typeparam>
		/// <param name="correlationId">(Optional) Transaction ID to trace execution through the call chain.</param>
		/// <param name="operationName">The GraphQL operation name (e.g., "getUser").</param>
		/// <param name="variables">The request body object containing variables.</param>
		/// <param name="projectionParams">(Optional) Additional parameters for result projection.</param>
		/// <returns>The response object of the specified type.</returns>
		protected async Task<T> ExecuteOperationAsync<T>(string correlationId, string operationName, object variables, ProjectionParams projectionParams = null)
			where T : class
		{
			var query = FindQueryByOperationName(operationName);
			query = AddProjectionParams(query, projectionParams);

			var json = await ExecuteQueryAsync(correlationId, query, variables);

			return ExtractEntity<T>(correlationId, json, operationName);
		}

		private string AddProjectionParams(string query, ProjectionParams projectionParams)
		{
			if (projectionParams == null) return query;
            var projectionJson = GraphQLRequestHelper.FormatProjectionParams(projectionParams);
            return query.Replace("$projection", projectionJson);
		}

		/// <summary>
		/// Executes a remote GraphQL query via HTTP/GraphQL protocol.
		/// </summary>
		/// <param name="correlationId">(Optional) Transaction ID to trace execution through the call chain.</param>
		/// <param name="query">The GraphQL query string to execute.</param>
		/// <param name="variables">The request body object containing variables.</param>
		/// <returns>The JSON response as a string.</returns>
		protected async Task<string> ExecuteQueryAsync(string correlationId, string query, object variables)
		{
			var uri = CreateRequestUri("");

			var requestEntity = new
			{
				query,
				variables
			};

			using (var requestContent = CreateEntityContent(requestEntity))
			{
				using (var response = await ExecuteRequestAsync(correlationId, HttpMethod.Post, uri, requestContent))
				{
					return await response.Content.ReadAsStringAsync();
				}
			}
		}
	}
}

