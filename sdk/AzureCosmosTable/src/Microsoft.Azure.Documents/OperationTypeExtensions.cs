using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal static class OperationTypeExtensions
	{
		private static readonly Dictionary<int, string> OperationTypeNames;

		static OperationTypeExtensions()
		{
			OperationTypeNames = new Dictionary<int, string>();
			foreach (OperationType value in Enum.GetValues(typeof(OperationType)))
			{
				OperationTypeNames[(int)value] = value.ToString();
			}
		}

		public static string ToOperationTypeString(this OperationType type)
		{
			return OperationTypeNames[(int)type];
		}

		public static bool IsWriteOperation(this OperationType type)
		{
			if (type != 0 && type != OperationType.Delete && type != OperationType.Replace && type != OperationType.ExecuteJavaScript && type != OperationType.BatchApply && type != OperationType.Batch && type != OperationType.Upsert && type != OperationType.Recreate && type != OperationType.GetSplitPoint && type != OperationType.AbortSplit && type != OperationType.CompleteSplit && type != OperationType.PreReplaceValidation && type != OperationType.ReportThroughputUtilization && type != OperationType.BatchReportThroughputUtilization && type != OperationType.OfferUpdateOperation && type != OperationType.CompletePartitionMigration && type != OperationType.AbortPartitionMigration && type != OperationType.MigratePartition && type != OperationType.ForceConfigRefresh && type != OperationType.MasterReplaceOfferOperation && type != OperationType.InitiateDatabaseOfferPartitionShrink)
			{
				return type == OperationType.CompleteDatabaseOfferPartitionShrink;
			}
			return true;
		}

		public static bool IsPointOperation(this OperationType type)
		{
			if (type != 0 && type != OperationType.Delete && type != OperationType.Read && type != OperationType.Patch && type != OperationType.Upsert)
			{
				return type == OperationType.Replace;
			}
			return true;
		}

		public static bool IsReadOperation(this OperationType type)
		{
			if (type != OperationType.Read && type != OperationType.ReadFeed && type != OperationType.Query && type != OperationType.SqlQuery && type != OperationType.Head && type != OperationType.HeadFeed)
			{
				return type == OperationType.QueryPlan;
			}
			return true;
		}

		/// <summary>
		/// Mapping the given operation type to the corresponding HTTP verb.
		/// </summary>
		/// <param name="operationType">The operation type.</param>
		/// <returns>The corresponding HTTP verb.</returns>
		public static string GetHttpMethod(this OperationType operationType)
		{
			switch (operationType)
			{
			case OperationType.ExecuteJavaScript:
			case OperationType.Create:
			case OperationType.BatchApply:
			case OperationType.SqlQuery:
			case OperationType.Query:
			case OperationType.Upsert:
			case OperationType.Batch:
			case OperationType.QueryPlan:
				return "POST";
			case OperationType.Delete:
				return "DELETE";
			case OperationType.Read:
			case OperationType.ReadFeed:
				return "GET";
			case OperationType.Replace:
				return "PUT";
			case OperationType.Patch:
				return "PATCH";
			case OperationType.Head:
			case OperationType.HeadFeed:
				return "HEAD";
			case OperationType.ForceConfigRefresh:
			case OperationType.Pause:
			case OperationType.Resume:
			case OperationType.Stop:
			case OperationType.Recycle:
			case OperationType.Crash:
			case OperationType.Recreate:
			case OperationType.Throttle:
			case OperationType.GetSplitPoint:
			case OperationType.PreCreateValidation:
			case OperationType.AbortSplit:
			case OperationType.CompleteSplit:
			case OperationType.CompletePartitionMigration:
			case OperationType.AbortPartitionMigration:
			case OperationType.OfferUpdateOperation:
			case OperationType.OfferPreGrowValidation:
			case OperationType.BatchReportThroughputUtilization:
			case OperationType.PreReplaceValidation:
			case OperationType.MigratePartition:
			case OperationType.MasterReplaceOfferOperation:
			case OperationType.InitiateDatabaseOfferPartitionShrink:
			case OperationType.CompleteDatabaseOfferPartitionShrink:
				return "POST";
			default:
				throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture, "Unsupported operation type: {0}.", operationType));
			}
		}
	}
}
