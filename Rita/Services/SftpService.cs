using System;
using System.IO;
using System.Collections.Generic;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Cloud.Interfaces;
using Octokit;
using FileMode = System.IO.FileMode;
using Cloud.Models;
using Serilog;

namespace Cloud.Deployment
{
      public class SftpService : ISftpService
      {
        private readonly IPrivateKeyProvider _privateKeyProvider;
        private readonly ISftpClientFactory _sftpClientFactory;
        private SftpClient _sftpClient;
        private readonly string _sshHost;
        private readonly string _sshUsername;
     
            public SftpService(ISftpClientFactory sftpClientFactory, string sshHost, string sshUsername)
            {
                
                _sftpClientFactory = sftpClientFactory;
                _sshHost = sshHost;
                _sshUsername = sshUsername;

            }

            public void Connect(PrivateKeyFile privateKeyFile)
            {
                _sftpClient = _sftpClientFactory.CreateSftpClient(_sshHost, _sshUsername, privateKeyFile);

                _sftpClient.Connect();

                if(_sftpClient.IsConnected)
                {
                    Log.Information("SFTP Client connected successfully.");
                }
                else
                {
                    throw new Exception("Failed to connect to the SFTP server.");
                }
            }

            public void UploadFile(string LocalDirectoryForDeploy, string remoteDirectory)
            {
                if(_sftpClient == null || !_sftpClient.IsConnected)
                {
                    throw new InvalidOperationException("SFTP client is not connected...");
                }

                _sftpClient.ChangeDirectory(remoteDirectory);
                Log.Information($"Connected to remote directory: {_sftpClient.WorkingDirectory}");

                using (FileStream fileStream = new FileStream(LocalDirectoryForDeploy, FileMode.Open, FileAccess.ReadWrite))
                {
                string fileToRemote = Path.GetFileName(LocalDirectoryForDeploy);
                    _sftpClient.UploadFile(fileStream, fileToRemote);

                    if (_sftpClient.Exists(fileToRemote))
                    {
                        Log.Information($"File '{fileToRemote}' was uploaded successfully to '{_sftpClient.WorkingDirectory}/{fileToRemote}'");
                    }
                    else
                    {
                        Log.Error($"File '{fileToRemote}' was not uploaded successfully.");
                    }

                }
            }

            public void Disconnect()
            {
                _sftpClient.Disconnect();
                Log.Information("SFTP Client disconnected.");
            }

            public bool IsConnected => _sftpClient != null && _sftpClient.IsConnected;

            public string WorkingDirectory => _sftpClient?.WorkingDirectory;

            public void Dispose()
            {
                _sftpClient?.Dispose();
            }
        }
}