using Cloud.Models;
using Cloud.Interfaces;
using System.IO;
using System;

namespace Cloud.Services
{
    public class DirectoryManager : IDirectoryManager
    {
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
}

