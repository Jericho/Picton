using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Picton
{
	/// <summary>
	/// This class attempts to detect which version of the Azure emulator is installed on your machine.
	/// One thing that is quite confusing is that the version of the emulator does not match the Azure
	/// SDK version. 
	/// 
	/// For instance:
	///	Storage Emulator:
	///		- 2.0 was released with SDK 2.2 in September 2013
	///		- 3.0 was released with SDK 2.3 in May 2014
	///		- 3.3 was released with SDK 2.4 in August 2014
	///		- 3.4 was released with SDK 2.5 in November 2014
	///		- 4.0 was released with SDK 2.6 in May 2015
	///		- 4.1 was released with SDK 2.7 in August 2015
	///		- 4.2 was released with SDK 2.8 in November 2015
	///		- 4.3 was released with SDK 2.9 in April 2016
	///		- 4.4 was released with SDK 2.9.1 in May 2016
	///		- 4.5 was released with SDK 2.9.5 in August 2016
	///		- 4.6 was released with in November 2016 as a seperate download, not as part of SDK 2.9.6
	/// </summary>
	/// <remarks>Inspired by <seealso cref="http://stackoverflow.com/questions/7547567/how-to-start-azure-storage-emulator-from-within-a-program">this StackOverflow discussion</seealso></remarks>
	public static class AzureEmulatorManager
	{
		private class EmulatorVersionInfo
		{
			/// <summary>
			/// The emulator version
			/// </summary>
			public int Version { get; private set; }

			/// <summary>
			/// Array containing the various possible process names for a given version of the emulator. 
			/// The process name is not always the same on different platforms. For instance, "WAStorageEmulator" is named "WASTOR~1" on Windows 8.
			/// That's why we need an array of strings to store the various names rather than a simple string
			/// </summary>
			public string[] ProcessNames { get; private set; }

			/// <summary>
			///  The path where the emulator executable is located
			/// </summary>
			public string ExecutablePath { get; private set; }

			/// <summary>
			/// The parameters expected by the emulator when starting
			/// </summary>
			public string Parameters { get; private set; }

			public EmulatorVersionInfo(int version, IEnumerable<string> processNames, string executablePath, string parameters)
			{
				Version = version;
				ProcessNames = processNames.ToArray();
				ExecutablePath = executablePath;
				Parameters = parameters;
			}
		}

		#region FIELDS

		private static IList<EmulatorVersionInfo> _emulatorVersions = new List<EmulatorVersionInfo>();

		#endregion

		#region CONSTRUCTOR

		static AzureEmulatorManager()
		{
			_emulatorVersions.Add(new EmulatorVersionInfo(2, new[] { "DSService" }, @"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe", "/devstore:start"));
			_emulatorVersions.Add(new EmulatorVersionInfo(3, new[] { "WAStorageEmulator", "WASTOR~1" }, @"C:\Program Files (x86)\Microsoft SDKs\Windows Azure\Storage Emulator\WAStorageEmulator.exe", "start"));
			_emulatorVersions.Add(new EmulatorVersionInfo(4, new[] { "AzureStorageEmulator" }, @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe", "start"));
		}

		#endregion

		#region PUBLIC METHODS

		/// <summary>
		/// Start the most recent version of the storage emulator if not already started.
		/// </summary>
		public static void EnsureStorageEmulatorIsStarted()
		{
			var found = false;

			// Ordering emulators in reverse order is important. 
			// We want to ensure we start the most recent version, even if an older version is available
			foreach (var emulatorVersion in _emulatorVersions.OrderByDescending(x => x.Version))
			{
				if (File.Exists(emulatorVersion.ExecutablePath))
				{
					var count = 0;
					foreach (var processName in emulatorVersion.ProcessNames)
					{
						count += Process.GetProcessesByName(processName).Length;
					}
					if (count == 0) StartStorageEmulator(emulatorVersion.Parameters, emulatorVersion.ExecutablePath);
					found = true;
					break;
				}
			}

			if (!found)
			{
				throw new FileNotFoundException("Unable to find the Azure emulator on this computer");
			}
		}

		/// <summary>
		/// Stop the storage emulator if running
		/// </summary>
		public static void StopStorageEmulator()
		{
			foreach (var processName in _emulatorVersions.SelectMany(x => x.ProcessNames))
			{
				var process = Process.GetProcessesByName(processName).FirstOrDefault();
				if (process != null) process.Kill();
			}
		}

		#endregion

		#region PRIVATE METHODS

		private static void StartStorageEmulator(string argument, string fileName)
		{
			var start = new ProcessStartInfo
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				Arguments = argument,
				FileName = fileName
			};
			var exitCode = ExecuteProcess(start);
			if (exitCode != 0)
			{
				var message = string.Format(
					"Error {0} executing {1} {2}",
					exitCode,
					start.FileName,
					start.Arguments);
				throw new InvalidOperationException(message);
			}
		}

		private static int ExecuteProcess(ProcessStartInfo start)
		{
			int exitCode;
			using (var proc = new Process { StartInfo = start })
			{
				proc.Start();
				proc.WaitForExit();
				exitCode = proc.ExitCode;
			}
			return exitCode;
		}

		#endregion
	}
}
