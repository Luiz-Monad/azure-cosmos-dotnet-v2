using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Query runtime execution times in the Azure Cosmos DB service.
	/// </summary>
	internal sealed class TextTable
	{
		internal struct Column
		{
			public readonly string ColumnName;

			public readonly int ColumnWidth;

			public Column(string columnName, int columnWidth)
			{
				ColumnName = columnName;
				ColumnWidth = columnWidth;
			}
		}

		private const char CellLeftTop = '┌';

		private const char CellRightTop = '┐';

		private const char CellLeftBottom = '└';

		private const char CellRightBottom = '┘';

		private const char CellHorizontalJointTop = '┬';

		private const char CellHorizontalJointBottom = '┴';

		private const char CellVerticalJointLeft = '├';

		private const char CellTJoint = '┼';

		private const char CellVerticalJointRight = '┤';

		private const char CellHorizontalLine = '─';

		private const char CellVerticalLine = '│';

		private const int PaddingLength = 3;

		private readonly List<Column> columns;

		private readonly string header;

		private readonly string topLine;

		private readonly string middleLine;

		private readonly string bottomLine;

		private readonly string rowFormatString;

		public string Header => header;

		public string TopLine => topLine;

		public string MiddleLine => middleLine;

		public string BottomLine => bottomLine;

		/// <summary>
		/// Initializes a new instance of the TextTable class.
		/// </summary>
		/// <param name="columns">The columns of the table.</param>
		public TextTable(params Column[] columns)
		{
			this.columns = new List<Column>(columns);
			string text = BuildLineFormatString("{{{0},-{1}}}", columns);
			string format = text;
			object[] args = (from textTableColumn in columns
			select textTableColumn.ColumnName).ToArray();
			header = string.Format(format, args);
			topLine = BuildLine('┌', '┐', '┬', columns);
			middleLine = BuildLine('├', '┤', '┼', columns);
			bottomLine = BuildLine('└', '┘', '┴', columns);
			rowFormatString = BuildLineFormatString("{{{0},{1}}}", columns);
		}

		public string GetRow(params object[] cells)
		{
			if (cells.Length != columns.Count)
			{
				throw new ArgumentException("Cells in a row needs to have exactly 1 element per column");
			}
			return string.Format(rowFormatString, cells);
		}

		private static string BuildLine(char firstChar, char lastChar, char seperator, IEnumerable<Column> columns)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(firstChar);
			foreach (Column item in columns.Take(columns.Count() - 1))
			{
				stringBuilder.Append('─', item.ColumnWidth);
				stringBuilder.Append(seperator);
			}
			stringBuilder.Append('─', columns.Last().ColumnWidth);
			stringBuilder.Append(lastChar);
			return stringBuilder.ToString();
		}

		private static string BuildLineFormatString(string cellFormatString, IEnumerable<Column> columns)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('│');
			int num = 0;
			foreach (Column column in columns)
			{
				stringBuilder.Append(string.Format(cellFormatString, num++, column.ColumnWidth));
				stringBuilder.Append('│');
			}
			return stringBuilder.ToString();
		}
	}
}
