using Shouldly;
using System;
using Xunit;

namespace Picton.UnitTests
{
	public class MimeTypeMapTests
	{
		[Fact]
		public void GetMimeType_throws_when_extension_is_null()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var mimeType = MimeTypeMap.GetMimeType(null);
			});
		}

		[Fact]
		public void GetMimeType_with_known_extension()
		{
			// Arrange
			var expected = "text/plain";

			// Act
			var mimeType = MimeTypeMap.GetMimeType(".txt");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[Fact]
		public void GetMimeType_with_malformed_extension()
		{
			// Arrange
			var expected = "text/plain";

			// Act
			var mimeType = MimeTypeMap.GetMimeType("txt");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[Fact]
		public void GetMimeType_with_unknown_extension()
		{
			// Arrange
			var expected = "application/octet-stream";

			// Act
			var mimeType = MimeTypeMap.GetMimeType(".blablabla");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[Fact]
		public void GetExtension_throws_when_mimeType_is_null()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var mimeType = MimeTypeMap.GetExtension(null);
			});
		}

		[Fact]
		public void GetExtension_throws_when_mimeType_is_malformed()
		{
			Should.Throw<ArgumentException>(() =>
			{
				var mimeType = MimeTypeMap.GetExtension(".blablabla");
			});
		}

		[Fact]
		public void GetExtension_with_known_mimeType()
		{
			// Arrange
			var expected = ".txt";

			// Act
			var mimeType = MimeTypeMap.GetExtension("text/plain");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[Fact]
		public void GetExtension_with_unknown_mimeType()
		{
			Should.Throw<ArgumentException>(() =>
			{
				var mimeType = MimeTypeMap.GetExtension("blablabla");
			});
		}
	}
}
