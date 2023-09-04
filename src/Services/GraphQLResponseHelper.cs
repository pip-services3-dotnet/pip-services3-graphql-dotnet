using Microsoft.AspNetCore.Http;
using PipServices3.Commons.Convert;
using PipServices3.Commons.Errors;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PipServices3.GraphQL.Services
{
	public static class GraphQLResponseHelper
	{
		/// <summary>
		/// Sends error serialized as ErrorDescription object and appropriate HTTP status
		/// code.If status code is not defined, it uses 500 status code.
		/// </summary>
		/// <param name="response">a Http response</param>
		/// <param name="ex">an error object to be sent.</param>
		public static async Task SendErrorAsync(HttpResponse response, Exception ex)
		{
			// Unwrap exception
			if (ex is AggregateException)
			{
				var ex2 = ex as AggregateException;
				ex = ex2.InnerExceptions.Count > 0 ? ex2.InnerExceptions[0] : ex;
			}

			if (ex is PipServices3.Commons.Errors.ApplicationException)
			{
				response.ContentType = "application/json";
				var ex3 = ex as PipServices3.Commons.Errors.ApplicationException;
				response.StatusCode = ex3.Status;
				var contentResult = JsonConverter.ToJson(ErrorDescriptionFactory.Create(ex3));
				await response.WriteAsync(contentResult);
			}
			else
			{
				response.ContentType = "application/json";
				response.StatusCode = (int)HttpStatusCode.InternalServerError;
				var contentResult = JsonConverter.ToJson(ErrorDescriptionFactory.Create(ex));
				await response.WriteAsync(contentResult);
			}
		}

	}
}
