using GraphQL;
using GraphQL.Transport;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using PipServices3.Commons.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PipServices3.GraphQL.Services
{
	/// <summary>
	/// Helper class that handles HTTP-based requests.
	/// </summary>
	public static class GraphQLRequestHelper
	{
		public static string GetCorrelationId(IResolveFieldContext context)
		{
			string correlationId;
			var parent = context;
			
			do
			{
				correlationId = parent.GetArgument<string>("correlationId");
				parent = parent.Parent;	
			}
			while (parent != null && correlationId == null);

			return correlationId;
		}

		public static FilterParams GetFilterParams(IResolveFieldContext context)
		{
			var filter = new FilterParams();
			var parser = FilterParams.FromString(context.GetArgument<string>("filter"));

			foreach (var filterParam in parser)
			{
				filter.Set(filterParam.Key, filterParam.Value);
			}

			return filter;
		}

		public static PagingParams GetPagingParams(IResolveFieldContext context)
		{
			return context.GetArgument<PagingParams>("paging");
		}

		public static SortParams GetSortParams(IResolveFieldContext context)
		{
			var fields = context.GetArgument<List<SortField>>("sort");
			return fields == null ? null : new SortParams(fields);
		}
		
		public static GraphQLRequest GetRequest(HttpRequest request, IGraphQLTextSerializer serializer)
		{
			string requestJson = GetRequestBody(request);
			return serializer.Deserialize<GraphQLRequest>(requestJson);
		}

		private static string GetRequestBody(HttpRequest request)
		{
			string body;

			using (var streamReader = new StreamReader(request.Body))
			{
				body = streamReader.ReadToEnd();
			}

			return body;
		}

		public static ProjectionParams GetProjectionParams(IResolveFieldContext context)
		{
			var projections = context.GetArgument<ProjectionParams>("projection");
			if (projections?.Count > 0) return projections;	

			projections = new ProjectionParams();

			var selections = GetSelections(context.Operation?.SelectionSet);
			var selection = selections.FirstOrDefault();

			foreach (var field in GetProjectionFields("", GetSelections(selection)))
			{
				projections.Add(ConvertCamelToSnake(field));
			}

			return projections;
		}

		private static List<string> GetProjectionFields(string root, IEnumerable<GraphQLField> selections)
		{
			var results = new List<string>();
			foreach (var selection in selections)
			{
				var name = selection.Name.StringValue;
				if (name.StartsWith("__")) continue; // ignore introspection fields

				var projField = string.IsNullOrWhiteSpace(root)
					? name
					: $"{root}.{name}";

				var subSelections = GetSelections(selection);
				if (subSelections.Any())
				{
					foreach (var field in GetProjectionFields(projField, subSelections))
					{
						results.Add(field);
					}
				}
				else
				{
					results.Add(projField);
				}
			}
			
			return results;
		}

		private static IEnumerable<GraphQLField> GetSelections(GraphQLField graphQLField)
		{
			return GetSelections(graphQLField?.SelectionSet);
		}

		private static IEnumerable<GraphQLField> GetSelections(GraphQLSelectionSet graphQLSet)
		{
			var selections = graphQLSet?.Selections;

			if (selections != null)
			{
				foreach (var selection in selections)
				{
					if (selection is GraphQLField graphQLField)
					{
						yield return graphQLField;
					}
				}
			}
		}

		public static string FormatProjectionParams(ProjectionParams projectionParams)
		{
			var dict = ConvertToDictionary(projectionParams);
			return "{ " + FormatQuery(dict) + " }";
		}

		private static Dictionary<string, object> ConvertToDictionary(IEnumerable<string> values)
		{
			if (!values.Any()) return null;

			var result = new Dictionary<string, object>();
			var groups = new Dictionary<string, List<string>>();

			foreach (var value in values.Select(x => Split(x, '.'))) 
			{
				var group = value[0];
				if (!groups.TryGetValue(group, out List<string> list))
				{ 
					list = new List<string>();
					groups.Add(group, list);
				}

				if (value.Length > 1)
				{
					list.Add(value[1]);
				}
			}
				
			foreach (string group in groups.Keys)
			{
				result.Add(group, ConvertToDictionary(groups[group]));
			}

			return result;
		}

		private static string FormatQuery(Dictionary<string, object> hierarchy)
		{
			StringBuilder result = new StringBuilder();

			bool first = true;	
			foreach (var key in hierarchy.Keys)
			{
				if (!first) result.Append(" ");

				result.Append(ConvertSnakeToCamel(key));

				if (hierarchy[key] is Dictionary<string, object> keyItems)
				{
					result
						.Append(" { ")
						.Append(FormatQuery(keyItems))
						.Append(" }");
				}

				first = false;
			}

			return result.ToString();
		}

		private static string[] Split(string s, char ch)
		{
			var index = s.IndexOf(ch);
			return index >= 0 ? (new[] { s.Substring(0, index), s.Substring(index + 1) }) : (new[] { s });
		}

		public static string ConvertCamelToSnake(string input)
		{
			StringBuilder result = new StringBuilder();
			
			for (int i = 0; i < input.Length; i++)
			{
				char currentChar = input[i];
				if (char.IsUpper(currentChar))
				{
					if (i > 0)
					{
						result.Append('_');
					}
					result.Append(char.ToLower(currentChar));
				}
				else
				{
					result.Append(currentChar);
				}
			}

			return result.ToString();
		}

		public static string ConvertSnakeToCamel(string input)
		{
			string[] words = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
			
			StringBuilder result = new StringBuilder();
			
			for (int i = 0; i < words.Length; i++)
			{
				var word = words[i];
				if (i > 0)
				{
					result.Append(char.ToUpper(word[0]) + word.Substring(1));
				}
				else
				{
					result.Append(char.ToLower(word[0]) + word.Substring(1));
				}
			}

			return result.ToString();
		}
	}
}
