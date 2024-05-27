using System;
using System.Collections.Generic;
using Renci.SshNet;
using Renci.SshNet.Sftp;


namespace Cloud.Interfaces
{
    public interface ISftpService : IDisposable
    {
        void Connect(PrivateKeyFile privateKeyFile);
        void UploadFile(string localFilePath, string remoteFilePath);
        void Disconnect();
        bool IsConnected {get;}
        string WorkingDirectory {get;}
    }

}