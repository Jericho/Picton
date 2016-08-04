// Install addins.
#addin "nuget:?package=Polly&version=4.2.0"

// Install tools.
#tool "nuget:?package=GitReleaseNotes"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=gitreleasemanager&version=0.5.0"

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

var solutions = GetFiles("./*.sln");
var solutionPaths = solutions.Select(solution => solution.GetDirectory());
var unitTestsPaths = GetDirectories("./*.UnitTests");
var outputDir = "./artifacts/";
var versionInfo = GitVersion(new GitVersionSettings() { OutputType = GitVersionOutput.Json });
var libraryName = "Picton.Common";
var isMainBranch = StringComparer.OrdinalIgnoreCase.Equals("master", BuildSystem.AppVeyor.Environment.Repository.Branch);
var cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();


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
	.Description("Cleans all directories that are used during the build process.")
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
	if (DirectoryExists(outputDir))
	{
		CleanDirectories(MakeAbsolute(Directory(outputDir)).FullPath);
	}
	else
	{
		CreateDirectory(outputDir);
	}
});

Task("Restore")
	.Description("Restores all the NuGet packages that are used by the specified solution.")
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

Task("Version")
	.Does(() =>
{
	GitVersion(new GitVersionSettings()
	{
		UpdateAssemblyInfo = true,
		OutputType = GitVersionOutput.BuildServer
	});
});

Task("Build")
	.Description("Builds all the different parts of the project.")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("Version")
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

Task("UnitTests")
	.Description("Run the unit tests for the project.")
	.IsDependentOn("Build")
	.Does(() =>
{
	foreach(var path in unitTestsPaths)
	{
		Information("Running unit tests in {0}...", path);
		MSTest(path + "/bin/" + configuration + "/*.UnitTests.dll");
	}
});

Task("Package")
	.Description("Build the nuget package.")
	.IsDependentOn("UnitTests")
	.Does(() =>
{
	var settings = new NuGetPackSettings
	{
		Id                      = libraryName,
		Version                 = versionInfo.NuGetVersionV2,
		Title                   = "The Picton library for Azure",
		Authors                 = new[] { "Jeremie Desautels" },
		Owners                  = new[] { "Jeremie Desautels" },
		Description             = "Convenient library for Azure",
		Summary                 = "Among other things, it contains extension methods and abstrations for StorageAccount, BlobClient, QueueClient, etc.",
		ProjectUrl              = new Uri("https://github.com/Jericho/Picton.Common"),
		IconUrl                 = new Uri("https://github.com/identicons/jericho.png"),
		LicenseUrl              = new Uri("http://jericho.mit-license.org"),
		Copyright               = "Copyright (c) 2016 Jeremie Desautels",
		ReleaseNotes            = new [] { "Initial release" },
		Tags                    = new [] { "Picton", "Azure" },
		RequireLicenseAcceptance= false,
		Symbols                 = false,
		NoPackageAnalysis       = true,
		Dependencies            = new [] {
			new NuSpecDependency { Id = "Newtonsoft.Json", Version = "9.0.1" },
			new NuSpecDependency { Id = "WindowsAzure.Storage", Version = "7.1.2" }
		},
		Files                   = new [] {
			new NuSpecContent { Source = libraryName + "/bin/" + configuration + "/" + libraryName + ".dll", Target = "lib/net452" },
			new NuSpecContent { Source = libraryName + ".45/bin/" + configuration + "/" + libraryName + ".dll", Target = "lib/net45" }
		},
		BasePath                = "./",
		OutputDirectory         = outputDir,
		ArgumentCustomization   = args => args.Append("-Prop Configuration=" + configuration)
	};
			
	NuGetPack(settings);
});

Task("ReleaseNotes")
	.Description("Update the release notes.")
	.IsDependentOn("Clean")
	.Does(() =>
{

	GitReleaseNotes(outputDir + "/releasenotes.md", new GitReleaseNotesSettings {
		WorkingDirectory         = ".",
		AllLabels                = true,
		AllTags                  = true,
		Verbose                  = true
	});
});


Task("UploadArtifacts")
	.Description("Upload artifacts to AppVeyor.")
	.IsDependentOn("Package")
	.IsDependentOn("ReleaseNotes")
	.Does(() =>
{
	if (AppVeyor.IsRunningOnAppVeyor)
	{
		foreach (var file in GetFiles(outputDir))
			AppVeyor.UploadArtifact(file.FullPath);
	}
});


///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Default")
	.Description("This is the default task which will be ran if no specific target is passed in.")
	.IsDependentOn("UploadArtifacts");


///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
