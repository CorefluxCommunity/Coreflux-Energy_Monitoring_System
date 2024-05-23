using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Nuke.Common.IO;
using Nuke.Common;
using System.IO;
using NuGet.Versioning;
using System.CodeDom;
using System.Security.Cryptography;
using JetBrains.Annotations;



// * classe responsavel por ter uma lista de todos os executaveis a serem criados
public class RuntimeConfig: List<Runtime>
{

    public RuntimeConfig()
    {

    }



}
public enum Phase
{
    Init,
    Build,
    Compile,
    Test,
    Zip,
    Deploy
}
public enum DirectoryBehaviour
{
    DoNothing,
    GuaranteeDirectoryExists,
    GuaranteeDirectoryExistsAndCleanFiles,
}
public class ManagedPaths
{
    public string Path;
    public DirectoryBehaviour Rule;

    public ManagedPaths(string path, DirectoryBehaviour rule)
    {
        this.Path = path;
        this.Rule = rule;
    }
}

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
        Phase.Deploy => Path.Combine(this.GetPathForPhase(Phase.Deploy), runtime.pathOutput),
      
       };
    }
    public void EnsureDirectory(string path, DirectoryBehaviour behaviour)
    {
        switch (behaviour)
        {
            case DirectoryBehaviour.DoNothing:

            break;

            case DirectoryBehaviour.GuaranteeDirectoryExists:
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                
            break;

            case DirectoryBehaviour.GuaranteeDirectoryExistsAndCleanFiles:
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                Directory.CreateDirectory(path);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(behaviour), behaviour, null);

        }
    }
}

public static class PathServiceProvider
{
    

    //TODO criar o metodo que e chamado e aplica o comportamento de cada directorio e apaga os ficheiros e garante que4 existem, se flhar ele retorna
    //TODO e o codigo para.

    public static  AbsolutePathList paths = new AbsolutePathList(NukeBuild.RootDirectory); 

   
}

//*: esta classe e responsavel por ter todo o conteudo para criar os executaveis 
public class Runtime
{
   public string dotNetIdentifier;
   public string pathOutput {  get { return dotNetIdentifier; } 
   }


   public Runtime(string dotnet)
   {
       this.dotNetIdentifier = dotnet;


   }


}