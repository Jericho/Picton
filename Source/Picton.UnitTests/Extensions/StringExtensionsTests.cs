using Shouldly;
using System;
using System.Text;
using Xunit;

namespace Picton.Extensions.UnitTests
{
	public class StringExtensionsTests
	{
		[Fact]
		public void TrimStart_throws_if_trimString_is_null()
		{
			// Arrange
			var source = "abc";
			var trim = (string)null;

			// Act
			Should.Throw<ArgumentNullException>(() => source.TrimStart(trim));
		}

		[Fact]
		public void TrimStart_no_match()
		{
			// Arrange
			var source = "abc";
			var trim = "zzz";

			// Act
			var result = source.TrimStart(trim);

			// Asert
			result.ShouldBe("abc");
		}

		[Fact]
		public void TrimStart_one_match()
		{
			// Arrange
			var source = "zzzabc";
			var trim = "zzz";

			// Act
			var result = source.TrimStart(trim);

			// Asert
			result.ShouldBe("abc");
		}

		[Fact]
		public void TrimStart_multiple_matches()
		{
			// Arrange
			var source = "zzzzzzzzzabc";
			var trim = "zzz";

			// Act
			var result = source.TrimStart(trim);

			// Asert
			result.ShouldBe("abc");
		}

		[Fact]
		public void TrimEnd_throws_if_trimString_is_null()
		{
			// Arrange
			var source = "abc";
			var trim = (string)null;

			// Act
			Should.Throw<ArgumentNullException>(() => source.TrimEnd(trim));
		}

		[Fact]
		public void TrimEnd_no_match()
		{
			// Arrange
			var source = "abc";
			var trim = "zzz";

			// Act
			var result = source.TrimEnd(trim);

			// Asert
			result.ShouldBe("abc");
		}

		[Fact]
		public void TrimEnd_one_match()
		{
			// Arrange
			var source = "abczzz";
			var trim = "zzz";

			// Act
			var result = source.TrimEnd(trim);

			// Asert
			result.ShouldBe("abc");
		}

		[Fact]
		public void TrimEnd_multiple_matches()
		{
			// Arrange
			var source = "abczzzzzzzzz";
			var trim = "zzz";

			// Act
			var result = source.TrimEnd(trim);

			// Asert
			result.ShouldBe("abc");
		}

		[Fact]
		public void ToBytes_null()
		{
			// Arrange
			var source = (string)null;

			// Act
			Should.Throw<ArgumentNullException>(() => source.ToBytes());
		}

		[Fact]
		public void ToBytes_UTF8()
		{
			// Arrange
			var source = "Hello World";
			var expected = new byte[] { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100 };

			// Act
			var result = source.ToBytes(Encoding.UTF8);

			// Assert
			result.ShouldBe(expected);
		}
	}
}
