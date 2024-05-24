using System;
using System.IO;
using System.Collections.Generic;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Cloud.Interfaces;
using Octokit;
using FileMode = System.IO.FileMode;

namespace Cloud.Deployment
{
        public class SftpService : ISftpService, IDisposable
    {
        private readonly ConnectionInfo _connectionInfo;

        private SftpClient _client;

        public SftpService(ConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
        }

        public void Connect()
        {
            _client = new SftpClient(_connectionInfo);
            _client.Connect();
        }

        public void Disconnect()
        {
            _client.Disconnect();
            _client.Dispose();

        }

        public void UploadFile(string localFilePath, string remoteFilePath)
        {
            using (FileStream fs = new FileStream(localFilePath, FileMode.Open))
            {
                _client.UploadFile(fs, remoteFilePath);
            }
        }

        
        public IEnumerable<ISftpFile> ListDirectory(string remoteDirectory)
        {
            return _client.ListDirectory(remoteDirectory);
        }

        public void Dispose() => _client?.Dispose();
    }
}