using System;
using System.Diagnostics.Tracing;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents.Client
{
	[EventSource(Name = "DocumentDBClient", Guid = "f832a342-0a53-5bab-b57b-d5bc65319768")]
	internal class DocumentClientEventSource : EventSource, ICommunicationEventSource
	{
		public class Keywords
		{
			public const EventKeywords HttpRequestAndResponse = (EventKeywords)1L;
		}

		private static Lazy<DocumentClientEventSource> documentClientEventSourceInstance = new Lazy<DocumentClientEventSource>(() => new DocumentClientEventSource());

		public static DocumentClientEventSource Instance => documentClientEventSourceInstance.Value;

		internal DocumentClientEventSource()
		{
		}

		[NonEvent]
		private unsafe void WriteEventCoreWithActivityId(Guid activityId, int eventId, int eventDataCount, EventData* dataDesc)
		{
			CustomTypeExtensions.SetActivityId(ref activityId);
			WriteEventCore(eventId, eventDataCount, dataDesc);
		}

		[Event(1, Message = "HttpRequest to URI '{2}' with resourceType '{3}' and request headers: accept '{4}', authorization '{5}', consistencyLevel '{6}', contentType '{7}', contentEncoding '{8}', contentLength '{9}', contentLocation '{10}', continuation '{11}', emitVerboseTracesInQuery '{12}', enableScanInQuery '{13}', eTag '{14}', httpDate '{15}', ifMatch '{16}', ifNoneMatch '{17}', indexingDirective '{18}', keepAlive '{19}', offerType '{20}', pageSize '{21}', preTriggerExclude '{22}', preTriggerInclude '{23}', postTriggerExclude '{24}', postTriggerInclude '{25}', profileRequest '{26}', resourceTokenExpiry '{27}', sessionToken '{28}', setCookie '{29}', slug '{30}', userAgent '{31}', xDate'{32}'. ActivityId {0}, localId {1}", Keywords = (EventKeywords)1L, Level = EventLevel.Verbose)]
		private unsafe void Request(Guid activityId, Guid localId, string uri, string resourceType, string accept, string authorization, string consistencyLevel, string contentType, string contentEncoding, string contentLength, string contentLocation, string continuation, string emitVerboseTracesInQuery, string enableScanInQuery, string eTag, string httpDate, string ifMatch, string ifNoneMatch, string indexingDirective, string keepAlive, string offerType, string pageSize, string preTriggerExclude, string preTriggerInclude, string postTriggerExclude, string postTriggerInclude, string profileRequest, string resourceTokenExpiry, string sessionToken, string setCookie, string slug, string userAgent, string xDate)
		{
			if (uri == null)
			{
				throw new ArgumentException("uri");
			}
			if (resourceType == null)
			{
				throw new ArgumentException("resourceType");
			}
			if (accept == null)
			{
				throw new ArgumentException("accept");
			}
			if (authorization == null)
			{
				throw new ArgumentException("authorization");
			}
			if (consistencyLevel == null)
			{
				throw new ArgumentException("consistencyLevel");
			}
			if (contentType == null)
			{
				throw new ArgumentException("contentType");
			}
			if (contentEncoding == null)
			{
				throw new ArgumentException("contentEncoding");
			}
			if (contentLength == null)
			{
				throw new ArgumentException("contentLength");
			}
			if (contentLocation == null)
			{
				throw new ArgumentException("contentLocation");
			}
			if (continuation == null)
			{
				throw new ArgumentException("continuation");
			}
			if (emitVerboseTracesInQuery == null)
			{
				throw new ArgumentException("emitVerboseTracesInQuery");
			}
			if (enableScanInQuery == null)
			{
				throw new ArgumentException("enableScanInQuery");
			}
			if (eTag == null)
			{
				throw new ArgumentException("eTag");
			}
			if (httpDate == null)
			{
				throw new ArgumentException("httpDate");
			}
			if (ifMatch == null)
			{
				throw new ArgumentException("ifMatch");
			}
			if (ifNoneMatch == null)
			{
				throw new ArgumentException("ifNoneMatch");
			}
			if (indexingDirective == null)
			{
				throw new ArgumentException("indexingDirective");
			}
			if (keepAlive == null)
			{
				throw new ArgumentException("keepAlive");
			}
			if (offerType == null)
			{
				throw new ArgumentException("offerType");
			}
			if (pageSize == null)
			{
				throw new ArgumentException("pageSize");
			}
			if (preTriggerExclude == null)
			{
				throw new ArgumentException("preTriggerExclude");
			}
			if (preTriggerInclude == null)
			{
				throw new ArgumentException("preTriggerInclude");
			}
			if (postTriggerExclude == null)
			{
				throw new ArgumentException("postTriggerExclude");
			}
			if (postTriggerInclude == null)
			{
				throw new ArgumentException("postTriggerInclude");
			}
			if (profileRequest == null)
			{
				throw new ArgumentException("profileRequest");
			}
			if (resourceTokenExpiry == null)
			{
				throw new ArgumentException("resourceTokenExpiry");
			}
			if (sessionToken == null)
			{
				throw new ArgumentException("sessionToken");
			}
			if (setCookie == null)
			{
				throw new ArgumentException("setCookie");
			}
			if (slug == null)
			{
				throw new ArgumentException("slug");
			}
			if (userAgent == null)
			{
				throw new ArgumentException("userAgent");
			}
			if (xDate == null)
			{
				throw new ArgumentException("xDate");
			}
			byte[] array = activityId.ToByteArray();
			byte[] array2 = localId.ToByteArray();
			fixed (byte* value = array)
			{
				fixed (byte* value2 = array2)
				{
					fixed (char* value3 = uri)
					{
						fixed (char* value4 = resourceType)
						{
							fixed (char* value5 = accept)
							{
								fixed (char* value6 = authorization)
								{
									fixed (char* value7 = consistencyLevel)
									{
										fixed (char* value8 = contentType)
										{
											fixed (char* value9 = contentEncoding)
											{
												fixed (char* value10 = contentLength)
												{
													fixed (char* value11 = contentLocation)
													{
														fixed (char* value12 = continuation)
														{
															fixed (char* value13 = emitVerboseTracesInQuery)
															{
																fixed (char* value14 = enableScanInQuery)
																{
																	fixed (char* value15 = eTag)
																	{
																		fixed (char* value16 = httpDate)
																		{
																			fixed (char* value17 = ifMatch)
																			{
																				fixed (char* value18 = ifNoneMatch)
																				{
																					fixed (char* value19 = indexingDirective)
																					{
																						fixed (char* value20 = keepAlive)
																						{
																							fixed (char* value21 = offerType)
																							{
																								fixed (char* value22 = pageSize)
																								{
																									fixed (char* value23 = preTriggerExclude)
																									{
																										fixed (char* value24 = preTriggerInclude)
																										{
																											fixed (char* value25 = postTriggerExclude)
																											{
																												fixed (char* value26 = postTriggerInclude)
																												{
																													fixed (char* value27 = profileRequest)
																													{
																														fixed (char* value28 = resourceTokenExpiry)
																														{
																															fixed (char* value29 = sessionToken)
																															{
																																fixed (char* value30 = setCookie)
																																{
																																	fixed (char* value31 = slug)
																																	{
																																		fixed (char* value32 = userAgent)
																																		{
																																			fixed (char* value33 = xDate)
																																			{
																																				EventData* ptr = stackalloc EventData[33];
																																				ptr->DataPointer = (IntPtr)(void*)value;
																																				ptr->Size = array.Length;
																																				ptr[1].DataPointer = (IntPtr)(void*)value2;
																																				ptr[1].Size = array2.Length;
																																				ptr[2].DataPointer = (IntPtr)(void*)value3;
																																				ptr[2].Size = (uri.Length + 1) * 2;
																																				ptr[3].DataPointer = (IntPtr)(void*)value4;
																																				ptr[3].Size = (resourceType.Length + 1) * 2;
																																				ptr[4].DataPointer = (IntPtr)(void*)value5;
																																				ptr[4].Size = (accept.Length + 1) * 2;
																																				ptr[5].DataPointer = (IntPtr)(void*)value6;
																																				ptr[5].Size = (authorization.Length + 1) * 2;
																																				ptr[6].DataPointer = (IntPtr)(void*)value7;
																																				ptr[6].Size = (consistencyLevel.Length + 1) * 2;
																																				ptr[7].DataPointer = (IntPtr)(void*)value8;
																																				ptr[7].Size = (contentType.Length + 1) * 2;
																																				ptr[8].DataPointer = (IntPtr)(void*)value9;
																																				ptr[8].Size = (contentEncoding.Length + 1) * 2;
																																				ptr[9].DataPointer = (IntPtr)(void*)value10;
																																				ptr[9].Size = (contentLength.Length + 1) * 2;
																																				ptr[10].DataPointer = (IntPtr)(void*)value11;
																																				ptr[10].Size = (contentLocation.Length + 1) * 2;
																																				ptr[11].DataPointer = (IntPtr)(void*)value12;
																																				ptr[11].Size = (continuation.Length + 1) * 2;
																																				ptr[12].DataPointer = (IntPtr)(void*)value13;
																																				ptr[12].Size = (emitVerboseTracesInQuery.Length + 1) * 2;
																																				ptr[13].DataPointer = (IntPtr)(void*)value14;
																																				ptr[13].Size = (enableScanInQuery.Length + 1) * 2;
																																				ptr[14].DataPointer = (IntPtr)(void*)value15;
																																				ptr[14].Size = (eTag.Length + 1) * 2;
																																				ptr[15].DataPointer = (IntPtr)(void*)value16;
																																				ptr[15].Size = (httpDate.Length + 1) * 2;
																																				ptr[16].DataPointer = (IntPtr)(void*)value17;
																																				ptr[16].Size = (ifMatch.Length + 1) * 2;
																																				ptr[17].DataPointer = (IntPtr)(void*)value18;
																																				ptr[17].Size = (ifNoneMatch.Length + 1) * 2;
																																				ptr[18].DataPointer = (IntPtr)(void*)value19;
																																				ptr[18].Size = (indexingDirective.Length + 1) * 2;
																																				ptr[19].DataPointer = (IntPtr)(void*)value20;
																																				ptr[19].Size = (keepAlive.Length + 1) * 2;
																																				ptr[20].DataPointer = (IntPtr)(void*)value21;
																																				ptr[20].Size = (offerType.Length + 1) * 2;
																																				ptr[21].DataPointer = (IntPtr)(void*)value22;
																																				ptr[21].Size = (pageSize.Length + 1) * 2;
																																				ptr[22].DataPointer = (IntPtr)(void*)value23;
																																				ptr[22].Size = (preTriggerExclude.Length + 1) * 2;
																																				ptr[23].DataPointer = (IntPtr)(void*)value24;
																																				ptr[23].Size = (preTriggerInclude.Length + 1) * 2;
																																				ptr[24].DataPointer = (IntPtr)(void*)value25;
																																				ptr[24].Size = (postTriggerExclude.Length + 1) * 2;
																																				ptr[25].DataPointer = (IntPtr)(void*)value26;
																																				ptr[25].Size = (postTriggerInclude.Length + 1) * 2;
																																				ptr[26].DataPointer = (IntPtr)(void*)value27;
																																				ptr[26].Size = (profileRequest.Length + 1) * 2;
																																				ptr[27].DataPointer = (IntPtr)(void*)value28;
																																				ptr[27].Size = (resourceTokenExpiry.Length + 1) * 2;
																																				ptr[28].DataPointer = (IntPtr)(void*)value29;
																																				ptr[28].Size = (sessionToken.Length + 1) * 2;
																																				ptr[29].DataPointer = (IntPtr)(void*)value30;
																																				ptr[29].Size = (setCookie.Length + 1) * 2;
																																				ptr[30].DataPointer = (IntPtr)(void*)value31;
																																				ptr[30].Size = (slug.Length + 1) * 2;
																																				ptr[31].DataPointer = (IntPtr)(void*)value32;
																																				ptr[31].Size = (userAgent.Length + 1) * 2;
																																				ptr[32].DataPointer = (IntPtr)(void*)value33;
																																				ptr[32].Size = (xDate.Length + 1) * 2;
																																				WriteEventCoreWithActivityId(activityId, 1, 33, ptr);
																																			}
																																		}
																																	}
																																}
																															}
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[NonEvent]
		public void Request(Guid activityId, Guid localId, string uri, string resourceType, HttpRequestHeaders requestHeaders)
		{
			if (IsEnabled(EventLevel.Verbose, (EventKeywords)1L))
			{
				string[] keys = new string[29]
				{
					"Accept",
					"authorization",
					"x-ms-consistency-level",
					"Content-Type",
					"Content-Encoding",
					"Content-Length",
					"Content-Location",
					"x-ms-continuation",
					"x-ms-documentdb-query-emit-traces",
					"x-ms-documentdb-query-enable-scan",
					"etag",
					"date",
					"If-Match",
					"If-None-Match",
					"x-ms-indexing-directive",
					"Keep-Alive",
					"x-ms-offer-type",
					"x-ms-max-item-count",
					"x-ms-documentdb-pre-trigger-exclude",
					"x-ms-documentdb-pre-trigger-include",
					"x-ms-documentdb-post-trigger-exclude",
					"x-ms-documentdb-post-trigger-include",
					"x-ms-profile-request",
					"x-ms-documentdb-expiry-seconds",
					"x-ms-session-token",
					"Set-Cookie",
					"Slug",
					"User-Agent",
					"x-ms-date"
				};
				string[] array = Helpers.ExtractValuesFromHTTPHeaders(requestHeaders, keys);
				Request(activityId, localId, uri, resourceType, array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7], array[8], array[9], array[10], array[11], array[12], array[13], array[14], array[15], array[16], array[17], array[18], array[19], array[20], array[21], array[22], array[23], array[24], array[25], array[26], array[27], array[28]);
			}
		}

		[Event(2, Message = "HttpResponse took {3}ms with status code {2} and response headers: contentType '{4}', contentEncoding '{5}', contentLength '{6}', contentLocation '{7}', currentMediaStorageUsageInMB '{8}', currentResourceQuotaUsage '{9}', databaseAccountConsumedDocumentStorageInMB '{10}', databaseAccountProvisionedDocumentStorageInMB '{11}', databaseAccountReservedDocumentStorageInMB '{12}', gatewayVersion '{13}', indexingDirective '{14}', itemCount '{15}', lastStateChangeUtc '{16}', maxMediaStorageUsageInMB '{17}', maxResourceQuota '{18}', newResourceId '{19}', ownerFullName '{20}', ownerId '{21}', requestCharge '{22}', requestValidationFailure '{23}', retryAfter '{24}', retryAfterInMilliseconds '{25}', serverVersion '{26}', schemaVersion '{27}', sessionToken '{28}', version '{29}'. ActivityId {0}, localId {1}", Keywords = (EventKeywords)1L, Level = EventLevel.Verbose)]
		private unsafe void Response(Guid activityId, Guid localId, short statusCode, double milliseconds, string contentType, string contentEncoding, string contentLength, string contentLocation, string currentMediaStorageUsageInMB, string currentResourceQuotaUsage, string databaseAccountConsumedDocumentStorageInMB, string databaseAccountProvisionedDocumentStorageInMB, string databaseAccountReservedDocumentStorageInMB, string gatewayVersion, string indexingDirective, string itemCount, string lastStateChangeUtc, string maxMediaStorageUsageInMB, string maxResourceQuota, string newResourceId, string ownerFullName, string ownerId, string requestCharge, string requestValidationFailure, string retryAfter, string retryAfterInMilliseconds, string serverVersion, string schemaVersion, string sessionToken, string version)
		{
			if (contentType == null)
			{
				throw new ArgumentException("contentType");
			}
			if (contentEncoding == null)
			{
				throw new ArgumentException("contentEncoding");
			}
			if (contentLength == null)
			{
				throw new ArgumentException("contentLength");
			}
			if (contentLocation == null)
			{
				throw new ArgumentException("contentLocation");
			}
			if (currentMediaStorageUsageInMB == null)
			{
				throw new ArgumentException("currentMediaStorageUsageInMB");
			}
			if (currentResourceQuotaUsage == null)
			{
				throw new ArgumentException("currentResourceQuotaUsage");
			}
			if (databaseAccountConsumedDocumentStorageInMB == null)
			{
				throw new ArgumentException("databaseAccountConsumedDocumentStorageInMB");
			}
			if (databaseAccountProvisionedDocumentStorageInMB == null)
			{
				throw new ArgumentException("databaseAccountProvisionedDocumentStorageInMB");
			}
			if (databaseAccountReservedDocumentStorageInMB == null)
			{
				throw new ArgumentException("databaseAccountReservedDocumentStorageInMB");
			}
			if (gatewayVersion == null)
			{
				throw new ArgumentException("gatewayVersion");
			}
			if (indexingDirective == null)
			{
				throw new ArgumentException("indexingDirective");
			}
			if (itemCount == null)
			{
				throw new ArgumentException("itemCount");
			}
			if (lastStateChangeUtc == null)
			{
				throw new ArgumentException("lastStateChangeUtc");
			}
			if (maxMediaStorageUsageInMB == null)
			{
				throw new ArgumentException("maxMediaStorageUsageInMB");
			}
			if (maxResourceQuota == null)
			{
				throw new ArgumentException("maxResourceQuota");
			}
			if (newResourceId == null)
			{
				throw new ArgumentException("newResourceId");
			}
			if (ownerFullName == null)
			{
				throw new ArgumentException("ownerFullName");
			}
			if (ownerId == null)
			{
				throw new ArgumentException("ownerId");
			}
			if (requestCharge == null)
			{
				throw new ArgumentException("requestCharge");
			}
			if (requestValidationFailure == null)
			{
				throw new ArgumentException("requestValidationFailure");
			}
			if (retryAfter == null)
			{
				throw new ArgumentException("retryAfter");
			}
			if (retryAfterInMilliseconds == null)
			{
				throw new ArgumentException("retryAfterInMilliseconds");
			}
			if (serverVersion == null)
			{
				throw new ArgumentException("serverVersion");
			}
			if (schemaVersion == null)
			{
				throw new ArgumentException("schemaVersion");
			}
			if (sessionToken == null)
			{
				throw new ArgumentException("sessionToken");
			}
			if (version == null)
			{
				throw new ArgumentException("version");
			}
			byte[] array = activityId.ToByteArray();
			byte[] array2 = localId.ToByteArray();
			fixed (byte* value = array)
			{
				fixed (byte* value2 = array2)
				{
					fixed (char* value3 = contentType)
					{
						fixed (char* value4 = contentEncoding)
						{
							fixed (char* value5 = contentLength)
							{
								fixed (char* value6 = contentLocation)
								{
									fixed (char* value7 = currentMediaStorageUsageInMB)
									{
										fixed (char* value8 = currentResourceQuotaUsage)
										{
											fixed (char* value9 = databaseAccountConsumedDocumentStorageInMB)
											{
												fixed (char* value10 = databaseAccountProvisionedDocumentStorageInMB)
												{
													fixed (char* value11 = databaseAccountReservedDocumentStorageInMB)
													{
														fixed (char* value12 = gatewayVersion)
														{
															fixed (char* value13 = indexingDirective)
															{
																fixed (char* value14 = itemCount)
																{
																	fixed (char* value15 = lastStateChangeUtc)
																	{
																		fixed (char* value16 = maxMediaStorageUsageInMB)
																		{
																			fixed (char* value17 = maxResourceQuota)
																			{
																				fixed (char* value18 = newResourceId)
																				{
																					fixed (char* value19 = ownerFullName)
																					{
																						fixed (char* value20 = ownerId)
																						{
																							fixed (char* value21 = requestCharge)
																							{
																								fixed (char* value22 = requestValidationFailure)
																								{
																									fixed (char* value23 = retryAfter)
																									{
																										fixed (char* value24 = retryAfterInMilliseconds)
																										{
																											fixed (char* value25 = serverVersion)
																											{
																												fixed (char* value26 = schemaVersion)
																												{
																													fixed (char* value27 = sessionToken)
																													{
																														fixed (char* value28 = version)
																														{
																															EventData* ptr = stackalloc EventData[30];
																															ptr->DataPointer = (IntPtr)(void*)value;
																															ptr->Size = array.Length;
																															ptr[1].DataPointer = (IntPtr)(void*)value2;
																															ptr[1].Size = array2.Length;
																															ptr[2].DataPointer = (IntPtr)(void*)(&statusCode);
																															ptr[2].Size = 2;
																															ptr[3].DataPointer = (IntPtr)(void*)(&milliseconds);
																															ptr[3].Size = 8;
																															ptr[4].DataPointer = (IntPtr)(void*)value3;
																															ptr[4].Size = (contentType.Length + 1) * 2;
																															ptr[5].DataPointer = (IntPtr)(void*)value4;
																															ptr[5].Size = (contentEncoding.Length + 1) * 2;
																															ptr[6].DataPointer = (IntPtr)(void*)value5;
																															ptr[6].Size = (contentLength.Length + 1) * 2;
																															ptr[7].DataPointer = (IntPtr)(void*)value6;
																															ptr[7].Size = (contentLocation.Length + 1) * 2;
																															ptr[8].DataPointer = (IntPtr)(void*)value7;
																															ptr[8].Size = (currentMediaStorageUsageInMB.Length + 1) * 2;
																															ptr[9].DataPointer = (IntPtr)(void*)value8;
																															ptr[9].Size = (currentResourceQuotaUsage.Length + 1) * 2;
																															ptr[10].DataPointer = (IntPtr)(void*)value9;
																															ptr[10].Size = (databaseAccountConsumedDocumentStorageInMB.Length + 1) * 2;
																															ptr[11].DataPointer = (IntPtr)(void*)value10;
																															ptr[11].Size = (databaseAccountProvisionedDocumentStorageInMB.Length + 1) * 2;
																															ptr[12].DataPointer = (IntPtr)(void*)value11;
																															ptr[12].Size = (databaseAccountReservedDocumentStorageInMB.Length + 1) * 2;
																															ptr[13].DataPointer = (IntPtr)(void*)value12;
																															ptr[13].Size = (gatewayVersion.Length + 1) * 2;
																															ptr[14].DataPointer = (IntPtr)(void*)value13;
																															ptr[14].Size = (indexingDirective.Length + 1) * 2;
																															ptr[15].DataPointer = (IntPtr)(void*)value14;
																															ptr[15].Size = (itemCount.Length + 1) * 2;
																															ptr[16].DataPointer = (IntPtr)(void*)value15;
																															ptr[16].Size = (lastStateChangeUtc.Length + 1) * 2;
																															ptr[17].DataPointer = (IntPtr)(void*)value16;
																															ptr[17].Size = (maxMediaStorageUsageInMB.Length + 1) * 2;
																															ptr[18].DataPointer = (IntPtr)(void*)value17;
																															ptr[18].Size = (maxResourceQuota.Length + 1) * 2;
																															ptr[19].DataPointer = (IntPtr)(void*)value18;
																															ptr[19].Size = (newResourceId.Length + 1) * 2;
																															ptr[20].DataPointer = (IntPtr)(void*)value19;
																															ptr[20].Size = (ownerFullName.Length + 1) * 2;
																															ptr[21].DataPointer = (IntPtr)(void*)value20;
																															ptr[21].Size = (ownerId.Length + 1) * 2;
																															ptr[22].DataPointer = (IntPtr)(void*)value21;
																															ptr[22].Size = (requestCharge.Length + 1) * 2;
																															ptr[23].DataPointer = (IntPtr)(void*)value22;
																															ptr[23].Size = (requestValidationFailure.Length + 1) * 2;
																															ptr[24].DataPointer = (IntPtr)(void*)value23;
																															ptr[24].Size = (retryAfter.Length + 1) * 2;
																															ptr[25].DataPointer = (IntPtr)(void*)value24;
																															ptr[25].Size = (retryAfterInMilliseconds.Length + 1) * 2;
																															ptr[26].DataPointer = (IntPtr)(void*)value25;
																															ptr[26].Size = (serverVersion.Length + 1) * 2;
																															ptr[27].DataPointer = (IntPtr)(void*)value26;
																															ptr[27].Size = (schemaVersion.Length + 1) * 2;
																															ptr[28].DataPointer = (IntPtr)(void*)value27;
																															ptr[28].Size = (sessionToken.Length + 1) * 2;
																															ptr[29].DataPointer = (IntPtr)(void*)value28;
																															ptr[29].Size = (version.Length + 1) * 2;
																															WriteEventCoreWithActivityId(activityId, 2, 30, ptr);
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[NonEvent]
		public virtual void Response(Guid activityId, Guid localId, short statusCode, double milliseconds, HttpResponseHeaders responseHeaders)
		{
			if (IsEnabled(EventLevel.Verbose, (EventKeywords)1L))
			{
				string[] keys = new string[26]
				{
					"Content-Type",
					"Content-Encoding",
					"Content-Length",
					"Content-Location",
					"x-ms-media-storage-usage-mb",
					"x-ms-resource-usage",
					"x-ms-databaseaccount-consumed-mb",
					"x-ms-databaseaccount-provisioned-mb",
					"x-ms-databaseaccount-reserved-mb",
					"x-ms-gatewayversion",
					"x-ms-indexing-directive",
					"x-ms-item-count",
					"x-ms-last-state-change-utc",
					"x-ms-max-media-storage-usage-mb",
					"x-ms-resource-quota",
					"x-ms-new-resource-id",
					"x-ms-alt-content-path",
					"x-ms-content-path",
					"x-ms-request-charge",
					"x-ms-request-validation-failure",
					"Retry-After",
					"x-ms-retry-after-ms",
					"x-ms-serviceversion",
					"x-ms-schemaversion",
					"x-ms-session-token",
					"x-ms-version"
				};
				string[] array = Helpers.ExtractValuesFromHTTPHeaders(responseHeaders, keys);
				Response(activityId, localId, statusCode, milliseconds, array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7], array[8], array[9], array[10], array[11], array[12], array[13], array[14], array[15], array[16], array[17], array[18], array[19], array[20], array[21], array[22], array[23], array[24], array[25]);
			}
		}
	}
}
