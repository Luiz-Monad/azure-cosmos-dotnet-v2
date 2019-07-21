using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal interface IChannel
	{
		bool Healthy
		{
			get;
		}

		Task<StoreResponse> RequestAsync(DocumentServiceRequest request, Uri physicalAddress, ResourceOperation resourceOperation, Guid activityId);

		void Close();
	}
}
