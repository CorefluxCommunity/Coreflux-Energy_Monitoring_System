using Newtonsoft.Json.Linq;
using Nuke.Common.IO;
using System.IO;

public static class JsonUtils
{
    public static JObject LoadJson(AbsolutePath filePath)
    {
        return JObject.Parse(File.ReadAllText(filePath));
    }
}