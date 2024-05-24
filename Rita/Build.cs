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
using Nuke.Common.Tools;
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

using Cloud.Models;
using Cloud.Services;
using Cloud.Interfaces;
using Renci.SshNet;
using Cloud.Deployment;
using System.Text;
using Renci.SshNet.Security;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push],
    InvokedTargets = [nameof(Deploy)],
    ImportSecrets = [nameof(ENERGY_SECRET)],
    AutoGenerate = false
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

    

    [Parameter("SSH Username")]
    readonly string SshUsername = "root";

    [Parameter("SSH Host")]
    readonly string SshHost = "209.38.44.94";

    [Parameter("SSH Port")]
    readonly int SshPort = 22;

     [Parameter] [Secret]
    private readonly string ENERGY_SECRET;

    [Parameter("Remote Directory")]
    readonly string RemoteDirectory = "/root/aggregator";
    RuntimeConfig runtimeConfig = new RuntimeConfig();

    Runtime runtime => runtimeConfig.Runtime;
    readonly AbsolutePathList paths = PathServiceProvider.paths;
  

    readonly AbsolutePath LocalDirectoryForDeploy = PathServiceProvider.paths.GetPathForPhase(Phase.Zip);



    Target Init =>
        _ =>
            _.Before(Clean)
                .Executes(() =>
                {
                 
                IDirectoryManager directoryManager = new DirectoryManager();
                AbsolutePathList paths = PathServiceProvider.paths;
            
                foreach (Phase phase in Enum.GetValues(typeof(Phase)))
                    {
                        ManagedPaths managedPath = paths[(Phase)phase];
                        directoryManager.EnsureDirectory(managedPath.Path, managedPath.Rule);
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
                                .SetOutput(outputDirectory));
                        

                        
                        Log.Information(
                            "Compilation outputs are directed to: {0}",
                            outputDirectory
                            
                        
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
                
                   
                        var outputDirectory = paths.ProvidePath(runtime, Phase.Compile);
                        var zipFilePath = Path.ChangeExtension(paths.ProvidePath(runtime, Phase.Zip), ".zip");

                        Log.Information($"Compressing output for {runtime.dotNetIdentifier}");

                        ZipFile.CreateFromDirectory(outputDirectory, zipFilePath);
                        
                        Log.Information($"Application compressed successfully into {zipFilePath}");
            
                    
                });

    Target Deploy =>
        _ =>
            _.DependsOn(Compress)
                .Executes(() =>
                {
                    

                    PrivateKeyFile keyFile = new(ENERGY_SECRET);
                    AuthenticationMethod[] methods = [new PrivateKeyAuthenticationMethod(SshUsername, keyFile)];
                    ConnectionInfo connectionInfo = new(SshHost, SshPort, SshUsername, methods);

                    using (ISftpService sftpService = new SftpService(connectionInfo))
                    {
                        Deployer deployer = new(sftpService, LocalDirectoryForDeploy, RemoteDirectory, paths);

                        deployer.Deploy(runtime);
                    }

                   
                  
                });
}
