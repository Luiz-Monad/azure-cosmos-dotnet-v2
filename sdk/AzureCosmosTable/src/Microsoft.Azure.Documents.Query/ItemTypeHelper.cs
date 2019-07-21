using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// Utility class for item types.
	/// </summary>
	internal static class ItemTypeHelper
	{
		/// <summary>
		/// Gets an ItemType based on the provided value.
		/// </summary>
		/// <param name="value">The value of the item.</param>
		/// <returns>The ItemType of the value.</returns>
		public static ItemType GetItemType(object value)
		{
			if (value is Undefined)
			{
				return ItemType.NoValue;
			}
			if (value == null)
			{
				return ItemType.Null;
			}
			if (value is bool)
			{
				return ItemType.Bool;
			}
			if (value is string)
			{
				return ItemType.String;
			}
			if (IsNumeric(value))
			{
				return ItemType.Number;
			}
			throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unrecognized type {0}", value.GetType().ToString()));
		}

		/// <summary>
		/// Determines if an object is a primitive value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>Whether or not the item type is primitive.</returns>
		public static bool IsPrimitive(object value)
		{
			if (value != null && !(value is bool) && !(value is string))
			{
				return IsNumeric(value);
			}
			return true;
		}

		/// <summary>
		/// Gets whether an object is a numeric type.
		/// </summary>
		/// <param name="value">The value to examine.</param>
		/// <returns>Whether it is a numeric type.</returns>
		public static bool IsNumeric(object value)
		{
			if (!(value is sbyte) && !(value is byte) && !(value is short) && !(value is ushort) && !(value is int) && !(value is uint) && !(value is long) && !(value is ulong) && !(value is float) && !(value is double))
			{
				return value is decimal;
			}
			return true;
		}
	}
}
