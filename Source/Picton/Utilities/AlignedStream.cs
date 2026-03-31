using System.IO;

namespace Picton.Utilities
{
	/// <summary>
	/// Provides a read-only stream that aligns the length of the underlying stream to a specified boundary by padding with
	/// zeros as needed.
	/// </summary>
	/// <remarks>AlignedStream is useful when a stream must have a length that is a multiple of a given alignment,
	/// such as for certain file formats or hardware requirements. The stream is read-only and does not support writing.
	/// Seeking and reading are supported. The original stream is extended with zero bytes if its length is not already
	/// aligned. Disposing the AlignedStream also disposes the underlying stream.</remarks>
	internal class AlignedStream : Stream
	{
		#region FIELDS

		// Suppress Warning CA2213 because _streams is properly disposed in the 'ReleaseManagedResources' method.
#pragma warning disable CA2213
		private readonly MultiStream _streams;
#pragma warning restore CA2213

		#endregion

		#region PROPERTIES

		/// <summary>Gets a value indicating whether the current stream supports reading.</summary>
		public override bool CanRead => true;

		/// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
		public override bool CanSeek => true;

		/// <summary>
		/// Gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <remarks>This property always returns <see langword="false"/>. Writing to the stream is not
		/// supported.</remarks>
		public override bool CanWrite => false;

		/// <summary>Gets the length, in bytes, of the stream.</summary>
		public override long Length => _streams.Length;

		/// <summary>
		/// Gets or sets the current position within the stream.
		/// </summary>
		/// <remarks>Setting this property seeks to the specified position within the stream. The value must be
		/// non-negative and within the length of the stream. Attempting to set a position outside the valid range may result
		/// in an exception.</remarks>
		public override long Position
		{
			get { return _streams.Position; }
			set { _streams.Position = value; }
		}

		#endregion

		#region CTORs

		/// <summary>
		/// Initializes a new instance of the <see cref="AlignedStream"/> class, ensuring the underlying stream's length is aligned to the.
		/// specified boundary by appending padding if necessary.
		/// </summary>
		/// <remarks>This constructor appends zero bytes to the end of the provided stream if its length is not already a
		/// multiple of the specified alignment. The resulting stream can be used in scenarios where data alignment is required,
		/// such as certain file formats or hardware interfaces.</remarks>
		/// <param name="stream">The input stream to be aligned. Must be readable and seekable.</param>
		/// <param name="alignement">The alignment boundary, in bytes. The stream's length will be padded to the nearest multiple of this value. Must be
		/// a positive integer.</param>
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

		/// <summary>
		/// Clears all buffers for the current writer and causes any buffered data to be written to the underlying device.
		/// </summary>
		public override void Flush() { }

		/// <summary>
		/// Sets the position within the current stream to the specified value.
		/// </summary>
		/// <param name="offset">A byte offset relative to the position specified by the origin parameter.</param>
		/// <param name="origin">A value of type SeekOrigin indicating the reference point used to obtain the new position.</param>
		/// <returns>The new position within the stream, measured in bytes from the beginning of the stream.</returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
			return _streams.Seek(offset, origin);
		}

		/// <summary>
		/// Sets the length of the current stream to the specified value.
		/// </summary>
		/// <param name="value">The desired length of the stream in bytes. Must be non-negative and less than or equal to the maximum allowed
		/// length for the stream.</param>
		public override void SetLength(long value) { }

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of
		/// bytes read.
		/// </summary>
		/// <param name="buffer">The buffer to write the data into. Must not be null.</param>
		/// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data read from the stream. Must be
		/// non-negative and less than the length of the buffer.</param>
		/// <param name="count">The maximum number of bytes to read. Must be non-negative and the sum of offset and count must not exceed the
		/// buffer length.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many
		/// bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			return _streams.Read(buffer, offset, count);
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within the stream by the number
		/// of bytes written.
		/// </summary>
		/// <param name="buffer">The buffer containing the bytes to write to the stream.</param>
		/// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the stream.</param>
		/// <param name="count">The number of bytes to write to the stream.</param>
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
