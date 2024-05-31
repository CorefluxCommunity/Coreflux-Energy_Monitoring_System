

namespace Cloud.Interfaces
{
    public interface IFileDeletionService
    {
        void DeleteFiles(string directory, params string[] files);
    }
}