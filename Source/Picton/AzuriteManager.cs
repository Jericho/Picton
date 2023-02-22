using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Picton
{
	/// <summary>
	/// Starts and stops the Azurite emulator.
	/// This is particularly useful when you need an environment to execute your integration tests.
	/// </summary>
	public class AzuriteManager : IDisposable
	{
		private Process _process;

		/// <summary>
		/// Initializes a new instance of the <see cref="AzuriteManager"/> class.
		/// </summary>
		public AzuriteManager()
		{
			StartEmulator();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			// Call 'Dispose' to release resources
			Dispose(true);

			// Tell the GC that we have done the cleanup and there is nothing left for the Finalizer to do
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			StopEmulator();
		}

		private static string LaunchVswhere(string arguments)
		{
			const string VswhereRelativePath = @"Microsoft Visual Studio\Installer\vswhere.exe";

			var start = new ProcessStartInfo
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				Arguments = arguments,
				RedirectStandardOutput = true,
				FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), VswhereRelativePath)
			};

			var exitCode = 0;
			var retMessage = string.Empty;

			using (var proc = new Process { EnableRaisingEvents = true, StartInfo = start })
			using (var mreOut = new ManualResetEvent(false))
			{
				proc.Start();
				proc.OutputDataReceived += (o, e) => { if (e.Data == null) mreOut.Set(); else retMessage = e.Data; };
				proc.BeginOutputReadLine();

				proc.WaitForExit();

				mreOut.WaitOne();

				exitCode = proc.ExitCode;
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

			return retMessage;
		}

		private void StartEmulator()
		{
			var azuriteLocation = LaunchVswhere("-find **\\azurite.exe");
			if (string.IsNullOrEmpty(azuriteLocation)) throw new Exception("Unable to locate Azurite on this machine");

			var start = new ProcessStartInfo
			{
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = true,
				Arguments = "--skipApiVersionCheck",
				FileName = azuriteLocation,
			};

			_process = Process.Start(start);
		}

		private void StopEmulator()
		{
			if (_process == null) return;

			try
			{
				_process.Kill();
				_process.WaitForExit();
				_process.Dispose();
				_process = null;
			}
			catch
			{
				// Intentionally left blank
			}
		}
	}
}
