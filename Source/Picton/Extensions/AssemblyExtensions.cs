using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Picton.Extensions
{
	public static class AssemblyExtensions
	{
		#region PUBLIC EXTENSION METHODS

		public static bool IsDebugConfiguration(this _Assembly assembly)
		{
			return IsAssemblyConfiguration(assembly, "Debug");
		}

		public static bool IsReleaseConfiguration(this _Assembly assembly)
		{
			return IsAssemblyConfiguration(assembly, "Release");
		}

		#endregion

		#region PRIVATE METHODS

		private static bool IsAssemblyConfiguration(_Assembly assembly, string configuration)
		{
			// Get the 'AssemblyConfiguration' attributes
			var attributes = assembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);

			// We expect only one attribute
			if (attributes == null || attributes.Length == 0)
			{
				throw new Exception("Assembly does not contain the AssemblyConfiguration attribute.");
			}
			else if (attributes.Length > 1)
			{
				throw new Exception("Assembly contains multiple AssemblyConfiguration attributes. There should only be one attribute.");
			}

			// Make sure the attribute is valid
			var assemblyConfiguration = attributes[0] as AssemblyConfigurationAttribute;
			if (assemblyConfiguration == null)
			{
				throw new Exception("AssemblyConfiguration attribute is invalid");
			}

			// Determine if the attribute matches the specified configuration
			return assemblyConfiguration.Configuration.Equals(configuration, StringComparison.InvariantCultureIgnoreCase);
		}

		#endregion
	}
}
