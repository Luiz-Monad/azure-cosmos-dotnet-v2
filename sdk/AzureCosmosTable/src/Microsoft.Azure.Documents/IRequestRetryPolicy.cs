using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Retry policy to evaluate a Response or Exception through <see cref="T:Microsoft.Azure.Documents.RequestRetryUtility" />.
	/// </summary>
	/// <remarks>The <see cref="T:Microsoft.Azure.Documents.RequestRetryUtility" /> will invoke <see cref="M:Microsoft.Azure.Documents.IRequestRetryPolicy`2.TryHandleResponseSynchronously(`0,`1,System.Exception,Microsoft.Azure.Documents.ShouldRetryResult@)" /> first and based on the result, it will invoke <see cref="M:Microsoft.Azure.Documents.IRequestRetryPolicy`2.ShouldRetryAsync(`0,`1,System.Exception,System.Threading.CancellationToken)" /></remarks>
	internal interface IRequestRetryPolicy<TRequest, TResponse>
	{
		/// <summary>
		/// Method that is called to determine from the policy that needs to retry on the Response.
		/// </summary>
		/// <param name="request">Request that generated the Response.</param>
		/// <param name="response">Response to be evaluated.</param>
		/// <param name="exception">Exception, if any, that was captured during execution.</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task<ShouldRetryResult> ShouldRetryAsync(TRequest request, TResponse response, Exception exception, CancellationToken cancellationToken);

		/// <summary>
		/// Tries to handle the response or exception synchronously.
		/// </summary>
		/// <param name="response">Response to be evaluated.</param>
		/// <param name="request">Request that generated the Response.</param>
		/// <param name="exception">Exception, if any, that was captured during execution.</param>
		/// <param name="shouldRetryResult">Populated if the return value is true.</param>
		/// <returns>If true, use the value of <paramref name="shouldRetryResult" />. If false, call ShouldRetryAsync.</returns>
		bool TryHandleResponseSynchronously(TRequest request, TResponse response, Exception exception, out ShouldRetryResult shouldRetryResult);

		/// <summary>
		/// Method to execute before sending the Request.
		/// </summary>
		/// <param name="request">Request to process.</param>
		void OnBeforeSendRequest(TRequest request);
	}
	/// <summary>
	/// Retry policy to evaluate a Response through <see cref="T:Microsoft.Azure.Documents.RequestRetryUtility" /> that includes arguments on the execution call.
	/// </summary>
	/// <remarks><see cref="P:Microsoft.Azure.Documents.IRequestRetryPolicy`3.ExecuteContext" /> will be passed on each invocation of the delegate.</remarks>
	internal interface IRequestRetryPolicy<TPolicyContext, TRequest, TResponse> : IRequestRetryPolicy<TRequest, TResponse>
	{
		/// <summary>
		/// Get the context from the policy to be sent to the execution context call.
		/// </summary>
		TPolicyContext ExecuteContext
		{
			get;
		}
	}
}
