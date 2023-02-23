using System.IO;

namespace Picton.Utilities
{
	internal class AlignedStream : Stream
	{
		#region FIELDS

		// Suppress Warning CA2213 because _streams is properly disposed in the 'ReleaseManagedResources' method.
#pragma warning disable CA2213
		private readonly MultiStream _streams;
#pragma warning restore CA2213

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

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ReleaseManagedResources();
			}
			else
			{
				// The object went out of scope and the Finalizer has been called.
				// The GC will take care of releasing managed resources, therefore there is nothing to do here.
			}

			ReleaseUnmanagedResources();
		}

		#endregion

		#region PRIVATE METHODS

		private void ReleaseManagedResources()
		{
			if (_streams != null)
			{
				_streams.Dispose();
			}
		}

		private void ReleaseUnmanagedResources()
		{
			// We do not hold references to unmanaged resources
		}

		#endregion
	}
}
