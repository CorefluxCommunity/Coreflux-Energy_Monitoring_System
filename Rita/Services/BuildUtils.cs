using System.IO;

public static class BuildUtils
{
    public static string GetProjectName(string projectPath)
    {
        return Path.GetFileNameWithoutExtension(projectPath);
    }
}