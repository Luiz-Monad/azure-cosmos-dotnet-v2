using Microsoft.Azure.Documents.Collections;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Documents
{
	internal sealed class HttpUtility
	{
		private class UrlDecoder
		{
			private int _bufferSize;

			private byte[] _byteBuffer;

			private char[] _charBuffer;

			private Encoding _encoding;

			private int _numBytes;

			private int _numChars;

			internal UrlDecoder(int bufferSize, Encoding encoding)
			{
				_bufferSize = bufferSize;
				_encoding = encoding;
				_charBuffer = new char[bufferSize];
			}

			internal void AddByte(byte b)
			{
				if (_byteBuffer == null)
				{
					_byteBuffer = new byte[_bufferSize];
				}
				_byteBuffer[_numBytes++] = b;
			}

			internal void AddChar(char ch)
			{
				if (_numBytes > 0)
				{
					FlushBytes();
				}
				_charBuffer[_numChars++] = ch;
			}

			private void FlushBytes()
			{
				if (_numBytes > 0)
				{
					_numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
					_numBytes = 0;
				}
			}

			internal string GetString()
			{
				if (_numBytes > 0)
				{
					FlushBytes();
				}
				if (_numChars > 0)
				{
					return new string(_charBuffer, 0, _numChars);
				}
				return string.Empty;
			}
		}

		private static char[] s_entityEndingChars = new char[2]
		{
			';',
			'&'
		};

		internal static string AspCompatUrlEncode(string s)
		{
			s = UrlEncode(s);
			s = s.Replace("!", "%21");
			s = s.Replace("*", "%2A");
			s = s.Replace("(", "%28");
			s = s.Replace(")", "%29");
			s = s.Replace("-", "%2D");
			s = s.Replace(".", "%2E");
			s = s.Replace("_", "%5F");
			s = s.Replace("\\", "%5C");
			return s;
		}

		internal static string CollapsePercentUFromStringInternal(string s, Encoding e)
		{
			int length = s.Length;
			UrlDecoder urlDecoder = new UrlDecoder(length, e);
			if (s.IndexOf("%u", StringComparison.Ordinal) == -1)
			{
				return s;
			}
			for (int i = 0; i < length; i++)
			{
				char c = s[i];
				if (c == '%' && i < length - 5 && s[i + 1] == 'u')
				{
					int num = HexToInt(s[i + 2]);
					int num2 = HexToInt(s[i + 3]);
					int num3 = HexToInt(s[i + 4]);
					int num4 = HexToInt(s[i + 5]);
					if (num >= 0 && num2 >= 0 && num3 >= 0 && num4 >= 0)
					{
						c = (char)((num << 12) | (num2 << 8) | (num3 << 4) | num4);
						i += 5;
						urlDecoder.AddChar(c);
						continue;
					}
				}
				if ((c & 0xFF80) == 0)
				{
					urlDecoder.AddByte((byte)c);
				}
				else
				{
					urlDecoder.AddChar(c);
				}
			}
			return urlDecoder.GetString();
		}

		internal static string FormatHttpCookieDateTime(DateTime dt)
		{
			if (dt < DateTime.MaxValue.AddDays(-1.0) && dt > DateTime.MinValue.AddDays(1.0))
			{
				dt = dt.ToUniversalTime();
			}
			return dt.ToString("ddd, dd-MMM-yyyy HH':'mm':'ss 'GMT'", DateTimeFormatInfo.InvariantInfo);
		}

		internal static string FormatHttpDateTime(DateTime dt)
		{
			if (dt < DateTime.MaxValue.AddDays(-1.0) && dt > DateTime.MinValue.AddDays(1.0))
			{
				dt = dt.ToUniversalTime();
			}
			return dt.ToString("R", DateTimeFormatInfo.InvariantInfo);
		}

		internal static string FormatHttpDateTimeUtc(DateTime dt)
		{
			return dt.ToString("R", DateTimeFormatInfo.InvariantInfo);
		}

		internal static string FormatPlainTextAsHtml(string s)
		{
			if (s == null)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			using (StringWriter output = new StringWriter(stringBuilder, CultureInfo.InvariantCulture))
			{
				FormatPlainTextAsHtml(s, output);
			}
			return stringBuilder.ToString();
		}

		internal static INameValueCollection ParseQueryString(string queryString)
		{
			INameValueCollection nameValueCollection = new StringKeyValueCollection();
			string[] array = queryString.Split(new char[1]
			{
				'&'
			});
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(new char[1]
				{
					'='
				});
				if (array2.Length > 1)
				{
					string str = UrlDecode(array2[0].Trim('?', ' '));
					string value = UrlDecode(array2[1].Trim());
					nameValueCollection.Add(CultureInfo.InvariantCulture.TextInfo.ToLower(str), value);
				}
			}
			return nameValueCollection;
		}

		internal static void FormatPlainTextAsHtml(string s, TextWriter output)
		{
			if (s == null)
			{
				return;
			}
			int length = s.Length;
			char c = '\0';
			for (int i = 0; i < length; i++)
			{
				char c2 = s[i];
				switch (c2)
				{
				case '\n':
					output.Write("<br>");
					break;
				case ' ':
					if (c == ' ')
					{
						output.Write("&nbsp;");
					}
					else
					{
						output.Write(c2);
					}
					break;
				case '"':
					output.Write("&quot;");
					break;
				case '&':
					output.Write("&amp;");
					break;
				case '<':
					output.Write("&lt;");
					break;
				case '>':
					output.Write("&gt;");
					break;
				default:
					if (c2 >= '\u00a0' && c2 < 'Ā')
					{
						output.Write("&#");
						int num = c2;
						output.Write(num.ToString(NumberFormatInfo.InvariantInfo));
						output.Write(';');
					}
					else
					{
						output.Write(c2);
					}
					break;
				case '\r':
					break;
				}
				c = c2;
			}
		}

		internal static string FormatPlainTextSpacesAsHtml(string s)
		{
			if (s == null)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			using (StringWriter stringWriter = new StringWriter(stringBuilder, CultureInfo.InvariantCulture))
			{
				int length = s.Length;
				for (int i = 0; i < length; i++)
				{
					char c = s[i];
					if (c == ' ')
					{
						stringWriter.Write("&nbsp;");
					}
					else
					{
						stringWriter.Write(c);
					}
				}
			}
			return stringBuilder.ToString();
		}

		private static int HexToInt(char h)
		{
			if (h >= '0' && h <= '9')
			{
				return h - 48;
			}
			if (h >= 'a' && h <= 'f')
			{
				return h - 97 + 10;
			}
			if (h >= 'A' && h <= 'F')
			{
				return h - 65 + 10;
			}
			return -1;
		}

		internal static char IntToHex(int n)
		{
			if (n <= 9)
			{
				return (char)(n + 48);
			}
			return (char)(n - 10 + 97);
		}

		private static bool IsNonAsciiByte(byte b)
		{
			if (b < 127)
			{
				return b < 32;
			}
			return true;
		}

		internal static bool IsSafe(char ch)
		{
			if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
			{
				return true;
			}
			switch (ch)
			{
			case '!':
			case '\'':
			case '(':
			case ')':
			case '*':
			case '-':
			case '.':
			case '_':
				return true;
			default:
				return false;
			}
		}

		public static string UrlDecode(string str)
		{
			if (str == null)
			{
				return null;
			}
			return UrlDecode(str, Encoding.UTF8);
		}

		public static string UrlDecode(byte[] bytes, Encoding e)
		{
			if (bytes == null)
			{
				return null;
			}
			return UrlDecode(bytes, 0, bytes.Length, e);
		}

		public static string UrlDecode(string str, Encoding e)
		{
			if (str == null)
			{
				return null;
			}
			return UrlDecodeStringFromStringInternal(str, e);
		}

		public static string UrlDecode(byte[] bytes, int offset, int count, Encoding e)
		{
			if (bytes == null && count == 0)
			{
				return null;
			}
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			if (offset < 0 || offset > bytes.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || offset + count > bytes.Length)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			return UrlDecodeStringFromBytesInternal(bytes, offset, count, e);
		}

		private static byte[] UrlDecodeBytesFromBytesInternal(byte[] buf, int offset, int count)
		{
			int num = 0;
			byte[] array = new byte[count];
			for (int i = 0; i < count; i++)
			{
				int num2 = offset + i;
				byte b = buf[num2];
				switch (b)
				{
				case 43:
					b = 32;
					break;
				case 37:
					if (i < count - 2)
					{
						int num3 = HexToInt((char)buf[num2 + 1]);
						int num4 = HexToInt((char)buf[num2 + 2]);
						if (num3 >= 0 && num4 >= 0)
						{
							b = (byte)((num3 << 4) | num4);
							i += 2;
						}
					}
					break;
				}
				array[num++] = b;
			}
			if (num < array.Length)
			{
				byte[] array2 = new byte[num];
				Array.Copy(array, array2, num);
				array = array2;
			}
			return array;
		}

		private static string UrlDecodeStringFromBytesInternal(byte[] buf, int offset, int count, Encoding e)
		{
			UrlDecoder urlDecoder = new UrlDecoder(count, e);
			for (int i = 0; i < count; i++)
			{
				int num = offset + i;
				byte b = buf[num];
				switch (b)
				{
				case 43:
					b = 32;
					break;
				case 37:
					if (i >= count - 2)
					{
						break;
					}
					if (buf[num + 1] == 117 && i < count - 5)
					{
						int num2 = HexToInt((char)buf[num + 2]);
						int num3 = HexToInt((char)buf[num + 3]);
						int num4 = HexToInt((char)buf[num + 4]);
						int num5 = HexToInt((char)buf[num + 5]);
						if (num2 >= 0 && num3 >= 0 && num4 >= 0 && num5 >= 0)
						{
							char ch = (char)((num2 << 12) | (num3 << 8) | (num4 << 4) | num5);
							i += 5;
							urlDecoder.AddChar(ch);
							continue;
						}
					}
					else
					{
						int num6 = HexToInt((char)buf[num + 1]);
						int num7 = HexToInt((char)buf[num + 2]);
						if (num6 >= 0 && num7 >= 0)
						{
							b = (byte)((num6 << 4) | num7);
							i += 2;
						}
					}
					break;
				}
				urlDecoder.AddByte(b);
			}
			return urlDecoder.GetString();
		}

		private static string UrlDecodeStringFromStringInternal(string s, Encoding e)
		{
			int length = s.Length;
			UrlDecoder urlDecoder = new UrlDecoder(length, e);
			for (int i = 0; i < length; i++)
			{
				char c = s[i];
				switch (c)
				{
				case '+':
					c = ' ';
					break;
				case '%':
					if (i >= length - 2)
					{
						break;
					}
					if (s[i + 1] == 'u' && i < length - 5)
					{
						int num = HexToInt(s[i + 2]);
						int num2 = HexToInt(s[i + 3]);
						int num3 = HexToInt(s[i + 4]);
						int num4 = HexToInt(s[i + 5]);
						if (num >= 0 && num2 >= 0 && num3 >= 0 && num4 >= 0)
						{
							c = (char)((num << 12) | (num2 << 8) | (num3 << 4) | num4);
							i += 5;
							urlDecoder.AddChar(c);
							continue;
						}
					}
					else
					{
						int num5 = HexToInt(s[i + 1]);
						int num6 = HexToInt(s[i + 2]);
						if (num5 >= 0 && num6 >= 0)
						{
							byte b = (byte)((num5 << 4) | num6);
							i += 2;
							urlDecoder.AddByte(b);
							continue;
						}
					}
					break;
				}
				if ((c & 0xFF80) == 0)
				{
					urlDecoder.AddByte((byte)c);
				}
				else
				{
					urlDecoder.AddChar(c);
				}
			}
			return urlDecoder.GetString();
		}

		public static byte[] UrlDecodeToBytes(byte[] bytes)
		{
			if (bytes == null)
			{
				return null;
			}
			return UrlDecodeToBytes(bytes, 0, (bytes != null) ? bytes.Length : 0);
		}

		public static byte[] UrlDecodeToBytes(string str)
		{
			if (str == null)
			{
				return null;
			}
			return UrlDecodeToBytes(str, Encoding.UTF8);
		}

		public static byte[] UrlDecodeToBytes(string str, Encoding e)
		{
			if (str == null)
			{
				return null;
			}
			return UrlDecodeToBytes(e.GetBytes(str));
		}

		public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count)
		{
			if (bytes == null && count == 0)
			{
				return null;
			}
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			if (offset < 0 || offset > bytes.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || offset + count > bytes.Length)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			return UrlDecodeBytesFromBytesInternal(bytes, offset, count);
		}

		public static string UrlEncode(byte[] bytes)
		{
			if (bytes == null)
			{
				return null;
			}
			byte[] array = UrlEncodeToBytes(bytes);
			return Encoding.UTF8.GetString(array, 0, array.Length);
		}

		public static string UrlEncode(string str)
		{
			if (str == null)
			{
				return null;
			}
			return UrlEncode(str, Encoding.UTF8);
		}

		public static string UrlEncode(string str, Encoding e)
		{
			if (str == null)
			{
				return null;
			}
			byte[] array = UrlEncodeToBytes(str, e);
			return Encoding.UTF8.GetString(array, 0, array.Length);
		}

		public static string UrlEncode(byte[] bytes, int offset, int count)
		{
			if (bytes == null)
			{
				return null;
			}
			byte[] array = UrlEncodeToBytes(bytes, offset, count);
			return Encoding.UTF8.GetString(array, 0, array.Length);
		}

		private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue)
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < count; i++)
			{
				char c = (char)bytes[offset + i];
				if (c == ' ')
				{
					num++;
				}
				else if (!IsSafe(c))
				{
					num2++;
				}
			}
			if (!alwaysCreateReturnValue && num == 0 && num2 == 0)
			{
				return bytes;
			}
			byte[] array = new byte[count + num2 * 2];
			int num3 = 0;
			for (int j = 0; j < count; j++)
			{
				byte b = bytes[offset + j];
				char c2 = (char)b;
				if (IsSafe(c2))
				{
					array[num3++] = b;
					continue;
				}
				if (c2 == ' ')
				{
					array[num3++] = 43;
					continue;
				}
				array[num3++] = 37;
				array[num3++] = (byte)IntToHex((b >> 4) & 0xF);
				array[num3++] = (byte)IntToHex(b & 0xF);
			}
			return array;
		}

		private static byte[] UrlEncodeBytesToBytesInternalNonAscii(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue)
		{
			int num = 0;
			for (int i = 0; i < count; i++)
			{
				if (IsNonAsciiByte(bytes[offset + i]))
				{
					num++;
				}
			}
			if (!alwaysCreateReturnValue && num == 0)
			{
				return bytes;
			}
			byte[] array = new byte[count + num * 2];
			int num2 = 0;
			for (int j = 0; j < count; j++)
			{
				byte b = bytes[offset + j];
				if (IsNonAsciiByte(b))
				{
					array[num2++] = 37;
					array[num2++] = (byte)IntToHex((b >> 4) & 0xF);
					array[num2++] = (byte)IntToHex(b & 0xF);
				}
				else
				{
					array[num2++] = b;
				}
			}
			return array;
		}

		internal static string UrlEncodeNonAscii(string str, Encoding e)
		{
			if (string.IsNullOrEmpty(str))
			{
				return str;
			}
			if (e == null)
			{
				e = Encoding.UTF8;
			}
			byte[] bytes = e.GetBytes(str);
			bytes = UrlEncodeBytesToBytesInternalNonAscii(bytes, 0, bytes.Length, alwaysCreateReturnValue: false);
			return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
		}

		internal static string UrlEncodeSpaces(string str)
		{
			if (str != null && str.IndexOf(' ') >= 0)
			{
				str = str.Replace(" ", "%20");
			}
			return str;
		}

		public static byte[] UrlEncodeToBytes(string str)
		{
			if (str == null)
			{
				return null;
			}
			return UrlEncodeToBytes(str, Encoding.UTF8);
		}

		public static byte[] UrlEncodeToBytes(byte[] bytes)
		{
			if (bytes == null)
			{
				return null;
			}
			return UrlEncodeToBytes(bytes, 0, bytes.Length);
		}

		public static byte[] UrlEncodeToBytes(string str, Encoding e)
		{
			if (str == null)
			{
				return null;
			}
			byte[] bytes = e.GetBytes(str);
			return UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, alwaysCreateReturnValue: false);
		}

		public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
		{
			if (bytes == null && count == 0)
			{
				return null;
			}
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			if (offset < 0 || offset > bytes.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || offset + count > bytes.Length)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			return UrlEncodeBytesToBytesInternal(bytes, offset, count, alwaysCreateReturnValue: true);
		}

		public static string UrlEncodeUnicode(string str)
		{
			if (str == null)
			{
				return null;
			}
			return UrlEncodeUnicodeStringToStringInternal(str, ignoreAscii: false);
		}

		private static string UrlEncodeUnicodeStringToStringInternal(string s, bool ignoreAscii)
		{
			int length = s.Length;
			StringBuilder stringBuilder = new StringBuilder(length);
			for (int i = 0; i < length; i++)
			{
				char c = s[i];
				if ((c & 0xFF80) == 0)
				{
					if (ignoreAscii || IsSafe(c))
					{
						stringBuilder.Append(c);
						continue;
					}
					if (c == ' ')
					{
						stringBuilder.Append('+');
						continue;
					}
					stringBuilder.Append('%');
					stringBuilder.Append(IntToHex(((int)c >> 4) & 0xF));
					stringBuilder.Append(IntToHex(c & 0xF));
				}
				else
				{
					stringBuilder.Append("%u");
					stringBuilder.Append(IntToHex(((int)c >> 12) & 0xF));
					stringBuilder.Append(IntToHex(((int)c >> 8) & 0xF));
					stringBuilder.Append(IntToHex(((int)c >> 4) & 0xF));
					stringBuilder.Append(IntToHex(c & 0xF));
				}
			}
			return stringBuilder.ToString();
		}

		public static byte[] UrlEncodeUnicodeToBytes(string str)
		{
			if (str == null)
			{
				return null;
			}
			return Encoding.UTF8.GetBytes(UrlEncodeUnicode(str));
		}

		public static string UrlPathEncode(string str)
		{
			if (str == null)
			{
				return null;
			}
			int num = str.IndexOf('?');
			if (num >= 0)
			{
				return UrlPathEncode(str.Substring(0, num)) + str.Substring(num);
			}
			return UrlEncodeSpaces(UrlEncodeNonAscii(str, Encoding.UTF8));
		}
	}
}
