using System;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// RetryOptions class defines the parameters an application can set to customize the
	/// built-in retry policies in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// The <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class supports retry
	/// on certain types of exceptions. This class provides options for applications to control the
	/// retry behavior.
	/// </remarks>
	public class RetryOptions
	{
		private int maxRetryAttemptsOnThrottledRequests;

		private int maxRetryWaitTime;

		/// <summary>
		/// Gets or sets the maximum number of retries in the case where the request fails
		/// because the Azure Cosmos DB service has applied rate limiting on the client.
		/// </summary>
		/// <value>
		/// The default value is 9. This means in the case where the request is rate limited,
		/// the same request will be issued for a maximum of 10 times to the server before 
		/// an error is returned to the application. If the value of this property is set to 0, 
		/// there will be no automatic retry on rate limiting requests from the client and the exception
		/// needs to handled at the application level. 
		/// For an example on how to set this value, please refer to <see cref="P:Microsoft.Azure.Documents.Client.ConnectionPolicy.RetryOptions" />.
		/// </value>
		/// <remarks>
		/// <para>
		/// When a client is sending requests faster than the allowed rate,
		/// the service will return HttpStatusCode 429 (Too Many Request) to rate limit the client. The current
		/// implementation in the SDK will then wait for the amount of time the service tells it to wait and
		/// retry after the time has elapsed.  
		/// </para>
		/// <para>
		/// For more information, see <see href="https://docs.microsoft.com/en-us/azure/documentdb/documentdb-performance-tips#429">Handle rate limiting/request rate too large</see>.
		/// </para>
		/// </remarks>
		public int MaxRetryAttemptsOnThrottledRequests
		{
			get
			{
				return maxRetryAttemptsOnThrottledRequests;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("value must be a positive integer.");
				}
				maxRetryAttemptsOnThrottledRequests = value;
			}
		}

		/// <summary>
		/// Gets or sets the maximum retry time in seconds for the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The default value is 30 seconds. For an example on how to set this value, please refer to <see cref="P:Microsoft.Azure.Documents.Client.ConnectionPolicy.RetryOptions" />.
		/// </value>
		/// <remarks>
		/// <para>
		/// When a request fails due to a rate limiting error, the service sends back a response that
		/// contains a value indicating the client should not retry before the <see cref="P:Microsoft.Azure.Documents.DocumentClientException.RetryAfter" /> time period has
		/// elapsed. This property allows the application to set a maximum wait time for all retry attempts.
		/// If the cumulative wait time exceeds the this value, the client will stop retrying and return the error to the application.
		/// </para>
		/// <para>
		/// For more information, see <see href="https://docs.microsoft.com/en-us/azure/documentdb/documentdb-performance-tips#429">Handle rate limiting/request rate too large</see>.
		/// </para>
		/// </remarks>
		public int MaxRetryWaitTimeInSeconds
		{
			get
			{
				return maxRetryWaitTime;
			}
			set
			{
				if (value < 0 || value > 2147483)
				{
					throw new ArgumentException("value must be a positive integer between the range of 0 to " + 2147483);
				}
				maxRetryWaitTime = value;
			}
		}

		/// <summary>
		/// Creates a new instance of the RetryOptions class and intialize all properties
		/// to default values for the Azure Cosmos DB service.
		/// </summary>
		public RetryOptions()
		{
			maxRetryAttemptsOnThrottledRequests = 9;
			maxRetryWaitTime = 30;
		}
	}
}
