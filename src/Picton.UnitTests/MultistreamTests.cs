using Microsoft.VisualStudio.TestTools.UnitTesting;
using Picton.Extensions;
using Shouldly;
using System;
using System.IO;
using System.Text;

namespace Picton.UnitTests
{
	[TestClass]
	public class MultistreamTests
	{
		[TestMethod]
		public void Check_properties()
		{
			// Arrange
			var stream = new MultiStream();

			// Assert
			stream.CanRead.ShouldBeTrue();
			stream.CanSeek.ShouldBeTrue();
			stream.CanWrite.ShouldBeFalse();
		}

		[TestMethod]
		public void Combine_two_streams()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Position = 0;

			var result = "";
			using (var sr = new StreamReader(stream, Encoding.UTF8))
			{
				result = sr.ReadToEnd();
			}


			// Assert
			result.ShouldBe(content1 + content2);
		}

		[TestMethod]
		public void Length()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));


			// Assert
			stream.Length.ShouldBe(content1.Length + content2.Length);
		}

		[TestMethod]
		public void Seek_from_beginning()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";
			var origin = SeekOrigin.Begin;


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Seek(4, origin);


			// Assert
			stream.Position.ShouldBe(4);
		}

		[TestMethod]
		public void Seek_from_current_position()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";
			var origin = SeekOrigin.Current;


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Position = 2;
			stream.Seek(2, origin);


			// Assert
			stream.Position.ShouldBe(4);
		}

		[TestMethod]
		public void Seek_from_end()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";
			var origin = SeekOrigin.End;


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Seek(2, origin);


			// Assert
			stream.Position.ShouldBe(content1.Length + content2.Length - 2);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Seek_throws_when_origin_is_invalid()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";
			var origin = (SeekOrigin)999;

			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Seek(4, origin);
		}

		[TestMethod]
		public void Seek_position_less_than_zero()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Seek(-2, SeekOrigin.Begin);


			// Assert
			stream.Position.ShouldBe(0);
		}

		[TestMethod]
		public void Seek_position_greather_than_length()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Seek(999, SeekOrigin.Begin);


			// Assert
			stream.Position.ShouldBe(content1.Length + content2.Length);
		}

		[TestMethod]
		public void Flush()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Flush();


			// Assert
			// The flush method is a noop. Therefore there's nothing to assert
		}

		[TestMethod]
		public void Write()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Write("New content".ToBytes(), 0, 11);


			// Assert
			// PLEASE NOTE: Write is a noop. Therefore it doesn't change the content of the stream
			stream.Length.ShouldBe(content1.Length + content2.Length);
		}

		[TestMethod]
		public void SetLength()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.SetLength(999);


			// Assert
			// PLEASE NOTE: SetLength is a noop. Therefore it doesn't change the size of the stream
			stream.Length.ShouldBe(content1.Length + content2.Length);
		}

		[TestMethod]
		public void Read_accross_two_streams()
		{
			// Arrange
			var content1 = "Hello";
			var content2 = " World";


			// Act
			var stream = new MultiStream();
			stream.AddStream(new MemoryStream(content1.ToBytes()));
			stream.AddStream(new MemoryStream(content2.ToBytes()));
			stream.Position = 0;
			var buffer = new byte[7];
			var read = stream.Read(buffer, 0, 7);
			var result = Encoding.UTF8.GetString(buffer, 0, buffer.Length);


			// Assert
			read.ShouldBe(7);
			stream.Position.ShouldBe(7);
			result.ShouldBe("Hello W");
		}
	}
}
