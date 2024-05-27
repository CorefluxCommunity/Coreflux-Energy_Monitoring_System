using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Cloud.Deployment;
using Cloud.Interfaces;
using Cloud.Models;
using Cloud.Services;
using Microsoft.Build.Framework;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Renci.SshNet;
using Renci.SshNet.Security;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;



[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push],
    ImportSecrets = [nameof(ENERGY_SECRET)],
    AutoGenerate = false
)]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Init, x => x.Execution);

    [Solution]
    readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild
        ? Configuration.Debug
        : Configuration.Release;

    readonly string SshUsername = "root";

    readonly string SshHost = "209.38.44.94";

    readonly int SshPort = 22;

    [Parameter] [Secret]
    private readonly string ENERGY_SECRET;


    readonly string RemoteDirectory = "/root/aggregator/";
    RuntimeConfig runtimeConfig = new RuntimeConfig();

    Runtime runtime => runtimeConfig.Runtime;
    readonly AbsolutePathList paths = PathServiceProvider.paths;

    readonly AbsolutePath LocalDirectoryForDeploy = Path.Combine(PathServiceProvider.paths.GetPathForPhase(
        Phase.Zip), "linux-x64.zip");

    private ISftpService _sftpService;



    Target Init => _ => _
                
                .DependentFor(Clean)
                .Executes(() =>
                {
                    DirectoryManager directoryManager = new();
                    AbsolutePathList paths = PathServiceProvider.paths;

                    foreach (Phase phase in Enum.GetValues(typeof(Phase)))
                    {
                        ManagedPaths managedPath = paths[phase];
                        directoryManager.EnsureDirectory(managedPath.Path, managedPath.Rule);

                        Log.Information($"Ensuring directory {paths} existence...");
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
                                .SetOutput(outputDirectory)
                        );

                        Log.Information(
                            "Compilation outputs are directed to: {0}",
                            outputDirectory
                        );

                        IFileDeletionService fileDeletionService = new FileDeletionService();
                        fileDeletionService.DeleteFiles(outputDirectory, "ShellyApp.pdb", "appsettings.Development.json", "appsettings.json");

                        Log.Information("Unecessary files deleted successfully.");

                    
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
                    var outputDirectory = paths.ProvidePath(runtime, Phase.Compile);
                    var zipFilePath = Path.ChangeExtension(
                        paths.ProvidePath(runtime, Phase.Zip),
                        ".zip"
                    );

                    Log.Information($"Compressing output for {runtime.dotNetIdentifier}");

                    ZipFile.CreateFromDirectory(outputDirectory, zipFilePath);

                    Log.Information($"Application compressed successfully into {zipFilePath}");
                });

    Target ConnectSFTP =>
        _ =>
            _.DependsOn(Compress)
                .Executes(() =>
                {
                 
                    IPrivateKeyProvider privateKeyProvider = new PrivateKeyProvider();
                    ISftpClientFactory sftpClientFactory = new SftpClientFactory();
                    
                    PrivateKeyFile key = privateKeyProvider.GetPrivateKey(ENERGY_SECRET);
                    _sftpService = new SftpService(sftpClientFactory, SshHost, SshUsername);

                    _sftpService.Connect(key);
                });

    Target Deploy =>
        _ =>
            _.DependsOn(ConnectSFTP)
                .Executes(() =>
                {
                   _sftpService.UploadFile(LocalDirectoryForDeploy, RemoteDirectory);

                });

    Target Unzip =>
        _ =>
            _.DependsOn(Deploy)
                .Executes(() => 
                {   
                    string remoteZipFilePath = $"{RemoteDirectory}/{Path.GetFileName(LocalDirectoryForDeploy)}";
                    _sftpService.ExecuteCommand($"unzip -o {remoteZipFilePath} -d {RemoteDirectory}");
                });


    Target Execution =>
        _ => 
            _.DependsOn(Unzip)
                .Executes(() =>
                {
                    _sftpService.ExecuteCommand($"cd {RemoteDirectory} && ./ShellyApp");

                    // _sftpService.Disconnect();
                });
}
