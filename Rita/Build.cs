using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Microsoft.Build.Framework;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using Nuke.Common.CI.GitHubActions;

[GitHubActions(
    "ci",
    GitHubActionsImage.UbuntuLatest,
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.MacOsLatest,
    OnPushBranches = new[] {"main"},
    OnPullRequestBranches = new[] {"main"},
    ImportSecrets = [nameof(NuGetAPIKey)]
    )]


class Build : NukeBuild
{
    
    public static int Main() => Execute<Build>(x => x.Init, x => x.Deploy);

    [Solution]
    readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild
        ? Configuration.Debug
        : Configuration.Release;

    [Parameter("API Key for NuGet")] [Secret] readonly string NuGetAPIKey;
    RuntimeConfig runtimes = new RuntimeConfig();
    AbsolutePathList paths;

    Target Init =>
        _ =>
            _.Before(Clean)
                .Executes(() =>
                {
                    paths = new AbsolutePathList(NukeBuild.RootDirectory);

                    Log.Information($"The Secret key is: {NuGetAPIKey}");

                    runtimes.Add(new Runtime("win-x64"));
                    runtimes.Add(new Runtime("linux-x64"));
                    runtimes.Add(new Runtime("linux-arm64"));
                    runtimes.Add(new Runtime("linux-arm")); // 32-bit ARM
                    runtimes.Add(new Runtime("osx-x64")); // Intel-based macOS
                    runtimes.Add(new Runtime("osx-arm64")); // Apple Silicon macOS
                    

                    foreach (var managedPath in paths.Values)
                    {
                        paths.EnsureDirectory(managedPath.Path, managedPath.Rule);
                    }
                });

    Target Clean =>
        _ =>
            _.Before(Restore)
                .Executes(() =>
                {
                    Log.Information("Cleaning output...");

                    try
                    {
                        DotNetTasks.DotNetClean(s => s.SetProject(Solution));
                        Log.Information("Cleaning successful...");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                });

    Target Restore =>
        _ =>
            _.DependsOn(Clean)
                .Executes(() =>
                {
                    Log.Information("Restoring Packages...");
                    try
                    {
                        DotNetTasks.DotNetRestore(s => s.SetProjectFile(Solution));
                        Log.Information("Packages restored successfully!");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                });

    Target Test =>
        _ =>
            _.DependsOn(Restore)
                .Executes(() =>
                {
                    DotNetTasks.DotNetTest(_ =>
                        _.SetProjectFile(paths.GetPathForPhase(Phase.Test))
                    );
                });

    Target Compile =>
        _ =>
            _.DependsOn(Test)
                .Executes(() =>
                {
                    try
                    {   
                        

                        foreach (var runtime in runtimes)
                        {
                            var projectPath = paths.ProvidePath(runtime, Phase.Build);
                            var outputDirectory = paths.ProvidePath(runtime, Phase.Compile);
                            
                            

                            Log.Information($"Compiling the program for {runtime.dotNetIdentifier}...");


                        DotNetTasks.DotNetPublish(s =>
                            s.SetProject(projectPath)
                                .AddProperty("IncludeNativeLibrariesForSelfExtract", true)
                                .AddProperty("PublishSelfContained", true)
                                .AddProperty("AssemblyName", "ShellyApp")
                                .SetRuntime(runtime.dotNetIdentifier)
                                .SetConfiguration("Release")
                                .EnablePublishSingleFile()
                                .SetOutput(outputDirectory));
                        

                        // File.Copy(sourceConfigFile, configFileDestination);
                        
                        Log.Information(
                            "Compilation outputs are directed to: {0}",
                            outputDirectory
                            
                        
                        );
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                        throw;
                    }
                });

    Target Compress =>
        _ =>
            _.DependsOn(Compile)
                .Executes(() =>
                {
                
                   foreach (var runtime in runtimes)
                   {
                    var outputDirectory = paths.ProvidePath(runtime, Phase.Compile);
                    var zipFilePath = Path.ChangeExtension(paths.ProvidePath(runtime, Phase.Zip), ".zip");

                    Log.Information($"Compressing output for {runtime.dotNetIdentifier}");

                    ZipFile.CreateFromDirectory(outputDirectory, zipFilePath);
                    
                    Log.Information($"Application compressed successfully into {zipFilePath}");
            
                   }
                });

    Target Deploy =>
        _ =>
            _.DependsOn(Compress)
                .Executes(() =>
                {
                    
                    foreach (var runtime in runtimes)
                    {

                        var zipFilePath = Path.ChangeExtension(paths.ProvidePath(runtime, Phase.Zip), ".zip");
                        var targetPath = Path.ChangeExtension(paths.ProvidePath(runtime, Phase.Deploy), ".zip");

                        Log.Information($"Deploying {zipFilePath} to {targetPath}");

                        CopyFile(zipFilePath, targetPath, FileExistsPolicy.Overwrite);

                        if (File.Exists(zipFilePath))
                        {
                            Log.Information($"Application deployed successfully to {targetPath}");

                        }
                        else 
                        {
                            Log.Error($"Failed to find compressed file: {zipFilePath}");
                        }
                    }
                });
}
