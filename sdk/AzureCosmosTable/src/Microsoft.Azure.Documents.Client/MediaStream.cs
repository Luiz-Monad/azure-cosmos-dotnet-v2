using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Client
{
	internal sealed class MediaStream : Stream
	{
		private HttpResponseMessage responseMessage;

		private Stream contentStream;

		private bool isDisposed;

		public override bool CanRead
		{
			get
			{
				return contentStream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return contentStream.CanSeek;
			}
		}

		public override bool CanTimeout
		{
			get
			{
				return contentStream.CanTimeout;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return contentStream.CanWrite;
			}
		}

		public override long Length
		{
			get
			{
				return contentStream.Length;
			}
		}

		public override int ReadTimeout
		{
			get
			{
				return contentStream.ReadTimeout;
			}
			set
			{
				contentStream.ReadTimeout = value;
			}
		}

		public override int WriteTimeout
		{
			get
			{
				return contentStream.WriteTimeout;
			}
			set
			{
				contentStream.WriteTimeout = value;
			}
		}

		public override long Position
		{
			get
			{
				return contentStream.Position;
			}
			set
			{
				contentStream.Position = value;
			}
		}

		public MediaStream(HttpResponseMessage responseMessage, Stream contentStream)
		{
			this.responseMessage = responseMessage;
			this.contentStream = contentStream;
			isDisposed = false;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return contentStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			contentStream.Write(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return contentStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return contentStream.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			return contentStream.CopyToAsync(destination, bufferSize, cancellationToken);
		}

		public override void Flush()
		{
			contentStream.Flush();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return contentStream.FlushAsync(cancellationToken);
		}

		public override int ReadByte()
		{
			return contentStream.ReadByte();
		}

		public override void WriteByte(byte value)
		{
			contentStream.WriteByte(value);
		}

		public override void SetLength(long value)
		{
			contentStream.SetLength(value);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return contentStream.Seek(offset, origin);
		}

		protected override void Dispose(bool disposing)
		{
			if (!isDisposed && disposing)
			{
				responseMessage.Dispose();
				isDisposed = true;
			}
			base.Dispose(disposing);
		}
	}
}
