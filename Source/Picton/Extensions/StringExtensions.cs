using System;
using System.Security.Cryptography;
using System.Text;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="string"/> data type.
	/// </summary>
	public static class StringExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Trim all occurrences of the exactly matching substring at the start of a given string.
		/// </summary>
		/// <remarks>
		/// From: http://stackoverflow.com/questions/4335878/c-sharp-trimstart-with-string-parameter
		/// </remarks>
		/// <param name="target">The string to be trimmed.</param>
		/// <param name="trimString">The substring to be removed.</param>
		/// <param name="comparisonType">Comparison settings.</param>
		/// <returns>The trimmed string.</returns>
		public static string TrimStart(this string target, string trimString, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (trimString == null) throw new ArgumentNullException(nameof(trimString));

			int startIndex = 0;
			while (target.IndexOf(trimString, startIndex, comparisonType) == startIndex)
			{
				startIndex += trimString.Length;
			}

			return target.Substring(startIndex);
		}

		/// <summary>
		/// Trim all occurrences of the exactly matching substring at the end of a given string.
		/// </summary>
		/// <remarks>
		/// From: http://stackoverflow.com/questions/4335878/c-sharp-trimstart-with-string-parameter
		/// </remarks>
		/// <param name="target">The string to be trimmed.</param>
		/// <param name="trimString">The substring to be removed.</param>
		/// <param name="comparisonType">Comparison settings.</param>
		/// <returns>The trimmed string.</returns>
		public static string TrimEnd(this string target, string trimString, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (trimString == null) throw new ArgumentNullException(nameof(trimString));

			int sourceLength = target.Length;
			int count = sourceLength;
			while (target.LastIndexOf(trimString, count - 1, comparisonType) == count - trimString.Length)
			{
				count -= trimString.Length;
			}

			return target.Substring(0, count);
		}

		/// <summary>
		/// Converts the string to a byte-array using the supplied encoding.
		/// </summary>
		/// <param name="value">The input string.</param>
		/// <param name="encoding">The encoding to be used.</param>
		/// <returns>The created byte array</returns>
		/// <example><code>
		/// var value = "Hello World";
		/// var ansiBytes = value.ToBytes(Encoding.GetEncoding(1252)); // 1252 = ANSI
		/// var utf8Bytes = value.ToBytes(Encoding.UTF8);
		/// </code></example>
		/// <remarks>From the .NET Extensions project: http://dnpextensions.codeplex.com/</remarks>
		public static byte[] ToBytes(this string value, Encoding encoding = null)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));

			return (encoding ?? Encoding.UTF8).GetBytes(value);
		}

		/// <summary>
		/// Calculates the MD5 hash for a given string.
		/// </summary>
		/// <param name="value">The string.</param>
		/// <returns>The hash.</returns>
		public static byte[] ToMD5Hash(this string value)
		{
			using (var md5 = MD5.Create())
			{
				// Convert the input string to a byte array.
				byte[] data = value.ToBytes(Encoding.UTF8);

				// Compute the hash.
				byte[] hash = md5.ComputeHash(data);

				// Return the byte array.
				return hash;
			}
		}

		/// <summary>
		/// Calculates the MD5 hash for a given string.
		/// </summary>
		/// <param name="value">The string.</param>
		/// <returns>The hash.</returns>
		public static string ToMD5HashString(this string value)
		{
			// Calculate the hash.
			var hash = value.ToMD5Hash();

			// Create a new Stringbuilder to collect the bytes and create a string.
			var sb = new StringBuilder();

			// Loop through each byte of the hashed data and format each one as a hexadecimal string.
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("x2"));
			}

			// Return the hexadecimal string.
			return sb.ToString();
		}

		#endregion
	}
}
