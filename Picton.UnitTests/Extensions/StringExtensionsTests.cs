using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace Picton.Extensions.UnitTests
{
	[TestClass]
	public class StringExtensionsTests
	{
		[TestMethod]
		public void TrimStart_no_match()
		{
			// Arrange
			var target = "abc";

			// Act
			var result = target.TrimStart("zzz");

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimStart_one_match()
		{
			// Arrange
			var target = "zzzabc";

			// Act
			var result = target.TrimStart("zzz");

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimStart_multiple_matches()
		{
			// Arrange
			var target = "zzzzzzzzzabc";

			// Act
			var result = target.TrimStart("zzz");

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimEnd_no_match()
		{
			// Arrange
			var target = "abc";

			// Act
			var result = target.TrimEnd("zzz");

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimEnd_one_match()
		{
			// Arrange
			var target = "abczzz";

			// Act
			var result = target.TrimEnd("zzz");

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void TrimEnd_multiple_matches()
		{
			// Arrange
			var target = "abczzzzzzzzz";

			// Act
			var result = target.TrimEnd("zzz");

			// Asert
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ToBytes_null()
		{
			// Arrange
			var target = (string)null;

			// Act
			var result = target.ToBytes();
		}

		[TestMethod]
		public void ToBytes_UTF8()
		{
			// Arrange
			var target = "Hello World";
			var expected = new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100 };

			// Act
			var result = target.ToBytes(Encoding.UTF8);

			// Assert
			CollectionAssert.AreEqual(expected, result);
		}
	}
}
