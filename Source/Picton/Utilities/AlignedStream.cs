using System.IO;

namespace Picton.Utilities
{
	internal class AlignedStream : Stream
	{
		#region FIELDS

		private readonly MultiStream _streams;

		#endregion

		#region PROPERTIES

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override long Length
		{
			get
			{
				return _streams.Length;
			}
		}

		public override long Position
		{
			get { return _streams.Position; }
			set { _streams.Position = value; }
		}

		#endregion

		#region CTORs

		public AlignedStream(Stream stream, int alignement)
		{
			var lengthModulo = stream.Length % alignement;
			var paddLength = lengthModulo == 0 ? 0 : alignement - lengthModulo;

			_streams = new MultiStream();
			_streams.AddStream(stream);
			_streams.AddStream(new MemoryStream(new byte[paddLength]));
		}

		#endregion

		#region PUBLIC METHODS

		public override void Flush() { }

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _streams.Seek(offset, origin);
		}

		public override void SetLength(long value) { }

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _streams.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
		}

		#endregion
	}
}
