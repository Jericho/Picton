// Install addins.
#addin "nuget:?package=Polly"
#addin "nuget:?package=Cake.Coveralls""

// Install tools.
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=GitReleaseManager"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=coveralls.io"

// Using statements
using Polly;


///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");


///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var libraryName = "Picton";
var gitHubRepo = "Picton";

var testCoverageFilter = "+[Picton]* -[Picton]Picton.Properties.*";
var testCoverageExcludeByAttribute = "*.ExcludeFromCodeCoverage*";
var testCoverageExcludeByFile = "*/*Designer.cs;*/*AssemblyInfo.cs";

var nuGetApiUrl = EnvironmentVariable("NUGET_API_URL");
var nuGetApiKey = EnvironmentVariable("NUGET_API_KEY");
var gitHubUserName = EnvironmentVariable("GITHUB_USERNAME");
var gitHubPassword = EnvironmentVariable("GITHUB_PASSWORD");

var solutions = GetFiles("./Source/*.sln");
var solutionPaths = solutions.Select(solution => solution.GetDirectory());
var unitTestsPaths = GetDirectories("./Source/*.UnitTests");
var outputDir = "./artifacts/";
var codeCoverageDir = outputDir + "CodeCoverage/";
var versionInfo = GitVersion(new GitVersionSettings() { OutputType = GitVersionOutput.Json });
var milestone = string.Concat("v", versionInfo.MajorMinorPatch);
var cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();
var isLocalBuild = BuildSystem.IsLocalBuild;
var isMainBranch = StringComparer.OrdinalIgnoreCase.Equals("master", BuildSystem.AppVeyor.Environment.Repository.Branch);
var	isMainRepo = StringComparer.OrdinalIgnoreCase.Equals(gitHubUserName + "/" + gitHubRepo, BuildSystem.AppVeyor.Environment.Repository.Name);
var	isPullRequest = BuildSystem.AppVeyor.Environment.PullRequest.IsPullRequest;
var	isTagged = (
	BuildSystem.AppVeyor.Environment.Repository.Tag.IsTag &&
	!string.IsNullOrWhiteSpace(BuildSystem.AppVeyor.Environment.Repository.Tag.Name)
);


///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
	if (isMainBranch && (context.Log.Verbosity != Verbosity.Diagnostic))
	{
		Information("Increasing verbosity to diagnostic.");
		context.Log.Verbosity = Verbosity.Diagnostic;
	}

	Information("Building version {0} of {1} ({2}, {3}) using version {4} of Cake",
		versionInfo.LegacySemVerPadded,
		libraryName,
		configuration,
		target,
		cakeVersion
	);

	Information("Variables:\r\n\tLocalBuild: {0}\r\n\tIsMainBranch: {1}\r\n\tIsMainRepo: {2}\r\n\tIsPullRequest: {3}\r\n\tIsTagged: {4}",
		isLocalBuild,
		isMainBranch,
		isMainRepo,
		isPullRequest,
		isTagged
	);

	Information("Nuget Info:\r\n\tApi Url: {0}\r\n\tApi Key: {1}",
		nuGetApiUrl,
		string.IsNullOrEmpty(nuGetApiKey) ? "[NULL]" : new string('*', nuGetApiKey.Length)
	);

	Information("GitHub Info:\r\n\tRepo: {0}\r\n\tUserName: {1}\r\n\tPassword: {2}",
		gitHubRepo,
		gitHubUserName,
		string.IsNullOrEmpty(gitHubPassword) ? "[NULL]" : new string('*', gitHubPassword.Length)
	);
});

Teardown(context =>
{
	// Executed AFTER the last task.
	Information("Finished running tasks.");
});


///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
	.Does(() =>
{
	// Clean solution directories.
	foreach(var path in solutionPaths)
	{
		Information("Cleaning {0}", path);
		CleanDirectories(path + "/*/bin/" + configuration);
		CleanDirectories(path + "/*/obj/" + configuration);
	}

	// Clean previous artifacts
	Information("Cleaning {0}", outputDir);
	if (DirectoryExists(outputDir)) CleanDirectories(MakeAbsolute(Directory(outputDir)).FullPath);
	else CreateDirectory(outputDir);

	// Create folder for code coverage report
	CreateDirectory(codeCoverageDir);
});

