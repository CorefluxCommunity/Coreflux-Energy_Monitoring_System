namespace Cloud.Models
{
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
}