using System.Collections;
using System.IO;

namespace Picton.Utilities
{
	internal class MultiStream : Stream
	{
		#region FIELDS

		private ArrayList streamList = new ArrayList();
		private long position = 0;

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
				long result = 0;
				foreach (Stream stream in streamList)
				{
					result += stream.Length;
				}

				return result;
			}
		}

		public override long Position
		{
			get { return position; }
			set { Seek(value, SeekOrigin.Begin); }
		}

		#endregion

		#region PUBLIC METHODS

		public override void Flush() { }

		public override long Seek(long offset, SeekOrigin origin)
		{
			long len = Length;
			switch (origin)
			{
				case SeekOrigin.Begin:
					position = offset;
					break;
				case SeekOrigin.Current:
					position += offset;
					break;
				case SeekOrigin.End:
					position = len - offset;
					break;
			}

			if (position > len)
			{
				position = len;
			}
			else if (position < 0)
			{
				position = 0;
			}

			return position;
		}

		public override void SetLength(long value) { }

		public void AddStream(Stream stream)
		{
			streamList.Add(stream);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			long len = 0;
			int result = 0;
			int buf_pos = offset;
			int bytesRead;
			foreach (Stream stream in streamList)
			{
				if (position < (len + stream.Length))
				{
					// Don't attempt to re-position streams if they are already at the right position.
					// This is particularly important when dealing with with non-seekable streams.
					var newPosition = position - len;
					if (newPosition != stream.Position) stream.Position = newPosition;

					bytesRead = stream.Read(buffer, buf_pos, count);
					result += bytesRead;
					buf_pos += bytesRead;
					position += bytesRead;
					if (bytesRead < count)
					{
						count -= bytesRead;
					}
					else
					{
						break;
					}
				}

				len += stream.Length;
			}

			return result;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
		}

		#endregion
	}
}
