using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
	/// Storage Emulator:
	/// - 2.0 was released with SDK 2.2 in September 2013
	/// - 3.0 was released with SDK 2.3 in May 2014
	/// - 3.3 was released with SDK 2.4 in August 2014
	/// - 3.4 was released with SDK 2.5 in November 2014
	/// - 4.0 was released with SDK 2.6 in May 2015
	/// - 4.1 was released with SDK 2.7 in August 2015
	/// - 4.2 was released with SDK 2.8 in November 2015
	/// - 4.3 was released with SDK 2.9 in April 2016
	/// - 4.4 was released with SDK 2.9.1 in May 2016
	/// - 4.5 was released with SDK 2.9.5 in August 2016
	/// - 4.6 was released with in November 2016 as a separate download, not as part of SDK 2.9.6
	/// - 5.0 was released with SDK 3.0.0 in March 2017
	/// - 5.1 was released with SDK 3.0.1 in May 2017
	/// - 5.3 was released in December 2017 as a separate download
	/// - 5.8 was released in October 2018 as a separate download
	/// - 5.9 was released in December 2018 as a separate download
	/// - 5.10 was released in August 2019 as a separate download
	///
	/// Storage emulator can be downloaded <a href="https://go.microsoft.com/fwlink/?linkid=717179&amp;clcid=0x409">here</a>.
	///
	/// Azure Storage Emulator is now deprecated and Azurite is the now prefered emulator.
	/// </summary>
	/// <remarks>Inspired by <a href="http://stackoverflow.com/questions/7547567/how-to-start-azure-storage-emulator-from-within-a-program">this StackOverflow discussion</a>.</remarks>
	public static class AzureEmulatorManager
	{
		private class EmulatorVersionInfo
		{
			/// <summary>
			/// Gets the emulator version.
			/// </summary>
			public int Version { get; private set; }

			/// <summary>
			/// Gets the array containing the various possible process names for a given version of the emulator.
			/// The process name is not always the same on different platforms. For instance, "WAStorageEmulator" is named "WASTOR~1" on Windows 8.
			/// That's why we need an array of strings to store the various names rather than a simple string.
			/// </summary>
			public string[] ProcessNames { get; private set; }

			/// <summary>
			/// Gets the path where the emulator executable is located.
			/// </summary>
			public string ExecutablePath { get; private set; }

			/// <summary>
			/// Gets the parameters expected by the emulator when starting.
			/// </summary>
			public string StartParameters { get; private set; }

			/// <summary>
			/// Gets the parameters expected by the emulator when stoping.
			/// </summary>
			public string StopParameters { get; private set; }

			public bool RequireElevated { get; private set; }

			public EmulatorVersionInfo(int version, IEnumerable<string> processNames, string executablePath, string startParameters, string stopParameters, bool requireElevated)
			{
				Version = version;
				ProcessNames = processNames.ToArray();
				ExecutablePath = executablePath;
				StartParameters = startParameters;
				StopParameters = stopParameters;
				RequireElevated = requireElevated;
			}
		}

		#region FIELDS

		private static readonly IList<EmulatorVersionInfo> _storageEmulatorVersions = new List<EmulatorVersionInfo>();
		private static readonly IList<EmulatorVersionInfo> _documentDbEmulatorVersions = new List<EmulatorVersionInfo>();

		#endregion

		#region CONSTRUCTOR

		static AzureEmulatorManager()
		{
			_storageEmulatorVersions.Add(new EmulatorVersionInfo(2, new[] { "DSService" }, @"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe", "/devstore:start", "/devstore:stop", false));
			_storageEmulatorVersions.Add(new EmulatorVersionInfo(3, new[] { "WAStorageEmulator", "WASTOR~1" }, @"C:\Program Files (x86)\Microsoft SDKs\Windows Azure\Storage Emulator\WAStorageEmulator.exe", "start", "stop", false));
			_storageEmulatorVersions.Add(new EmulatorVersionInfo(4, new[] { "AzureStorageEmulator" }, @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe", "start", "stop", false));

			_documentDbEmulatorVersions.Add(new EmulatorVersionInfo(0, new[] { "DocumentDB.Emulator" }, @"C:\Program Files\DocumentDB Emulator\DocumentDB.Emulator.exe", string.Empty, "/shutdown", true));
			_documentDbEmulatorVersions.Add(new EmulatorVersionInfo(1, new[] { "DocumentDB.GatewayService" }, @"C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe", string.Empty, "/shutdown", true));
		}

		#endregion

		#region PUBLIC METHODS

		/// <summary>
		/// Start the most recent version of the storage emulator if not already started.
		/// </summary>
		public static void EnsureStorageEmulatorIsStarted()
		{
			// The storage emulator process completes quickly therefore no need to set a wait timeout
			var waitTimeout = TimeSpan.Zero;

			EnsureEmulatorIsStarted(_storageEmulatorVersions, waitTimeout);
		}

		/// <summary>
		/// Stop the storage emulator if running.
		/// </summary>
		public static void StopStorageEmulator()
		{
			EnsureEmulatorIsStoped(_storageEmulatorVersions, false);
		}

		/// <summary>
		/// Start the most recent version of the DocumentDb emulator if not already started.
		/// </summary>
		public static void EnsureDocumentDbEmulatorIsStarted()
		{
			// The DocumentDb emulator process never seems to complete (I don't know why), therefore we must set a reasonable wait timeout
			var waitTimeout = TimeSpan.FromSeconds(20);

			EnsureEmulatorIsStarted(_documentDbEmulatorVersions, waitTimeout);
		}

		/// <summary>
		/// Stop the DocumentDb emulator if running.
		/// </summary>
		public static void StopDocumentDbEmulator()
		{
			EnsureEmulatorIsStoped(_documentDbEmulatorVersions, true);
		}

		#endregion

		#region PRIVATE METHODS

		private static void EnsureEmulatorIsStarted(IEnumerable<EmulatorVersionInfo> emulatorVersions, TimeSpan waitTimeout)
		{
			var found = false;

			// Ordering emulators in reverse order is important.
			// We want to ensure we start the most recent version, even if an older version is available
			foreach (var emulatorVersion in emulatorVersions.OrderByDescending(x => x.Version))
			{
				if (File.Exists(emulatorVersion.ExecutablePath))
				{
					var count = 0;
					foreach (var processName in emulatorVersion.ProcessNames)
					{
						count += Process.GetProcessesByName(processName).Length;
					}

					if (count == 0) LaunchProcess(emulatorVersion.ExecutablePath, emulatorVersion.StartParameters, emulatorVersion.RequireElevated, waitTimeout);
					found = true;
					break;
				}
			}

			if (!found)
			{
				throw new FileNotFoundException("Unable to find the emulator on this computer");
			}
		}

		/// <summary>
		/// Stop the storage emulator if running.
		/// </summary>
		private static void EnsureEmulatorIsStoped(IEnumerable<EmulatorVersionInfo> emulatorVersions, bool elevated)
		{
			// Looping through all versions of the emulator to make sure we stop any version that might be running
			foreach (var emulatorVersion in emulatorVersions)
			{
				if (File.Exists(emulatorVersion.ExecutablePath))
				{
					var count = 0;
					foreach (var processName in emulatorVersion.ProcessNames)
					{
						count += Process.GetProcessesByName(processName).Length;
					}

					if (count > 0) LaunchProcess(emulatorVersion.ExecutablePath, emulatorVersion.StopParameters, elevated, TimeSpan.Zero);
				}
			}
		}

		private static void LaunchProcess(string fileName, string arguments, bool elevated, TimeSpan waitTimeout)
		{
			var start = new ProcessStartInfo
			{
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				Arguments = arguments,
				FileName = fileName
			};

			if (elevated)
			{
				start.Verb = "runas";
				start.UseShellExecute = true;
			}

			var exitCode = 0;

			using (var proc = new Process { EnableRaisingEvents = true, StartInfo = start })
			{
				proc.Start();

				if (waitTimeout == TimeSpan.Zero)
				{
					proc.WaitForExit();
					exitCode = proc.ExitCode;
				}
				else
				{
					var completed = proc.WaitForExit((int)waitTimeout.TotalMilliseconds);
					if (completed) exitCode = proc.ExitCode;
				}
			}

			if (exitCode != 0)
			{
				var message = string.Format(
					CultureInfo.InvariantCulture,
					"Error {0} executing {1} {2}",
					exitCode,
					start.FileName,
					start.Arguments);
				throw new InvalidOperationException(message);
			}
		}

		#endregion
	}
}
