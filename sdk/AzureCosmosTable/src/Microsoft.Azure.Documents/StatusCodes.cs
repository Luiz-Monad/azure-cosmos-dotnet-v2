namespace Microsoft.Azure.Documents
{
	internal enum StatusCodes
	{
		Ok = 200,
		Created = 201,
		Accepted = 202,
		NoContent = 204,
		MultiStatus = 207,
		NotModified = 304,
		BadRequest = 400,
		Unauthorized = 401,
		Forbidden = 403,
		NotFound = 404,
		MethodNotAllowed = 405,
		RequestTimeout = 408,
		Conflict = 409,
		Gone = 410,
		PreconditionFailed = 412,
		RequestEntityTooLarge = 413,
		Locked = 423,
		FailedDependency = 424,
		TooManyRequests = 429,
		RetryWith = 449,
		InternalServerError = 500,
		BadGateway = 502,
		ServiceUnavailable = 503,
		OperationPaused = 1200,
		OperationCancelled = 1201
	}
}
