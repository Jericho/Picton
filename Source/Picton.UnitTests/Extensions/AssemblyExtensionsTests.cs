using Moq;
using Shouldly;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Picton.Extensions.UnitTests
{
	public class AssemblyExtensionsTests
	{
		// Can't moq an extension method like GetCustomAttributes
		/*
		[Fact]
		public void Configuration_attribute_is_debug()
		{
			// Arrange
			var mockAssembly = new Mock<Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes<AssemblyConfigurationAttribute>())
				.Returns(new[] { new AssemblyConfigurationAttribute("Debug") });

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
			var isRelease = mockAssembly.Object.IsReleaseConfiguration();

			// Assert
			isDebug.ShouldBeTrue();
			isRelease.ShouldBeFalse();
		}

		[Fact]
		public void Configuration_attribute_is_release()
		{
			// Arrange
			var mockAssembly = new Mock<Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes<AssemblyConfigurationAttribute>())
				.Returns(new[] { new AssemblyConfigurationAttribute("Release") });

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
			var isRelease = mockAssembly.Object.IsReleaseConfiguration();

			// Assert
			isDebug.ShouldBeFalse();
			isRelease.ShouldBeTrue();
		}

		[Fact]
		public void Configuration_attribute_is_unknown()
		{
			// Arrange
			var mockAssembly = new Mock<Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes<AssemblyConfigurationAttribute>())
				.Returns(new[] { new AssemblyConfigurationAttribute("unknown_value") });

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
			var isRelease = mockAssembly.Object.IsReleaseConfiguration();

			// Assert
			isDebug.ShouldBeFalse();
			isRelease.ShouldBeFalse();
		}

		[Fact]
		public void Configuration_attribute_is_missing()
		{
			// Arrange
			var mockAssembly = new Mock<Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes<AssemblyConfigurationAttribute>())
				.Returns(new AssemblyConfigurationAttribute[] { });

			// Act
			Should.Throw<Exception>(() =>
			{
				var isDebug = mockAssembly.Object.IsDebugConfiguration();
			});
		}

		[Fact]
		public void Configuration_attribute_is_specified_multiple_times()
		{
			// Arrange
			var mockAssembly = new Mock<Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes<AssemblyConfigurationAttribute>())
				.Returns(new[]
				{
					new AssemblyConfigurationAttribute("Debug"),
					new AssemblyConfigurationAttribute("Debug")
				});

			// Act
			Should.Throw<Exception>(() =>
			{
				var isDebug = mockAssembly.Object.IsDebugConfiguration();
			});
		}
		*/
	}
}
