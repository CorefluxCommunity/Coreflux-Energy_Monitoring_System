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
        private SshClient _sshClient;
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

                _sshClient = new SshClient(_sshHost, _sshUsername, privateKeyFile);
                _sshClient.Connect();

                if(_sftpClient.IsConnected && _sshClient.IsConnected)
                {
                    Log.Information("SFTP and SSH Clients connected successfully.");
                }
                else
                {
                    throw new Exception("Failed to connect to the SFTP or SSH server.");
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

            public (string Result, string Error, int ExitStatus) ExecuteCommand(string command)
            {
                if(_sshClient == null || !_sshClient.IsConnected)
                {
                    throw new InvalidOperationException("SSH Client is not connected.");
                }

                using (SshCommand cmd = _sshClient.CreateCommand(command))
                {
                    string result = cmd.Execute();
                    string error = cmd.Error;
                    int exitStatus = cmd.ExitStatus;

                    Log.Information($"Command executed with result: {result}");


                    return (result, error, exitStatus);
                    
                }
            }

            public void Disconnect()
            {
                _sftpClient.Disconnect();
                _sshClient.Disconnect();
                Log.Information("SFTP and SSH Clients Disconnected.");
            }

            public bool IsConnected => _sftpClient != null && _sftpClient.IsConnected && _sshClient != null && _sshClient.IsConnected;

            public string WorkingDirectory => _sftpClient?.WorkingDirectory;

            public void Dispose()
            {
                _sftpClient?.Dispose();
            }
        }
}