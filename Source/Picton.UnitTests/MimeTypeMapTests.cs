using HeyRed.Mime;
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
			Should.Throw<NullReferenceException>(() =>
			{
				var mimeType = MimeTypesMap.GetMimeType(null);
			});
		}

		[Fact]
		public void GetMimeType_with_known_extension()
		{
			// Arrange
			var expected = "text/plain";

			// Act
			var mimeType = MimeTypesMap.GetMimeType("myfile.txt");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[Fact]
		public void GetMimeType_with_unknown_extension()
		{
			// Arrange
			var expected = "application/octet-stream";

			// Act
			var mimeType = MimeTypesMap.GetMimeType("myfile.blablabla");

			// Assert
			mimeType.ShouldBe(expected);
		}

		[Fact]
		public void GetExtension_throws_when_mimeType_is_null()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var extension = MimeTypesMap.GetExtension(null);
			});
		}

		[Fact]
		public void GetExtension_with_known_mimeType()
		{
			// Arrange
			var expected = "txt";

			// Act
			var extension = MimeTypesMap.GetExtension("text/plain");

			// Assert
			extension.ShouldBe(expected);
		}

		[Fact]
		public void GetExtension_with_unknown_mimeType()
		{
			// Arrange
			var expected = "bin";

			// Act
			var extension = MimeTypesMap.GetExtension("notvalid/bogus");

			// Assert
			extension.ShouldBe(expected);
		}
	}
}
