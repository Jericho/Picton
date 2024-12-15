using System;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="Uri"/> data type.
	/// </summary>
	public static class UriExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Return a value indicating whether the Uri is the well-know Uri for the (now obsolete) storage emulator or Azurite.
		/// </summary>
		/// <param name="uri">The Uri.</param>
		/// <returns>True if the Uri corresponds to the emulator or Azurite Uri, false otherwise.</returns>
		public static bool IsEmulator(this Uri uri)
		{
			return uri.IsAbsoluteUri && uri.AbsoluteUri.StartsWith("http://127.0.0.1:10000/devstoreaccount1", StringComparison.Ordinal);
		}

		#endregion
	}
}
