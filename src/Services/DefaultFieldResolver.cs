using GraphQL.Resolvers;
using GraphQL;
using GraphQLParser.AST;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types;
using System.Linq;
using System.Collections.Concurrent;

namespace PipServices3.GraphQL.Services
{
	internal class DefaultFieldResolver : IFieldResolver
	{
		private const string OPERATION_QUERY = "query";
		private const string OPERATION_MUTATION = "mutation";

		private Schema _schema;
		private ConcurrentDictionary<(string, string), Func<IResolveFieldContext, Task<object>>> _resolveFuncs;

		public DefaultFieldResolver(Schema schema)
		{
			_schema = schema ?? throw new ArgumentNullException("schema");
			_resolveFuncs = new ConcurrentDictionary<(string, string), Func<IResolveFieldContext, Task<object>>>();
		}

		public async ValueTask<object> ResolveAsync(IResolveFieldContext context)
		{
			string operation;
			switch (context.Operation.Operation)
			{
				case OperationType.Query: operation = OPERATION_QUERY; break;
				case OperationType.Mutation: operation = OPERATION_MUTATION; break;
				default: throw new NotSupportedException($"Operation {context.Operation.Operation} not supported");
			}; 

			//var path = string.Join(".", context.Path);
			var path = context.ParentType.Name + "." + context.FieldDefinition.Name;

			if (!_resolveFuncs.TryGetValue((operation, path), out var func))
				return null;

			return await func(context);
		}

		public void RegisterQuery(string typeName, string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			if (!TryRegisterQuery(typeName, path, resolverFunc))
				throw new Exception($"Not found {OPERATION_QUERY} {path}");
		}

		public void RegisterMutation(string typeName, string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			if (!TryRegisterMutation(typeName, path, resolverFunc))
				throw new Exception($"Not found {OPERATION_MUTATION} {path}");
		}

		public bool TryRegisterQuery(string typeName, string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			var field = FindQueryField(_schema, path);
			if (field == null) return false;

			field.Resolver = this;
			RegisterResolver(OPERATION_QUERY, typeName, path, resolverFunc);
			return true;
		}

		public bool TryRegisterMutation(string typeName, string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			var field = FindMutationField(_schema, path);
			if (field == null) return false;

			field.Resolver = this;
			RegisterResolver(OPERATION_MUTATION, typeName, path, resolverFunc);
			return true;
		}

		private void RegisterResolver(string operation, string typeName, string path, Func<IResolveFieldContext, Task<object>> resolverFunc)
		{
			path = path.Contains(".") ? path : $"{typeName}.{path}";

			var key = (operation, path);
			if (_resolveFuncs.ContainsKey(key)) throw new InvalidOperationException();

			_resolveFuncs[key] = resolverFunc;
		}

		private static FieldType FindQueryField(Schema schema, string path)
		{
			var parts = path.Split('.');
			if (parts.Length == 2)
			{
				var addType = schema.AdditionalTypeInstances.Where(x => x.Name == parts[0]).FirstOrDefault() as ObjectGraphType;
				var field = addType?.Fields.FirstOrDefault(x => x.Name == parts[1]);
				return field;
			}
			else
			{
				var queryType = schema.AdditionalTypeInstances.Where(x => x.Name == "Query").FirstOrDefault() as ObjectGraphType;
				var field = queryType?.Fields.FirstOrDefault(x => x.Name == path);
				return field;
			}
		}

		private static FieldType FindMutationField(Schema schema, string name)
		{
			var field = schema.Mutation?.Fields.FirstOrDefault(x => x.Name == name);
			return field;
		}
	}
}
