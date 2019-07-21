using System;
using System.Collections.Specialized;

namespace Microsoft.Azure.Documents.Collections
{
	/// <summary>
	///
	/// </summary>
	internal interface INameValueCollectionFactory
	{
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		INameValueCollection CreateNewNameValueCollection();

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		INameValueCollection CreateNewNameValueCollection(int capacity);

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		INameValueCollection CreateNewNameValueCollection(StringComparer comparer);

		/// <summary>
		///
		/// </summary>
		/// <param name="collection"></param>
		/// <returns></returns>
		INameValueCollection CreateNewNameValueCollection(NameValueCollection collection);

		/// <summary>
		///
		/// </summary>
		/// <param name="collection"></param>
		/// <returns></returns>
		INameValueCollection CreateNewNameValueCollection(INameValueCollection collection);
	}
}
