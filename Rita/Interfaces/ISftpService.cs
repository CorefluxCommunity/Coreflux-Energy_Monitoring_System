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
        (string Result, string Error, int ExitStatus) ExecuteCommand(string command);
        void Disconnect();
        bool IsConnected {get;}
        string WorkingDirectory {get;}
    }

}