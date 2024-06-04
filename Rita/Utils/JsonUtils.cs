using Newtonsoft.Json.Linq;
using Nuke.Common.IO;
using System.IO;
using System.Runtime.CompilerServices;

public static class JsonUtils
{
    public static JObject LoadJson(AbsolutePath filePath)
    {
        return JObject.Parse(File.ReadAllText(filePath));
    }

    public static JToken LoadJsonToken(AbsolutePath filepath)
    {
        return JToken.Parse(File.ReadAllText(filepath));
    }
}