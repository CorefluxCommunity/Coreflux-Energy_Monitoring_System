using System;
using System.IO;
using System.IO.Compression;
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

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Init, x => x.Deploy);

    public bool TestsSucceeded;

    [Solution]
    readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild
        ? Configuration.Debug
        : Configuration.Release;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    AbsolutePath TestDirectory => RootDirectory / "ProjectShelly.Tests" / "ProjectShelly.Tests.csproj";

    AbsolutePath BuildOutDirectory => ArtifactsDirectory / "buildout";

    AbsolutePath CompressedBuild => ArtifactsDirectory / "compressed";

    AbsolutePath ZipFilePath => CompressedBuild / "build.zip";

    AbsolutePath TargetPath => DeployZipDirectory / "DeployedBuild.zip";

    AbsolutePath ProjectPath => RootDirectory / "ProjectShelly" / "ProjectShelly.csproj";

    AbsolutePath DeployZipDirectory => ArtifactsDirectory / "deployed";

    Target Init =>
        _ =>
            _.Before(Clean)
                .Executes(() =>
                {
                    Log.Information(
                        "Ensuring the existence of {0}, {1}, {2}, {3}, {4}, {5}.",
                        ArtifactsDirectory,
                        BuildOutDirectory,
                        ZipFilePath,
                        CompressedBuild,
                        TargetPath,
                        DeployZipDirectory
                    );

                    if (Directory.Exists(ArtifactsDirectory))
                    {
                        Log.Information(
                            "Directory {0} already exists and will be cleared.",
                            ArtifactsDirectory
                        );
                    }
                        Directory.CreateDirectory(ArtifactsDirectory);

                    if (Directory.Exists(BuildOutDirectory ))
                    {
                        Log.Information(
                            "Directory {0} already exists and will be cleared.",
                            BuildOutDirectory
                        );
                        Directory.Delete(BuildOutDirectory, true);
                    }
                
                    Directory.CreateDirectory(BuildOutDirectory);

                    if (Directory.Exists(CompressedBuild))
                    {
                        Directory.Delete(CompressedBuild, true);
                    }

                    Directory.CreateDirectory(CompressedBuild);
    
                    if (File.Exists(ZipFilePath))
                    {
                        Log.Information(
                            "Zip file already exists and will be overwritten at {0}",
                            ZipFilePath
                        );

                        File.Delete(ZipFilePath);
                    }

                    if (File.Exists(TargetPath))
                    {
                        Log.Information("Deployed file will be overwritten at {0}.", TargetPath);

                        File.Delete(TargetPath);
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
                        _.SetProjectFile(TestDirectory)
                    );
                });

    Target Compile =>
        _ =>
            _.DependsOn(Test)
                .Executes(() =>
                {
                    try
                    {
                        DotNetTasks.DotNetPublish(s =>
                            s.SetProject(ProjectPath)
                                .AddProperty("IncludeNativeLibrariesForSelfExtract", true)
                                //.AddProperty("CopyLocalLockFileAssemblies", false)
                                .AddProperty("PublishSelfContained", true)
                                .AddProperty("AssemblyName", "linkin")
                                .SetRuntime("win-x64")
                                .SetConfiguration("Release")
                                .EnablePublishSingleFile()
                                //.EnableSelfContained()
                                .SetOutput(BuildOutDirectory)
                        );

                        Log.Information(
                            "Compilation outputs are directed to: {0}",
                            BuildOutDirectory
                        );
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
                    try
                    {
                        if (Directory.Exists(BuildOutDirectory))
                        {
                            ZipFile.CreateFromDirectory(BuildOutDirectory, ZipFilePath);
                            Log.Information(
                                "Application compressed successfully into {0}",
                                ZipFilePath
                            );
                        }
                        else
                        {
                            Log.Warning("Source directory does not exist: {0}", BuildOutDirectory);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                        throw;
                    }
                });

    Target Deploy =>
        _ =>
            _.DependsOn(Compress)
                .Executes(() =>
                {
                    try
                    {
                        CopyFile(ZipFilePath, TargetPath, FileExistsPolicy.Overwrite);

                        Log.Information("Application deployed Successfully to {0}", TargetPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Deployment Failed: {ex.Message}");
                        throw;
                    }
                });
}
