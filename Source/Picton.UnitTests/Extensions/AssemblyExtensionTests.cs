using Moq;
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

				var mockAssembly = new Mock<MockAssembly>(MockBehavior.Strict);
				mockAssembly
					.Setup(m => m.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true))
					.Returns(attributes)
					.Verifiable();

				// Act
				var result = mockAssembly.Object.IsDebugConfiguration();

				// Assert
				mockAssembly.Verify();
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

				var mockAssembly = new Mock<MockAssembly>(MockBehavior.Strict);
				mockAssembly
					.Setup(m => m.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true))
					.Returns(attributes)
					.Verifiable();

				// Act
				var result = mockAssembly.Object.IsDebugConfiguration();

				// Assert
				mockAssembly.Verify();
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

				var mockAssembly = new Mock<MockAssembly>(MockBehavior.Strict);
				mockAssembly
					.Setup(m => m.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true))
					.Returns(attributes)
					.Verifiable();

				// Act
				var result = mockAssembly.Object.IsReleaseConfiguration();

				// Assert
				mockAssembly.Verify();
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

				var mockAssembly = new Mock<MockAssembly>(MockBehavior.Strict);
				mockAssembly
					.Setup(m => m.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true))
					.Returns(attributes)
					.Verifiable();

				// Act
				var result = mockAssembly.Object.IsDebugConfiguration();

				// Assert
				mockAssembly.Verify();
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

				var mockAssembly = new Mock<MockAssembly>(MockBehavior.Strict);
				mockAssembly
					.Setup(m => m.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true))
					.Returns(attributes)
					.Verifiable();

				// Act
				var e = Should.Throw<Exception>(() => mockAssembly.Object.IsDebugConfiguration());

				// Assert
				mockAssembly.Verify();
				e.Message.ShouldBe("Assembly does not contain the AssemblyConfiguration attribute.");
			}

			[Fact]
			public void Throws_if_zero_matching_attributes()
			{
				// Arrange
				var attributes = new AssemblyConfigurationAttribute[] { };

				var mockAssembly = new Mock<MockAssembly>(MockBehavior.Strict);
				mockAssembly
					.Setup(m => m.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true))
					.Returns(attributes)
					.Verifiable();

				// Act
				var e = Should.Throw<Exception>(() => mockAssembly.Object.IsDebugConfiguration());

				// Assert
				mockAssembly.Verify();
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

				var mockAssembly = new Mock<MockAssembly>(MockBehavior.Strict);
				mockAssembly
					.Setup(m => m.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true))
					.Returns(attributes)
					.Verifiable();

				// Act
				var e = Should.Throw<Exception>(() => mockAssembly.Object.IsDebugConfiguration());

				// Assert
				mockAssembly.Verify();
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
