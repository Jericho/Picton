using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;

namespace Picton.Extensions
{
	public static class AssemblyExtensions
	{
		#region PUBLIC EXTENSION METHODS

		public static bool IsDebugConfiguration(this Assembly assembly)
		{
			return IsAssemblyConfiguration(assembly, "Debug");
		}

		public static bool IsReleaseConfiguration(this Assembly assembly)
		{
			return IsAssemblyConfiguration(assembly, "Release");
		}

		#endregion

		#region PRIVATE METHODS

		private static bool IsAssemblyConfiguration(Assembly assembly, string configuration)
		{
			// Get the 'AssemblyConfiguration' attributes
			var attributes = assembly.GetCustomAttributes<AssemblyConfigurationAttribute>();

			// We expect only one attribute
			if (attributes == null || !attributes.Any())
			{
				throw new Exception("Assembly does not contain the AssemblyConfiguration attribute.");
			}
			else if (attributes.Count() > 1)
			{
				throw new Exception("Assembly contains multiple AssemblyConfiguration attributes. There should only be one attribute.");
			}

			// Determine if the attribute matches the specified configuration
			var assemblyConfiguration = attributes.First().Configuration;
			return assemblyConfiguration.Equals(configuration, StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}
}
