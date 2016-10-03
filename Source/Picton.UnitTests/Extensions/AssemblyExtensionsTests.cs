using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Picton.Extensions.UnitTests
{
	[TestClass]
	public class AssemblyExtensionsTests
	{
		[TestMethod]
		public void Configuration_attribute_is_debug()
		{
			// Arrange
			var mockAssembly = new Mock<_Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false))
				.Returns(new object[] { new AssemblyConfigurationAttribute("Debug") });

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
			var isRelease = mockAssembly.Object.IsReleaseConfiguration();

			// Assert
			isDebug.ShouldBeTrue();
			isRelease.ShouldBeFalse();
		}

		[TestMethod]
		public void Configuration_attribute_is_release()
		{
			// Arrange
			var mockAssembly = new Mock<_Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false))
				.Returns(new object[] { new AssemblyConfigurationAttribute("Release") });

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
			var isRelease = mockAssembly.Object.IsReleaseConfiguration();

			// Assert
			isDebug.ShouldBeFalse();
			isRelease.ShouldBeTrue();
		}

		[TestMethod]
		public void Configuration_attribute_is_unknown()
		{
			// Arrange
			var mockAssembly = new Mock<_Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false))
				.Returns(new object[] { new AssemblyConfigurationAttribute("unknown_value") });

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
			var isRelease = mockAssembly.Object.IsReleaseConfiguration();

			// Assert
			isDebug.ShouldBeFalse();
			isRelease.ShouldBeFalse();
		}

		[TestMethod]
		[ExpectedException(typeof(Exception))]
		public void Configuration_attribute_is_invalid()
		{
			// Arrange
			var mockAssembly = new Mock<_Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false))
				.Returns(new object[] { "blablabla" });

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
		}

		[TestMethod]
		[ExpectedException(typeof(Exception))]
		public void Configuration_attribute_is_missing()
		{
			// Arrange
			var mockAssembly = new Mock<_Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false))
				.Returns(new object[] { });

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
		}

		[TestMethod]
		[ExpectedException(typeof(Exception))]
		public void Configuration_attribute_is_specified_multiple_times()
		{
			// Arrange
			var mockAssembly = new Mock<_Assembly>(MockBehavior.Strict);
			mockAssembly
				.Setup(a => a.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false))
				.Returns(new object[]
				{
					new AssemblyConfigurationAttribute("Debug"),
					new AssemblyConfigurationAttribute("Debug")
				});

			// Act
			var isDebug = mockAssembly.Object.IsDebugConfiguration();
		}
	}
}
