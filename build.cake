#tool "GitVersion.CommandLine"
#tool "DocCreator"
#tool "xunit.runner.console"
#addin "Cake.DocCreator"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Publish");
var configuration = Argument<string>("configuration", "Release");
var artifacts = Argument<string>("artifacts", "./artifacts/");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var solutionPath = File("./src/Config.Abstractions.sln");
var solution = ParseSolution(solutionPath);
var projects = solution.Projects;
var projectPaths = projects.Select(p => p.Path.GetDirectory());
var testAssemblies = projects.Where(p => p.Name.Contains("Test")).Select(p => p.Path.GetDirectory() + "/bin/" + configuration + "/" + p.Name + ".dll");
var testResultsPath = MakeAbsolute(Directory(artifacts + "./test-results"));
GitVersion versionInfo = null;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(() =>
{
    // Executed BEFORE the first task.
    Information("Running tasks...");
	versionInfo = GitVersion();
	Information("Building for version {0}", versionInfo.FullSemVer);
});

Teardown(() =>
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
    foreach(var path in projectPaths)
    {
        Information("Cleaning {0}", path);
        CleanDirectories(path + "/**/bin/" + configuration);
        CleanDirectories(path + "/**/obj/" + configuration);
    }
    Information("Cleaning common files...");
    CleanDirectory(artifacts);
});

Task("Restore")
    .Does(() =>
{
    // Restore all NuGet packages.
    Information("Restoring solution...");
    NuGetRestore(solutionPath);
});

Task("Version")
	.Does(() => {
		Information("Bumping AssemblyInfo.cs versions");
		GitVersion(new GitVersionSettings {
			UpdateAssemblyInfo = true
		});
	});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
	.IsDependentOn("Version")
    .Does(() =>
{
	Information("Building solution...");
	MSBuild(solutionPath, settings =>
		settings.SetPlatformTarget(PlatformTarget.MSIL)
			.WithProperty("TreatWarningsAsErrors","true")
			.SetVerbosity(Verbosity.Quiet)
			.WithTarget("Build")
			.SetConfiguration(configuration));
});

Task("Copy-Files")
    .IsDependentOn("Build")
    .Does(() =>
{
    CreateDirectory(artifacts + "build");
	foreach (var project in projects) {
		CreateDirectory(artifacts + "build/" + project.Name);
		var files = GetFiles(project.Path.GetDirectory() + "/bin/" + configuration +"/" + project.Name + ".*");
		Information("");
		CopyFiles(files, artifacts + "build/" + project.Name);
	}
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    CreateDirectory(testResultsPath);

    var settings = new XUnit2Settings {
        NoAppDomain = true,
		XmlReport = true,
		HtmlReport = true,
        OutputDirectory = testResultsPath,
    };
    settings.ExcludeTrait("Category", "Integration");

    XUnit2(testAssemblies, settings);
});

///////////////////////////////////////////////////////////////////////////////
// NUGET TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
	.IsDependentOn("Copy-Files")
	.Does(() => {
		var outDir = artifacts + "./package";
		CreateDirectory(outDir);
		var ca = "Configuration.Abstractions";
		var specs = GetFiles("./*.nuspec");
		var nuGetPackSettings  = new NuGetPackSettings {
								Version				 = versionInfo.NuGetVersionV2,
								ReleaseNotes			= new [] { "Migrated to new repository and package format" },
								Symbols				 = false,
								NoPackageAnalysis	   = true,
								Files				   = new [] {
																	 new NuSpecContent { Source = ca + ".Net45/" + ca + ".Net45.dll", Target = "lib/net45" },
																	 new NuSpecContent { Source = ca + ".Portable/" + ca + ".Portable.dll", Target = "lib/dotnet5.4" },
																	 new NuSpecContent { 
																		 Source = ca + ".Portable/" + ca + ".Portable.dll",
																		 Target = "lib/portable-net45+netcore45+win8+wp81+dnxcore50"
																		 }
																  },
								BasePath				= artifacts + "build",
								OutputDirectory		 = outDir
							};
		NuGetPack(specs, nuGetPackSettings);
	});
///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Publish")
	.IsDependentOn("Package");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