Task("Restore-NuGet-Packages")
	.IsDependentOn("Clean")
	.Does(() =>
{
	// Restore all NuGet packages.
	foreach(var solution in solutions)
	{
		var maxRetryCount = 5;
		var toolTimeout = 1d;

		Information("Restoring {0}...", solution);

		Policy
			.Handle<Exception>()
			.Retry(maxRetryCount, (exception, retryCount, context) => {
				if (retryCount == maxRetryCount)
				{
					throw exception;
				}
				else
				{
					Verbose("{0}", exception);
					toolTimeout += 0.5;
				}})
			.Execute(()=> {
				NuGetRestore(solution, new NuGetRestoreSettings {
					Source = new List<string> {
						"https://api.nuget.org/v3/index.json",
						"https://www.myget.org/F/roslyn-nightly/api/v3/index.json"
					},
					ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
				});
			});
	}
});

Task("Update-Asembly-Version")
	.Does(() =>
{
	GitVersion(new GitVersionSettings()
	{
		UpdateAssemblyInfo = true,
		OutputType = GitVersionOutput.BuildServer
	});
});

Task("Build")
	.IsDependentOn("Restore-NuGet-Packages")
	.IsDependentOn("Update-Asembly-Version")
	.Does(() =>
{
	// Build all solutions.
	foreach(var solution in solutions)
	{
		Information("Building {0}", solution);
		MSBuild(solution, new MSBuildSettings()
			.SetPlatformTarget(PlatformTarget.MSIL)
			.SetConfiguration(configuration)
			.SetVerbosity(Verbosity.Minimal)
			.SetNodeReuse(false)
			.WithProperty("Windows", "True")
			.WithProperty("TreatWarningsAsErrors", "True")
			.WithTarget("Build")
		);
	}
});

Task("Run-Unit-Tests")
	.IsDependentOn("Build")
	.Does(() =>
{
	foreach(var path in unitTestsPaths)
	{
		Information("Running unit tests in {0}...", path);
		VSTest(path + "/bin/" + configuration + "/*.UnitTests.dll");
	}
});

Task("Run-Code-Coverage")
	.IsDependentOn("Build")
	.Does(() =>
{
	var testAssemblyPath = string.Format("./Source/{0}.UnitTests/bin/{1}/{0}.UnitTests.dll", libraryName, configuration);
	var vsTestSettings = new VSTestSettings();
	if (AppVeyor.IsRunningOnAppVeyor) vsTestSettings.ArgumentCustomization = args => args.Append("/logger:Appveyor");

	OpenCover(
		tool => { tool.VSTest(testAssemblyPath, vsTestSettings); },
		new FilePath(codeCoverageDir + "coverage.xml"),
		new OpenCoverSettings() { ReturnTargetCodeOffset = 0 }
			.WithFilter(testCoverageFilter)
			.ExcludeByAttribute(testCoverageExcludeByAttribute)
			.ExcludeByFile(testCoverageExcludeByFile)
	);
});

Task("Upload-Coverage-Result")
	.Does(() =>
{
	CoverallsIo(codeCoverageDir + "coverage.xml");
});

Task("Generate-Code-Coverage-Report")
	.IsDependentOn("Run-Code-Coverage")
	.Does(() =>
{
	ReportGenerator(
		codeCoverageDir + "coverage.xml",
		codeCoverageDir,
		new ReportGeneratorSettings() {
			ClassFilters = new[] { "*.UnitTests*" }
		}
	);
});

Task("Create-NuGet-Package")
	.IsDependentOn("Build")
	.Does(() =>
{
	var settings = new NuGetPackSettings
	{
		Id                      = libraryName,
		Version                 = versionInfo.NuGetVersionV2,
		Title                   = libraryName,
		Authors                 = new[] { "Jeremie Desautels" },
		Owners                  = new[] { "Jeremie Desautels" },
		Description             = "The Picton library for Azure",
		Summary                 = "Among other things, it contains extension methods and abstrations for StorageAccount, BlobClient, QueueClient, etc.",
		ProjectUrl              = new Uri("https://github.com/Jericho/Picton"),
		IconUrl                 = new Uri("https://github.com/identicons/jericho.png"),
		LicenseUrl              = new Uri("http://jericho.mit-license.org"),
		Copyright               = "Copyright (c) 2016 Jeremie Desautels",
		ReleaseNotes            = new [] { "" },
		Tags                    = new [] { "Picton", "Azure" },
		RequireLicenseAcceptance= false,
		Symbols                 = false,
		NoPackageAnalysis       = true,
		Dependencies            = new [] {
			new NuSpecDependency { Id = "Newtonsoft.Json", Version = "9.0.1" },
			new NuSpecDependency { Id = "WindowsAzure.Storage", Version = "7.1.2" }
		},
		Files                   = new [] {
			new NuSpecContent { Source = libraryName + ".45/bin/" + configuration + "/" + libraryName + ".dll", Target = "lib/net45" },
			new NuSpecContent { Source = libraryName + ".451/bin/" + configuration + "/" + libraryName + ".dll", Target = "lib/net451" },
			new NuSpecContent { Source = libraryName + ".452/bin/" + configuration + "/" + libraryName + ".dll", Target = "lib/net452" },
			new NuSpecContent { Source = libraryName + ".46/bin/" + configuration + "/" + libraryName + ".dll", Target = "lib/net46" },
			new NuSpecContent { Source = libraryName + ".461/bin/" + configuration + "/" + libraryName + ".dll", Target = "lib/net461" },
			new NuSpecContent { Source = libraryName + ".462/bin/" + configuration + "/" + libraryName + ".dll", Target = "lib/net462" }
		},
		BasePath                = "./Source/",
		OutputDirectory         = outputDir,
		ArgumentCustomization   = args => args.Append("-Prop Configuration=" + configuration)
	};
			
	NuGetPack(settings);
});

