using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Documents
{
	internal sealed class IndexingPolicyBuilder
	{
		public sealed class CompositePath
		{
			public sealed class CompositePathEqualityComparer : IEqualityComparer<CompositePath>
			{
				public static readonly CompositePathEqualityComparer Singleton = new CompositePathEqualityComparer();

				public bool Equals(CompositePath compositePath1, CompositePath compositePath2)
				{
					if (compositePath1 == compositePath2)
					{
						return true;
					}
					if (compositePath1 == null || compositePath2 == null)
					{
						return false;
					}
					if (compositePath1.Path == compositePath2.Path && compositePath2.CompositePathSortOrder == compositePath2.CompositePathSortOrder)
					{
						return true;
					}
					return false;
				}

				public int GetHashCode(CompositePath compositePath)
				{
					if (compositePath == null)
					{
						return 0;
					}
					return compositePath.Path.GetHashCode() ^ compositePath.CompositePathSortOrder.GetHashCode();
				}
			}

			[JsonProperty(PropertyName = "path")]
			public string Path
			{
				get;
			}

			[JsonProperty(PropertyName = "order")]
			[JsonConverter(typeof(StringEnumConverter))]
			public CompositePathSortOrder CompositePathSortOrder
			{
				get;
			}

			public CompositePath(string path, CompositePathSortOrder compositePathSortOrder = CompositePathSortOrder.Ascending)
			{
				if (path == null)
				{
					throw new ArgumentNullException(string.Format("{0} must not be null.", "path"));
				}
				Path = path;
				CompositePathSortOrder = compositePathSortOrder;
			}
		}

		private sealed class CompositePathsEqualityComparer : IEqualityComparer<HashSet<CompositePath>>
		{
			public static readonly CompositePathsEqualityComparer Singleton = new CompositePathsEqualityComparer();

			private static readonly CompositePath.CompositePathEqualityComparer compositePathEqualityComparer = new CompositePath.CompositePathEqualityComparer();

			public bool Equals(HashSet<CompositePath> compositePaths1, HashSet<CompositePath> compositePaths2)
			{
				if (compositePaths1 == compositePaths2)
				{
					return true;
				}
				if (compositePaths1 == null || compositePaths2 == null)
				{
					return false;
				}
				return compositePaths1.SetEquals(compositePaths2);
			}

			public int GetHashCode(HashSet<CompositePath> obj)
			{
				if (obj == null)
				{
					return 0;
				}
				int num = 0;
				foreach (CompositePath item in obj)
				{
					num ^= compositePathEqualityComparer.GetHashCode(item);
				}
				return num;
			}
		}

		public sealed class SpatialIndex
		{
			public sealed class SpatialIndexEqualityComparer : IEqualityComparer<SpatialIndex>
			{
				public static readonly SpatialIndexEqualityComparer Singleton = new SpatialIndexEqualityComparer();

				public bool Equals(SpatialIndex x, SpatialIndex y)
				{
					if (x == y)
					{
						return true;
					}
					if (x == null || y == null)
					{
						return false;
					}
					return (byte)(1 & (x.Path.Equals(y.Path) ? 1 : 0) & (x.SpatialTypes.SetEquals(y.SpatialTypes) ? 1 : 0)) != 0;
				}

				public int GetHashCode(SpatialIndex obj)
				{
					int num = 0;
					num ^= obj.Path.GetHashCode();
					foreach (SpatialType spatialType in obj.SpatialTypes)
					{
						SpatialType spatialType2 = spatialType;
						num ^= obj.Path.GetHashCode();
					}
					return num;
				}
			}

			[JsonProperty(PropertyName = "path")]
			public string Path
			{
				get;
			}

			[JsonProperty(PropertyName = "types", ItemConverterType = typeof(StringEnumConverter))]
			public HashSet<SpatialType> SpatialTypes
			{
				get;
			}

			public SpatialIndex(string path, params SpatialType[] spatialTypes)
			{
				if (path == null)
				{
					throw new ArgumentNullException(string.Format("{0} must not be null.", "path"));
				}
				Path = path;
				SpatialTypes = new HashSet<SpatialType>();
				foreach (SpatialType item in spatialTypes)
				{
					SpatialTypes.Add(item);
				}
			}
		}

		[JsonProperty(PropertyName = "includedPaths")]
		private readonly HashSet<IncludedPath> includedPaths;

		[JsonProperty(PropertyName = "excludedPaths")]
		private readonly HashSet<ExcludedPath> excludedPaths;

		[JsonProperty(PropertyName = "compositeIndexes")]
		private readonly HashSet<HashSet<CompositePath>> compositeIndexes;

		[JsonProperty(PropertyName = "spatialIndexes")]
		private readonly HashSet<SpatialIndex> spatialIndexes;

		private static readonly Collection<Index> DefaultIndexes = new Collection<Index>
		{
			Index.Range(DataType.String, -1),
			Index.Range(DataType.Number, -1)
		};

		[JsonProperty(PropertyName = "automatic")]
		public bool Automatic
		{
			get;
			set;
		}

		[JsonProperty(PropertyName = "indexingMode")]
		[JsonConverter(typeof(StringEnumConverter))]
		public IndexingMode IndexingMode
		{
			get;
			set;
		}

		public IndexingPolicyBuilder()
		{
			includedPaths = new HashSet<IncludedPath>(IndexingPolicy.IncludedPathEqualityComparer.Singleton);
			excludedPaths = new HashSet<ExcludedPath>(IndexingPolicy.ExcludedPathEqualityComparer.Singleton);
			compositeIndexes = new HashSet<HashSet<CompositePath>>();
			spatialIndexes = new HashSet<SpatialIndex>(SpatialIndex.SpatialIndexEqualityComparer.Singleton);
			Automatic = true;
			IndexingMode = IndexingMode.Consistent;
		}

		public void AddIncludedPath(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "path"));
			}
			IncludedPath item = new IncludedPath
			{
				Path = path,
				Indexes = DefaultIndexes
			};
			includedPaths.Add(item);
		}

		public void AddExcludedPath(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "path"));
			}
			ExcludedPath item = new ExcludedPath
			{
				Path = path
			};
			excludedPaths.Add(item);
		}

		public void AddCompositeIndex(params CompositePath[] compositePaths)
		{
			if (compositePaths == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "compositePaths"));
			}
			HashSet<CompositePath> hashSet = new HashSet<CompositePath>(CompositePath.CompositePathEqualityComparer.Singleton);
			foreach (CompositePath compositePath in compositePaths)
			{
				if (compositePath == null)
				{
					throw new ArgumentException(string.Format("{0} must not have null elements.", "compositePaths"));
				}
				hashSet.Add(compositePath);
			}
			compositeIndexes.Add(hashSet);
		}

		public void AddSpatialIndex(SpatialIndex spatialIndex)
		{
			if (spatialIndex == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "spatialIndex"));
			}
			spatialIndexes.Add(spatialIndex);
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}
}
