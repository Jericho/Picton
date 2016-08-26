using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace Picton.Extensions.UnitTests
{
	[TestClass]
	public class StringExtensionsTests
	{
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TrimStart_throws_if_trimString_is_null()
		{
			// Arrange
			var source = "abc";
			var trim = (string)null;

			// Act
			var result = source.TrimStart(trim);
		}

		[TestMethod]
		public void TrimStart_no_match()
		{
			// Arrange
			var source = "abc";
			var trim = "zzz";

			// Act
			var result = source.TrimStart(trim);

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimStart_one_match()
		{
			// Arrange
			var source = "zzzabc";
			var trim = "zzz";

			// Act
			var result = source.TrimStart(trim);

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimStart_multiple_matches()
		{
			// Arrange
			var source = "zzzzzzzzzabc";
			var trim = "zzz";

			// Act
			var result = source.TrimStart(trim);

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TrimEnd_throws_if_trimString_is_null()
		{
			// Arrange
			var source = "abc";
			var trim = (string)null;

			// Act
			var result = source.TrimEnd(trim);
		}

		[TestMethod]
		public void TrimEnd_no_match()
		{
			// Arrange
			var source = "abc";
			var trim = "zzz";

			// Act
			var result = source.TrimEnd(trim);

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimEnd_one_match()
		{
			// Arrange
			var source = "abczzz";
			var trim = "zzz";

			// Act
			var result = source.TrimEnd(trim);

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimEnd_multiple_matches()
		{
			// Arrange
			var source = "abczzzzzzzzz";
			var trim = "zzz";

			// Act
			var result = source.TrimEnd(trim);

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ToBytes_null()
		{
			// Arrange
			var source = (string)null;

			// Act
			var result = source.ToBytes();
		}

		[TestMethod]
		public void ToBytes_UTF8()
		{
			// Arrange
			var source = "Hello World";
			var expected = new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100 };

			// Act
			var result = source.ToBytes(Encoding.UTF8);

			// Assert
			CollectionAssert.AreEqual(expected, result);
		}
	}
}