Task("Upload-AppVeyor-Artifacts")
	.WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
	.Does(() =>
{
	foreach (var file in GetFiles(outputDir + "*.*"))
	{
		AppVeyor.UploadArtifact(file.FullPath);
	}
});

Task("Publish-NuGet")
	.IsDependentOn("Create-NuGet-Package")
	.WithCriteria(() => !isLocalBuild)
	.WithCriteria(() => !isPullRequest)
	.WithCriteria(() => isMainRepo)
	.WithCriteria(() => isMainBranch)
	.WithCriteria(() => isTagged)
	.Does(() =>
{
	if(string.IsNullOrEmpty(nuGetApiKey)) throw new InvalidOperationException("Could not resolve NuGet API key.");
	if(string.IsNullOrEmpty(nuGetApiUrl)) throw new InvalidOperationException("Could not resolve NuGet API url.");

	foreach(var package in GetFiles(outputDir + "*.nupkg"))
	{
		// Push the package.
		NuGetPush(package, new NuGetPushSettings {
			ApiKey = nuGetApiKey,
			Source = nuGetApiUrl
		});
	}
});

Task("Create-Release-Notes")
	.Does(() =>
{
	if(string.IsNullOrEmpty(gitHubUserName)) throw new InvalidOperationException("Could not resolve GitHub user name.");
	if(string.IsNullOrEmpty(gitHubPassword)) throw new InvalidOperationException("Could not resolve GitHub password.");

	GitReleaseManagerCreate(gitHubUserName, gitHubPassword, gitHubUserName, gitHubRepo, new GitReleaseManagerCreateSettings {
		Name              = milestone,
		Milestone         = milestone,
		Prerelease        = true,
		TargetCommitish   = "master"
	});
});

Task("Publish-GitHub-Release")
	.WithCriteria(() => !isLocalBuild)
	.WithCriteria(() => !isPullRequest)
	.WithCriteria(() => isMainRepo)
	.WithCriteria(() => isMainBranch)
	.WithCriteria(() => isTagged)
	.Does(() =>
{
	if(string.IsNullOrEmpty(gitHubUserName)) throw new InvalidOperationException("Could not resolve GitHub user name.");
	if(string.IsNullOrEmpty(gitHubPassword)) throw new InvalidOperationException("Could not resolve GitHub password.");

	GitReleaseManagerClose(gitHubUserName, gitHubPassword, gitHubUserName, gitHubRepo, milestone);
});


///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
	.IsDependentOn("Run-Unit-Tests")
	.IsDependentOn("Create-NuGet-Package");

Task("Coverage")
	.IsDependentOn("Generate-Code-Coverage-Report")
	.Does(() =>
{
	StartProcess("cmd", "/c start " + codeCoverageDir + "index.htm");
});

Task("ReleaseNotes")
	.IsDependentOn("Create-Release-Notes"); 

Task("AppVeyor")
	.IsDependentOn("Run-Code-Coverage")
	.IsDependentOn("Upload-Coverage-Result")
	.IsDependentOn("Create-NuGet-Package")
	.IsDependentOn("Upload-AppVeyor-Artifacts")
	.IsDependentOn("Publish-NuGet")
	.IsDependentOn("Publish-GitHub-Release");

Task("Default")
	.IsDependentOn("Package");


///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
