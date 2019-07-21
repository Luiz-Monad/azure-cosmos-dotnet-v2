using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents the indexing policy configuration for a collection.
	/// </summary> 
	/// <remarks>
	/// Indexing policies can used to configure which properties (JSON paths) are included/excluded, whether the index is updated consistently
	/// or offline (lazy), automatic vs. opt-in per-document, as well as the precision and type of index per path.
	///
	/// Refer to http://azure.microsoft.com/documentation/articles/documentdb-indexing-policies/ for additional information on how to specify
	/// indexing policies.
	/// </remarks>
	/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
	internal sealed class IndexingPolicyOld : JsonSerializable, ICloneable
	{
		/// <summary>
		/// Gets or sets the path level configurations for indexing.
		/// </summary>
		private Collection<IndexingPath> included;

		private IList<string> excluded;

		/// <summary>
		/// Gets or sets a value that indicates whether automatic indexing is enabled for a collection.
		/// </summary>
		/// <remarks>
		/// In automatic indexing, documents can be explicitly excluded from indexing using <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />.  
		/// In manual indexing, documents can be explicitly included.
		/// </remarks>
		public bool Automatic
		{
			get
			{
				return GetValue<bool>("automatic");
			}
			set
			{
				SetValue("automatic", value);
			}
		}

		/// <summary>
		/// Gets or sets the indexing mode (consistent or lazy).
		/// </summary>
		/// <value>
		/// One of the values of the <see cref="T:Microsoft.Azure.Documents.IndexingMode" /> enumeration.
		/// </value>
		[JsonConverter(typeof(StringEnumConverter))]
		public IndexingMode IndexingMode
		{
			get
			{
				IndexingMode result = IndexingMode.Lazy;
				string value = GetValue<string>("indexingMode");
				if (!string.IsNullOrEmpty(value))
				{
					Enum.TryParse(value, ignoreCase: true, out result);
				}
				return result;
			}
			set
			{
				SetValue("indexingMode", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the list containing included paths.
		/// </summary>
		/// <value>
		/// The list containing included paths.
		/// </value>
		public Collection<IndexingPath> IncludedPaths
		{
			get
			{
				if (included == null)
				{
					included = GetValue<Collection<IndexingPath>>("includedPaths");
					if (included == null)
					{
						included = new Collection<IndexingPath>();
					}
				}
				return included;
			}
		}

		/// <summary>
		/// Gets or sets the list containing excluded paths.
		/// </summary>
		/// <value>
		/// The list containing excluded paths.
		/// </value>
		public IList<string> ExcludedPaths
		{
			get
			{
				if (excluded == null)
				{
					excluded = GetValue<IList<string>>("excludedPaths");
					if (excluded == null)
					{
						excluded = new List<string>();
					}
				}
				return excluded;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.IndexingPolicyOld" /> class.
		/// </summary>
		/// <remarks>
		/// Indexing mode is set to consistent.
		/// </remarks>
		public IndexingPolicyOld()
		{
			Automatic = true;
			IndexingMode = IndexingMode.Consistent;
		}

		internal override void OnSave()
		{
			if (included == null || included.Count != 0 || excluded == null || excluded.Count != 0)
			{
				if (included != null)
				{
					SetObjectCollection("includedPaths", included);
				}
				if (excluded != null)
				{
					SetValue("excludedPaths", excluded);
				}
			}
		}

		/// <summary>
		/// Performs a deep copy of the indexing policy.
		/// </summary>
		/// <returns>
		/// A clone of the indexing policy.
		/// </returns>
		public object Clone()
		{
			IndexingPolicyOld indexingPolicyOld = new IndexingPolicyOld
			{
				Automatic = Automatic,
				IndexingMode = IndexingMode
			};
			foreach (IndexingPath includedPath in IncludedPaths)
			{
				indexingPolicyOld.IncludedPaths.Add((IndexingPath)includedPath.Clone());
			}
			foreach (string excludedPath in ExcludedPaths)
			{
				indexingPolicyOld.ExcludedPaths.Add(excludedPath);
			}
			return indexingPolicyOld;
		}
	}
}
