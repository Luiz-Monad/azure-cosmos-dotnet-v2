using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Helper class to generate and parse media id (only used in frontend)
	/// </summary>
	internal sealed class MediaIdHelper
	{
		public static string NewMediaId(string attachmentId, byte storageIndex)
		{
			if (storageIndex == 0)
			{
				return attachmentId;
			}
			ResourceId resourceId = ResourceId.Parse(attachmentId);
			byte[] array = new byte[ResourceId.Length + 1];
			resourceId.Value.CopyTo(array, 0);
			array[array.Length - 1] = storageIndex;
			return ResourceId.ToBase64String(array);
		}

		public static bool TryParseMediaId(string mediaId, out string attachmentId, out byte storageIndex)
		{
			storageIndex = 0;
			attachmentId = string.Empty;
			byte[] array = null;
			try
			{
				array = ResourceId.FromBase64String(mediaId);
			}
			catch (FormatException)
			{
				return false;
			}
			if (array.Length != ResourceId.Length && array.Length != ResourceId.Length + 1)
			{
				return false;
			}
			if (array.Length == ResourceId.Length)
			{
				storageIndex = 0;
				attachmentId = mediaId;
				return true;
			}
			storageIndex = array[array.Length - 1];
			attachmentId = ResourceId.ToBase64String(array, 0, ResourceId.Length);
			return true;
		}
	}
}
