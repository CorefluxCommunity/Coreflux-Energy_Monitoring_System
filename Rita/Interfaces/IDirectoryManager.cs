using Cloud.Models;

    namespace Cloud.Interfaces
    {
        public interface IDirectoryManager
        {
            void EnsureDirectory(string path, DirectoryBehaviour behaviour);
        }
    }
