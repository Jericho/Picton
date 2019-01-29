using System;
using System.Linq;
using System.Reflection;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="Assembly"/> class.
	/// </summary>
	public static class AssemblyExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Returns a boolean indicating if the assembly has been compiled in Debug mode.
		/// </summary>
		/// <param name="assembly">The assembly.</param>
		/// <returns>true if the assembly was compiled in Debug mode; false otherwise.</returns>
		public static bool IsDebugConfiguration(this Assembly assembly)
		{
			return IsAssemblyConfiguration(assembly, "Debug");
		}

		/// <summary>
		/// Returns a boolean indicating if the assembly has been compiled in Release mode.
		/// </summary>
		/// <param name="assembly">The assembly.</param>
		/// <returns>true if the assembly was compiled in Release mode; false otherwise.</returns>
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
