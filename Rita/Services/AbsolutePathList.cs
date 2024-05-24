using System.Collections.Generic;
using System.IO;
using Cloud.Models;
using Nuke.Common.IO;

namespace Cloud.Services
{
    public class AbsolutePathList : Dictionary<Phase,ManagedPaths>
    {
        public AbsolutePath RootDirectory;

        public AbsolutePathList(AbsolutePath rootDirectory)
        {
            RootDirectory = rootDirectory;
            InitializePaths();
        }
        

        public void InitializePaths()
        {
            this.Add(Phase.Init, new ManagedPaths(Path.Combine(RootDirectory, "artifacts"), DirectoryBehaviour.GuaranteeDirectoryExistsAndCleanFiles));
            this.Add(Phase.Build, new ManagedPaths (Path.Combine(RootDirectory, "ProjectShelly", "ProjectShelly.csproj"), DirectoryBehaviour.DoNothing));
            this.Add(Phase.Compile, new ManagedPaths( Path.Combine(RootDirectory, "artifacts", "buildout"), DirectoryBehaviour.GuaranteeDirectoryExistsAndCleanFiles));
            this.Add(Phase.Test, new ManagedPaths(Path.Combine(RootDirectory, "ProjectShelly.Tests", "ProjectShelly.Tests.csproj"), DirectoryBehaviour.DoNothing));
            this.Add(Phase.Zip, new ManagedPaths(Path.Combine(RootDirectory, "artifacts", "compressed"), DirectoryBehaviour.GuaranteeDirectoryExists));
            this.Add(Phase.Deploy, new ManagedPaths(Path.Combine(RootDirectory, "artifacts", "deployed"), DirectoryBehaviour.GuaranteeDirectoryExistsAndCleanFiles));
        }
        public string GetPathForPhase(Phase phase)
        {
            return this[phase].Path;

        }

        public string ProvidePath(Runtime runtime, Phase phase)
        {
        

        return phase switch
        {
            Phase.Init => Path.Combine(this.GetPathForPhase(Phase.Init)),
            Phase.Build => Path.Combine(this.GetPathForPhase(Phase.Build)),
            Phase.Compile => Path.Combine(this.GetPathForPhase(Phase.Compile), runtime.pathOutput),
            Phase.Test => Path.Combine(this.GetPathForPhase(Phase.Test)),
            Phase.Zip => Path.Combine(this.GetPathForPhase(Phase.Zip), runtime.pathOutput),
            Phase.Deploy => Path.Combine(this.GetPathForPhase(Phase.Deploy), runtime.pathOutput)
        
        };
        }
    }
}    