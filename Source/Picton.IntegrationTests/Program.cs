using Formitable.BetterStack.Logger.Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.IntegrationTests
{
	public class Program
	{
		public static async Task<int> Main()
		{
			var source = new CancellationTokenSource();
			Console.CancelKeyPress += (s, e) =>
			{
				e.Cancel = true;
				source.Cancel();
			};

			var services = new ServiceCollection();
			ConfigureServices(services);
			using var serviceProvider = services.BuildServiceProvider();
			var app = serviceProvider.GetService<TestsRunner>();
			return await app.RunAsync(source.Token).ConfigureAwait(false);
		}

		private static void ConfigureServices(ServiceCollection services)
		{
			services
				.AddLogging(logging =>
				{
					var betterStackToken = Environment.GetEnvironmentVariable("BETTERSTACK_TOKEN");
					if (!string.IsNullOrEmpty(betterStackToken))
					{
						logging.AddBetterStackLogger(options =>
						{
							options.SourceToken = betterStackToken;
							options.Context["source"] = "Picton_integration_tests";
							options.Context["Picton-Version"] = typeof(CloudMessage).Assembly.GetName().Version.ToString(3);
						});
					}

					logging.AddSimpleConsole(options =>
					{
						options.SingleLine = true;
						options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
					});

					logging.AddFilter("*", LogLevel.Debug);
				})
				.AddTransient<TestsRunner>();
		}
	}
}
