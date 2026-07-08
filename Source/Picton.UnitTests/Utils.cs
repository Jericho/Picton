using System.Runtime.InteropServices;
using Xunit;

namespace Picton.UnitTests
{
	public static class Utils
	{
		public static void SkipWhenLinuxFullFramework()
		{
			var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#if NETFRAMEWORK || NETSTANDARD
			var isFullFramework = true;
#else
			var isFullFramework = false;
#endif

			Assert.SkipWhen(isLinux && isFullFramework, "Skipping because it fails under Linux with .NET full framework");
		}
	}
}
