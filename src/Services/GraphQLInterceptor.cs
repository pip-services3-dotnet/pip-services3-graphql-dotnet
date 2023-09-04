using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices3.GraphQL.Services
{
	public delegate ValueTask GraphQLInterceptor(
		HttpContext context,
		IRequestExecutor executor,
		IQueryRequestBuilder builder,
		CancellationToken token
	);

	internal class HttpRequestInterceptor : DefaultHttpRequestInterceptor
	{
		public GraphQLInterceptor Action 
		{ 
			get; 
			set; 
		}

		public HttpRequestInterceptor()
		{
			Action = (context, requestExecutor, requestBuilder, cancellationToken) => 
				base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
		}

		public override ValueTask OnCreateAsync(HttpContext context,
			IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
			CancellationToken cancellationToken)
		{
			return Action.Invoke(context, requestExecutor, requestBuilder, cancellationToken);
		}
	}
}
