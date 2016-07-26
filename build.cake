#tool "nuget:?package=GitReleaseNotes"
#tool "nuget:?package=GitVersion.CommandLine"


///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");


///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

// It is important to filter out files under the 'packages' folder because some
// package may have sample solutions. For example, OpenCover contains a sample
// solution called BomSample.sln
var solutions = GetFiles("./**/*.sln")
	.Where(file => !file.ToString().Contains("/packages/"));
var solutionPaths = solutions
	.Select(solution => solution.GetDirectory())
	.Where(directory => !directory.ToString().Contains("/packages/"));
var unitTestsPaths = GetDirectories("./*.UnitTests");
var outputDir = "./artifacts/";
var versionInfo = (GitVersion)null;


///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
	// Executed BEFORE the first task.
	Information("Running tasks...");
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
		CleanDirectories(path + "/**/bin/" + configuration);
		CleanDirectories(path + "/**/obj/" + configuration);
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
		Information("Restoring {0}...", solution);
		NuGetRestore(solution);
	}
});

Task("Version")
	.Does(() =>
{
	// We have to invoke GetVersion twice: the first time to update
	// AssemblyInfo.cs and a second time to get the values in our C# code
	GitVersion(new GitVersionSettings()
	{
		UpdateAssemblyInfo = true,
		OutputType = GitVersionOutput.BuildServer
	});
	versionInfo = GitVersion(new GitVersionSettings()
	{
		UpdateAssemblyInfo = false
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
		MSBuild(solution, settings =>
			settings.SetPlatformTarget(PlatformTarget.MSIL)
				.WithProperty("TreatWarningsAsErrors","true")
				.WithTarget("Build")
				.SetConfiguration(configuration));
	}
});

Task("Unit-Tests")
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
//	.IsDependentOn("Unit-Tests")
	.IsDependentOn("Version")
	.Does(() =>
{
	var files = GetFiles("./Picton.Common/bin/" + configuration + "/Picton.Common.dll")
		.Select(file => new NuSpecContent {Source = file.ToString(), Target = "bin"})
		.ToArray();

	var settings = new NuGetPackSettings
	{
		Id                      = "Picton.Common",
		Version                 = versionInfo.NuGetVersionV2,
		Title                   = "The Picton library for Azure",
		Authors                 = new[] {"Jeremie Desautels"},
		Owners                  = new[] {""},
		Description             = "The description of the package",
		Summary                 = "Excellent summary of what the package does",
		ProjectUrl              = new Uri("https://github.com/SomeUser/TestNuget/"),
		IconUrl                 = new Uri("http://cdn.rawgit.com/SomeUser/TestNuget/master/icons/testnuget.png"),
		LicenseUrl              = new Uri("https://github.com/SomeUser/TestNuget/blob/master/LICENSE.md"),
		Copyright               = "Some company 2015",
		ReleaseNotes            = new [] {"Initial release"},
		Tags                    = new [] {"Picton", "Azure"},
		RequireLicenseAcceptance= false,
		Symbols                 = false,
		NoPackageAnalysis       = true,
		Files                   = files,
		BasePath                = "./src/TestNuget/bin/release",
		OutputDirectory         = outputDir,
		ArgumentCustomization   = args => args.Append("-Prop Configuration=" + configuration)
	};
			
	NuGetPack(settings);

	// TODO not sure why this isn't working
		// GitReleaseNotes("outputDir/releasenotes.md", new GitReleaseNotesSettings {
		//     WorkingDirectory         = ".",
		//     AllTags                  = false
		// });
		var releaseNotesExitCode = StartProcess(
			@"tools\GitReleaseNotes\tools\gitreleasenotes.exe", 
			new ProcessSettings { Arguments = ". /o artifacts/releasenotes.md" });
		if (string.IsNullOrEmpty(System.IO.File.ReadAllText("./artifacts/releasenotes.md")))
			System.IO.File.WriteAllText("./artifacts/releasenotes.md", "No issues closed since last release");

		if (releaseNotesExitCode != 0) throw new Exception("Failed to generate release notes");

		System.IO.File.WriteAllLines(outputDir + "artifacts", new[]{
			"nuget:Picton.Common." + versionInfo.NuGetVersion + ".nupkg",
			"nugetSymbols:Picton.Common." + versionInfo.NuGetVersion + ".symbols.nupkg",
			"releaseNotes:releasenotes.md"
		});

		if (AppVeyor.IsRunningOnAppVeyor)
		{
			foreach (var file in GetFiles(outputDir + "**/*"))
				AppVeyor.UploadArtifact(file.FullPath);
		}
});


///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Default")
	.Description("This is the default task which will be ran if no specific target is passed in.")
	.IsDependentOn("Package");


///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
