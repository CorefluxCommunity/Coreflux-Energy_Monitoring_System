using System;
using System.ComponentModel;
using System.Linq;
using Nuke.Common.Tooling;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Cloud.Models;
using Cloud.Services;
using Newtonsoft.Json.Linq;
using System.IO;


[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static Configuration Debug = new Configuration { Value = nameof(Debug) };
    public static Configuration Release = new Configuration { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration)
    {
        return configuration.Value;
    }
}

public class BuildConfig
{
    public Solution Solution { get; }
    public Configuration Configuration { get; }
    public AbsolutePath ParametersFile { get; }
    public AbsolutePath ProjectPathsFile { get; }
    public string SshUsername { get; } = "root";
    public string SshHost { get; } = "209.38.44.94";
    public string ENERGY_SECRET { get; }
    public Runtime Runtime { get; }
    public AbsolutePathList Paths { get; }
    public AbsolutePath LocalDirectoryForDeploy { get; }
    public string RemoteDirectory { get; } = "/root/aggregator/";
    public string ServiceName { get; } = "projectshelly.service";
    public string Content { get; }

    public BuildConfig()
    {

        ParametersFile = NukeBuild.RootDirectory / ".nuke" / "parameters.json";
        ProjectPathsFile = NukeBuild.RootDirectory / ".nuke" / "projectsPath.json";
        Runtime = new RuntimeConfig().Runtime;
        Paths = PathServiceProvider.paths;
        LocalDirectoryForDeploy = Path.Combine(PathServiceProvider.paths.GetPathForPhase(Phase.Zip), "linux-x64.zip");


        var parameters = JsonUtils.LoadJson(ParametersFile);
        Content = parameters["ServiceContent"].ToString();

    }
}
