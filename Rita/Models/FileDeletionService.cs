using System;
using System.IO;
using Cloud.Interfaces;
using Serilog;


namespace Cloud.Models
{   
    public class FileDeletionService : IFileDeletionService
    {

        public void DeleteFiles(string directory, params string[] files)
        {
            foreach (string file in files)
            {
                string filePath = Path.Combine(directory, file);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Log.Information($"Deleted file: {filePath}");
                }
                else
                {
                    Log.Warning($"File not found: {filePath}");
                }
            }
       }
    }
}