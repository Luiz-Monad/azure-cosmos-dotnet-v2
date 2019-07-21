using System;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Documents
{
	internal static class IndexingPolicyTranslator
	{
		public static IndexingPolicy TranslateIndexingPolicyV1ToV2(IndexingPolicyOld indexingPolicyOld)
		{
			if (indexingPolicyOld == null)
			{
				throw new ArgumentNullException("indexingPolicyOld");
			}
			IndexingPolicy indexingPolicy = new IndexingPolicy();
			indexingPolicy.Automatic = indexingPolicyOld.Automatic;
			indexingPolicy.IndexingMode = indexingPolicyOld.IndexingMode;
			foreach (IndexingPath includedPath2 in indexingPolicyOld.IncludedPaths)
			{
				IncludedPath includedPath = new IncludedPath
				{
					Path = includedPath2.Path,
					Indexes = new Collection<Index>()
				};
				if (includedPath2.IndexType == IndexType.Hash)
				{
					includedPath.Indexes.Add(new HashIndex(DataType.Number)
					{
						Precision = (short?)includedPath2.NumericPrecision
					});
					includedPath.Indexes.Add(new HashIndex(DataType.String)
					{
						Precision = (short?)includedPath2.StringPrecision
					});
				}
				else if (includedPath2.IndexType == IndexType.Range)
				{
					includedPath.Indexes.Add(new RangeIndex(DataType.Number)
					{
						Precision = (short?)includedPath2.NumericPrecision
					});
					includedPath.Indexes.Add(new HashIndex(DataType.String)
					{
						Precision = (short?)includedPath2.StringPrecision
					});
				}
				indexingPolicy.IncludedPaths.Add(includedPath);
			}
			foreach (string excludedPath in indexingPolicyOld.ExcludedPaths)
			{
				ExcludedPath item = new ExcludedPath
				{
					Path = excludedPath
				};
				indexingPolicy.ExcludedPaths.Add(item);
			}
			return indexingPolicy;
		}
	}
}
