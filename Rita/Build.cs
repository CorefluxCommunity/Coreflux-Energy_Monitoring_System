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
using Newtonsoft.Json.Linq;
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
using Services;
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
public class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Init, x => x.RunService);

    [Solution]
    readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild
        ? Configuration.Debug
        : Configuration.Release;


    [Parameter]
    [Secret]
    private readonly string ENERGY_SECRET;

    BuildConfig config;
    ISftpService _sftpService;
    IServiceManager _serviceManager;
    PrivateKeyFile _privateKey;
    IPrivateKeyProvider _privateKeyProvider;
    ISftpClientFactory _sftpClientFactory;


    Target Init => _ => _

                .DependentFor(Clean)
                .Executes(() =>
                {

                    config = new BuildConfig();

                    _privateKeyProvider = new PrivateKeyProvider();
                    _privateKey = _privateKeyProvider.GetPrivateKey(ENERGY_SECRET);
                    _sftpClientFactory = new SftpClientFactory();

                    _sftpService = new SftpService(_sftpClientFactory, config.SshHost, config.SshUsername);
                    _serviceManager = new ServiceManager(_sftpService);





                    DirectoryManager directoryManager = new();
                    AbsolutePathList paths = PathServiceProvider.paths;

                    foreach (Phase phase in Enum.GetValues(typeof(Phase)))
                    {
                        ManagedPaths managedPath = paths[phase];
                        directoryManager.EnsureDirectory(managedPath.Path, managedPath.Rule);

                        Log.Information($"Ensuring directory {managedPath.Path} existence...");
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

                    JObject parameters = JsonUtils.LoadJson(config.ParametersFile);
                    JObject projectPaths = JsonUtils.LoadJson(config.ProjectPathsFile);

                    List<string> projectsToTest = parameters["ProjectsToTest"].ToObject<List<string>>();

                    foreach (string project in projectsToTest)
                    {
                        string projectPath = projectPaths[project].ToString();
                        DotNetTasks.DotNetTest(_ => _.SetProjectFile(projectPath));
                    }

                });

    Target Compile =>
        _ =>
            _.DependsOn(Test)
                .Executes(() =>
                {
                    try
                    {

                        string outputDirectory = config.Paths.ProvidePath(config.Runtime, Phase.Compile);

                        JObject parameters = JsonUtils.LoadJson(config.ParametersFile);
                        JObject projectPaths = JsonUtils.LoadJson(config.ProjectPathsFile);

                        var projectsToBuildArray = parameters["ProjectsToBuildForDroplet"] as JArray;

                        var projectsToBuild = projectsToBuildArray.ToObject<List<ProjectToBuild>>();

                        foreach (ProjectToBuild project in projectsToBuild)
                        {
                            string projectPath = project.ProjectPath;
                            string projectName = project.ProjectName;

                            string projectOutputDir = project.OutputPath;

                            Log.Information($"Compiling project: {project}. Path: {projectPath}...");

                            DotNetTasks.DotNetPublish(s =>
                                s.SetProject(projectPath)
                                    .AddProperty("IncludeNativeLibrariesForSelfExtract", true)
                                    .AddProperty("PublishSelfContained", true)
                                    .AddProperty("AssemblyName", projectName)
                                    .SetRuntime(config.Runtime.dotNetIdentifier)
                                    .SetConfiguration("Release")
                                    .EnablePublishSingleFile()
                                    .SetOutput(projectOutputDir)
                                    
                            );

                            Log.Information(
                                "Compilation outputs are directed to: {0}, {1}",
                                projectOutputDir, projectName
                            );

                            IFileDeletionService fileDeletionService = new FileDeletionService();
                            fileDeletionService.DeleteFiles(projectOutputDir, $"{projectName}.pdb", "appsettings.Development.json", "appsettings.json");

                            Log.Information("Unnecessary files deleted successfully.");

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
                    var outputDirectory = config.Paths.ProvidePath(config.Runtime, Phase.Compile);
                    var zipFilePath = Path.ChangeExtension(
                        config.Paths.ProvidePath(config.Runtime, Phase.Zip),
                        ".zip"
                    );

                    Log.Information($"Compressing output for {config.Runtime.dotNetIdentifier}");

                    ZipFile.CreateFromDirectory(outputDirectory, zipFilePath);

                    Log.Information($"Application compressed successfully into {zipFilePath}");
                });

    Target ConnectSFTP =>
        _ =>
            _.DependsOn(Compress)
                .Executes(() =>
                {
                    _sftpService.Connect(_privateKey);
                });

    Target Deploy =>
        _ =>
            _.DependsOn(ConnectSFTP)
                .Executes(() =>
                {
                    _sftpService.UploadFile(config.LocalDirectoryForDeploy, config.RemoteDirectory);

                });

    Target Unzip =>
        _ =>
            _.DependsOn(Deploy)
                .Executes(() =>
                {
                    string remoteZipFilePath = $"{config.RemoteDirectory}/{Path.GetFileName(config.LocalDirectoryForDeploy)}";
                    _sftpService.ExecuteCommand($"unzip -o {remoteZipFilePath} -d {config.RemoteDirectory}");
                });

    Target CreateService =>
        _ =>
            _.DependsOn(Unzip)
                .Executes(() =>
                {
                    _serviceManager.CreateServiceFile(config.ServiceName, config.Content);

                });


    Target RunService =>
        _ =>
            _.DependsOn(CreateService)
                .Executes(() =>
                {
                    _serviceManager.ReloadSystem();
                    _serviceManager.EnableService(config.ServiceName);
                    _serviceManager.StartService(config.ServiceName);
                    _sftpService.Disconnect();

                });
}
