using System;
using System.Collections.Generic;
using Renci.SshNet.Sftp;


namespace Cloud.Interfaces
{
    public interface ISftpService : IDisposable
    {
        void Connect();
        void Disconnect();
        void UploadFile(string localFilePath, string remoteFilePath);
        IEnumerable<ISftpFile> ListDirectory(string remoteDirectory);
    }

}