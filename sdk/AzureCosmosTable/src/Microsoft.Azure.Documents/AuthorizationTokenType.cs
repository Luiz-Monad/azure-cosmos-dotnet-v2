namespace Microsoft.Azure.Documents
{
	internal enum AuthorizationTokenType
	{
		Invalid,
		PrimaryMasterKey,
		PrimaryReadonlyMasterKey,
		SecondaryMasterKey,
		SecondaryReadonlyMasterKey,
		SystemReadOnly,
		SystemReadWrite,
		SystemAll,
		ResourceToken,
		ComputeGatewayKey
	}
}
