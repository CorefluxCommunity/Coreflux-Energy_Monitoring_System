using Cloud.Models;
using Nuke.Common;
using Nuke.Common.IO;


namespace Cloud.Services
{
    public static class PathServiceProvider
    {
        
        public static  AbsolutePathList paths = new AbsolutePathList(NukeBuild.RootDirectory); 

    }
}