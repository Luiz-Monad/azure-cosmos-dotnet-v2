using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class CloneableStream : Stream
	{
		private readonly MemoryStream internalStream;

		public override bool CanRead => internalStream.CanRead;

		public override bool CanSeek => internalStream.CanSeek;

		public override bool CanTimeout => internalStream.CanTimeout;

		public override bool CanWrite => internalStream.CanWrite;

		public override long Length => internalStream.Length;

		public override long Position
		{
			get
			{
				return internalStream.Position;
			}
			set
			{
				internalStream.Position = value;
			}
		}

		public override int ReadTimeout
		{
			get
			{
				return internalStream.ReadTimeout;
			}
			set
			{
				internalStream.ReadTimeout = value;
			}
		}

		public override int WriteTimeout
		{
			get
			{
				return internalStream.WriteTimeout;
			}
			set
			{
				internalStream.WriteTimeout = value;
			}
		}

		public CloneableStream Clone()
		{
			return new CloneableStream(new MemoryStream(CustomTypeExtensions.GetBuffer(internalStream), 0, (int)internalStream.Length, writable: false, publiclyVisible: true));
		}

		public CloneableStream(MemoryStream internalStream)
		{
			this.internalStream = internalStream;
		}

		public override void Flush()
		{
			internalStream.Flush();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return internalStream.FlushAsync(cancellationToken);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return internalStream.Read(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return internalStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override int ReadByte()
		{
			return internalStream.ReadByte();
		}

		public override long Seek(long offset, SeekOrigin loc)
		{
			return internalStream.Seek(offset, loc);
		}

		public override void SetLength(long value)
		{
			internalStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			internalStream.Write(buffer, offset, count);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return internalStream.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public override void WriteByte(byte value)
		{
			internalStream.WriteByte(value);
		}

		protected override void Dispose(bool disposing)
		{
			internalStream.Dispose();
		}

		public void WriteTo(Stream target)
		{
			internalStream.WriteTo(target);
		}
	}
}
