using System;
using System.Net;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Http;
using PipServices3.Commons.Convert;
using PipServices3.Commons.Errors;

namespace PipServices3.GraphQL.Services
{
    /// <summary>
    /// Helper class that handles HTTP-based responses.
    /// </summary>
    public static class GraphQLResponseSender
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

        public static async Task SendErrorAsync(HttpResponse response, IGraphQLTextSerializer serializer, ExecutionResult result)
        {
	        response.ContentType = "application/json";
			response.StatusCode = (int)HttpStatusCode.BadRequest;
			string contentResult = serializer.Serialize(result);
			await response.WriteAsync(contentResult);
		}

		/// <summary>
		/// Creates a callback function that sends result as JSON object. That callack
		/// function call be called directly or passed as a parameter to business logic components.
		/// 
		/// If object is not null it returns 200 status code. For null results it returns
		/// 204 status code. If error occur it sends ErrorDescription with approproate status code.
		/// </summary>
		/// <param name="response">a Http response</param>
		/// <param name="result">a body object to result.</param>
		public static async Task SendResultAsync(HttpResponse response, IGraphQLTextSerializer serializer, ExecutionResult result)
        {
			if (result == null)
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
            }
			else
            {
				response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.OK;
				string contentResult = serializer.Serialize(result);
				await response.WriteAsync(contentResult);
            }
        }

        /// <summary>
        /// Creates a callback function that sends an empty result with 204 status code.
        /// If error occur it sends ErrorDescription with approproate status code.
        /// </summary>
        /// <param name="response">aHttp response</param>
        public static async Task SendEmptyResultAsync(HttpResponse response)
        {
            response.ContentType = "application/json";
            response.StatusCode = (int)HttpStatusCode.NoContent;
            await Task.Delay(0);
        }

        /// <summary>
        /// Creates a callback function that sends newly created object as JSON. That
        /// callack function call be called directly or passed as a parameter to business logic components.
        /// 
        /// If object is not null it returns 201 status code. For null results it returns
        /// 204 status code. If error occur it sends ErrorDescription with approproate status code.
        /// </summary>
        /// <param name="response">a Http response</param>
        /// <param name="result">a body object to created result</param>
        public static async Task SendCreatedResultAsync(HttpResponse response, object result)
        {
            if (result == null)
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else
            {
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.Created;
                var contentResult = JsonConverter.ToJson(result);
                await response.WriteAsync(contentResult);
            }
        }

        /// <summary>
        /// Creates a callback function that sends deleted object as JSON. That callack
        /// function call be called directly or passed as a parameter to business logic components.
        /// 
        /// If object is not null it returns 200 status code. For null results it returns
        /// 204 status code. If error occur it sends ErrorDescription with approproate status code.
        /// </summary>
        /// <param name="response">a Http response</param>
        /// <param name="result">a body object to deleted result</param>
        public static async Task SendDeletedResultAsync(HttpResponse response, object result)
        {
            if (result == null)
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else
            {
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.OK;
                var contentResult = JsonConverter.ToJson(result);
                await response.WriteAsync(contentResult);
            }
        }

    }
}
