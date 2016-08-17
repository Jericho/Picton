using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Picton.UnitTests
{
	[TestClass]
	public class MimeTypeMapTests
	{
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetMimeType_throws_when_extension_is_null()
		{
			var mimeType = MimeTypeMap.GetMimeType(null);
		}

		[TestMethod]
		public void GetMimeType_with_known_extension()
		{
			// Arrange
			var expected = "text/plain";

			// Act
			var mimeType = MimeTypeMap.GetMimeType(".txt");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[TestMethod]
		public void GetMimeType_with_malformed_extension()
		{
			// Arrange
			var expected = "text/plain";

			// Act
			var mimeType = MimeTypeMap.GetMimeType("txt");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[TestMethod]
		public void GetMimeType_with_unknown_extension()
		{
			// Arrange
			var expected = "application/octet-stream";

			// Act
			var mimeType = MimeTypeMap.GetMimeType(".blablabla");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetExtension_throws_when_mimeType_is_null()
		{
			var mimeType = MimeTypeMap.GetExtension(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void GetExtension_throws_when_mimeType_is_malformed()
		{
			var mimeType = MimeTypeMap.GetExtension(".blablabla");
		}

		[TestMethod]
		public void GetExtension_with_known_mimeType()
		{
			// Arrange
			var expected = ".txt";

			// Act
			var mimeType = MimeTypeMap.GetExtension("text/plain");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void GetExtension_with_unknown_mimeType()
		{
			var mimeType = MimeTypeMap.GetExtension("blablabla");
		}
	}
}
