using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Spatial index specification
	/// </summary>
	public sealed class SpatialSpec : JsonSerializable
	{
		private Collection<SpatialType> spatialTypes;

		/// <summary>
		/// Path in JSON document to index
		/// </summary>
		[JsonProperty(PropertyName = "path")]
		public string Path
		{
			get
			{
				return GetValue<string>("path");
			}
			set
			{
				SetValue("path", value);
			}
		}

		/// <summary>
		/// Path's spatial type
		/// </summary>
		[JsonProperty(PropertyName = "types", ItemConverterType = typeof(StringEnumConverter))]
		public Collection<SpatialType> SpatialTypes
		{
			get
			{
				if (spatialTypes == null)
				{
					spatialTypes = GetValue<Collection<SpatialType>>("types");
					if (spatialTypes == null)
					{
						spatialTypes = new Collection<SpatialType>();
					}
				}
				return spatialTypes;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, RMResources.PropertyCannotBeNull, "SpatialTypes"));
				}
				spatialTypes = value;
				SetValue("types", value);
			}
		}

		internal object Clone()
		{
			SpatialSpec spatialSpec = new SpatialSpec
			{
				Path = Path
			};
			foreach (SpatialType spatialType in SpatialTypes)
			{
				spatialSpec.SpatialTypes.Add(spatialType);
			}
			return spatialSpec;
		}

		internal override void OnSave()
		{
			if (spatialTypes != null)
			{
				SetValue("types", spatialTypes);
			}
		}
	}
}
