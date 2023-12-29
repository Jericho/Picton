using NSubstitute;
using Shouldly;
using System;
using System.Reflection;
using Xunit;

namespace Picton.UnitTests.Extensions
{
	public class AssemblyExtensionTests
	{
		public class IsDebugConfiguration
		{
			[Fact]
			public void True()
			{
				// Arrange
				var attributes = new[]
				{
					new AssemblyConfigurationAttribute("debug")
				};

				var mockAssembly = Substitute.For<MockAssembly>();
				mockAssembly
					.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true)
					.Returns(attributes);

				// Act
				var result = mockAssembly.IsDebugConfiguration();

				// Assert
				result.ShouldBeTrue();
			}

			[Fact]
			public void False()
			{
				// Arrange
				var attributes = new[]
				{
					new AssemblyConfigurationAttribute("qwerty")
				};

				var mockAssembly = Substitute.For<MockAssembly>();
				mockAssembly
					.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true)
					.Returns(attributes);

				// Act
				var result = mockAssembly.IsDebugConfiguration();

				// Assert
				result.ShouldBeFalse();
			}
		}

		public class IsReleaseConfiguration
		{
			[Fact]
			public void True()
			{
				// Arrange
				var attributes = new[]
				{
					new AssemblyConfigurationAttribute("release")
				};

				var mockAssembly = Substitute.For<MockAssembly>();
				mockAssembly
					.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true)
					.Returns(attributes);

				// Act
				var result = mockAssembly.IsReleaseConfiguration();

				// Assert
				result.ShouldBeTrue();
			}

			[Fact]
			public void False()
			{
				// Arrange
				var attributes = new[]
				{
					new AssemblyConfigurationAttribute("qwerty")
				};

				var mockAssembly = Substitute.For<MockAssembly>();
				mockAssembly
					.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true)
					.Returns(attributes);

				// Act
				var result = mockAssembly.IsDebugConfiguration();

				// Assert
				result.ShouldBeFalse();
			}
		}

		public class IsAssemblyConfiguration
		{
			[Fact]
			public void Throws_if_null_matching_attributes()
			{
				// Arrange
				var attributes = (AssemblyConfigurationAttribute[])null;

				var mockAssembly = Substitute.For<MockAssembly>();
				mockAssembly
					.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true)
					.Returns(attributes);

				// Act
				var e = Should.Throw<Exception>(() => mockAssembly.IsDebugConfiguration());

				// Assert
				e.Message.ShouldBe("Assembly does not contain the AssemblyConfiguration attribute.");
			}

			[Fact]
			public void Throws_if_zero_matching_attributes()
			{
				// Arrange
				var attributes = Array.Empty<AssemblyConfigurationAttribute>();

				var mockAssembly = Substitute.For<MockAssembly>();
				mockAssembly
					.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true)
					.Returns(attributes);

				// Act
				var e = Should.Throw<Exception>(() => mockAssembly.IsDebugConfiguration());

				// Assert
				e.Message.ShouldBe("Assembly does not contain the AssemblyConfiguration attribute.");
			}

			[Fact]
			public void Throws_if_multiple_matching_attributes()
			{
				// Arrange
				var attributes = new AssemblyConfigurationAttribute[]
				{
					new AssemblyConfigurationAttribute("qwerty"),
					new AssemblyConfigurationAttribute("qwerty")
				};

				var mockAssembly = Substitute.For<MockAssembly>();
				mockAssembly
					.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true)
					.Returns(attributes);

				// Act
				var e = Should.Throw<Exception>(() => mockAssembly.IsDebugConfiguration());

				// Assert
				e.Message.ShouldBe("Assembly contains multiple AssemblyConfiguration attributes. There should only be one attribute.");
			}
		}
	}

	// For some reason, we can't mock Assembly. We need a class that derives from Assembly.
	// If you try to mock Assembly you get the following exception:
	//	The type System.Reflection.Assembly implements ISerializable, but failed to provide a deserialization constructor
	public class MockAssembly : Assembly
	{
		public MockAssembly()
		{
		}
	}
}
