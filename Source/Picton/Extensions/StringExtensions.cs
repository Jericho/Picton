using System;
using System.Text;

namespace Picton
{
	public static class StringExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// From: http://stackoverflow.com/questions/4335878/c-sharp-trimstart-with-string-parameter
		/// </summary>
		/// <param name="target"></param>
		/// <param name="trimString"></param>
		/// <returns></returns>
		public static string TrimStart(this string target, string trimString, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
		{
			if (trimString == null) throw new ArgumentNullException(nameof(trimString));

			string result = target;
			while (result.StartsWith(trimString, stringComparison))
			{
				result = result.Substring(trimString.Length);
			}

			return result;
		}


		/// <summary>
		/// From: http://stackoverflow.com/questions/4335878/c-sharp-trimstart-with-string-parameter
		/// </summary>
		/// <param name="target"></param>
		/// <param name="trimString"></param>
		/// <returns></returns>
		public static string TrimEnd(this string target, string trimString, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
		{
			if (trimString == null) throw new ArgumentNullException(nameof(trimString));

			string result = target;
			while (result.EndsWith(trimString, stringComparison))
			{
				result = result.Substring(0, result.Length - trimString.Length);
			}

			return result;
		}

		/// <summary>
		/// Converts the string to a byte-array using the supplied encoding
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
#if NETFULL
			return (encoding ?? Encoding.Default).GetBytes(value);
#else
			return (encoding ?? Encoding.UTF8).GetBytes(value);
#endif
		}

		#endregion
	}
}
