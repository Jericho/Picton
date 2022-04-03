using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Picton
{
	public class AzuriteManager : IDisposable
	{
		private Process _process;

		public AzuriteManager()
		{
			StartEmulator();
		}

		public void Dispose()
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
			using (ManualResetEvent mreOut = new ManualResetEvent(false))
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
				Verb = "runas"
			};

			_process = Process.Start(start);
		}

		private void StopEmulator()
		{
			try
			{
				_process.Kill();
				_process.WaitForExit();
			}
			catch
			{
			}
		}
	}
}
