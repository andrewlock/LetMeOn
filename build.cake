// Target - The task you want to start. Runs the Default task if not specified.
var target = Argument("Target", "Default");
var configuration = Argument("Configuration", "Release");

// Set the package version based on an environment variable 
var packageVersion = 
    HasArgument("PackageVersion") ? Argument<string>("PackageVersion") :  
    !string.IsNullOrEmpty(EnvironmentVariable("PackageVersion")) ? EnvironmentVariable("PackageVersion") : "0.1.0";

// The build number to use in the version number of the built NuGet packages.
// There are multiple ways this value can be passed, this is a common pattern.
// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if running on AppVeyor, get it's build number.
// 3. Otherwise if running on Travis CI, get it's build number.
// 4. Otherwise if an Environment variable exists, use that.
// 5. Otherwise default the build number to 0.
var buildNumber =
    HasArgument("BuildNumber") ? Argument<int>("BuildNumber") :
    AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number :
    TravisCI.IsRunningOnTravisCI ? TravisCI.Environment.Build.BuildNumber :
    EnvironmentVariable("BuildNumber") != null ? int.Parse(EnvironmentVariable("BuildNumber")) : 1;
 
// Assume we're building on appveyor for publishing NuGets
// So always add a beta prefix if not doing a tag
var isTag = EnvironmentVariable("APPVEYOR_REPO_TAG") != null && EnvironmentVariable("APPVEYOR_REPO_TAG") == "true" ;
var revision = isTag ? null : "beta-" +buildNumber.ToString("D4");

Information($"Running target {target} in {configuration} configuration, version {packageVersion}{revision}");

var artifactsDirectory = Directory("./artifacts");
var distDirectory = Directory("./dist");
 
// Deletes the contents of the Artifacts folder if it should contain anything from a previous build.
Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);
    });
 
// Run dotnet restore to restore all package references.
Task("Restore")
    .Does(() =>
    {
        DotNetCoreRestore();
    });
 
// Find all sln files and build them using the build configuration specified as an argument.
 Task("Build")
    .Does(() =>
    {
        DotNetCoreBuild(
            ".",
            new DotNetCoreBuildSettings()
            {
                Configuration = configuration,
                ArgumentCustomization = args => args.Append($"--no-restore /p:Version={packageVersion}"),
            });
    });
 
// Look under a 'Tests' folder and run dotnet test against all of those projects.
// Then drop the XML test results file in the Artifacts folder at the root.
Task("Test")
    .Does(() =>
    {
        var projects = GetFiles("./test/**/*Tests.csproj");
        foreach(var project in projects)
        {
            Information("Testing project " + project);
            DotNetCoreTest(
                project.ToString(),
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = true,
                    ArgumentCustomization = args => args.Append($"--no-restore /p:Version={packageVersion}"),
                });
        }
    });
 
// Run dotnet pack to produce NuGet packages from our projects. Versions the package
// using the build number argument on the script which is used as the revision number 
// (Last number in 1.0.0.0). The packages are dropped in the Artifacts directory.
Task("Pack")
    .Does(() =>
    {
        DotNetCorePack(
            ".",
            new DotNetCorePackSettings()
            {
                Configuration = configuration,
                OutputDirectory = artifactsDirectory,
                VersionSuffix = revision,
                NoBuild = true,
                ArgumentCustomization = args => args.Append($"/p:PackageVersion={packageVersion}"),
            });
    });

Task("PublishWeb")
    .Does(() =>
    {
        DotNetCorePublish(
            "./src/LetMeOn/LetMeOn.csproj",
            new DotNetCorePublishSettings()
            {
                Configuration = configuration,
                OutputDirectory = distDirectory,
                VersionSuffix = revision,
                ArgumentCustomization = args => args.Append($"/p:Version={packageVersion}"),
            });
    });

// The default task to run if none is explicitly specified. In this case, we want
// to run everything starting from Clean, all the way up to Pack.
Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack")
    .IsDependentOn("PublishWeb");
 
// Executes the task specified in the target argument.
RunTarget(target);