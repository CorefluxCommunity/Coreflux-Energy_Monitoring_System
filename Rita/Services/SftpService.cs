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
      public class sftpService
      {
        private readonly IPrivateKeyProvider _privateKeyProvider;
        private readonly ISftpClientFactory _sftpClientFactory;

        public sftpService(IPrivateKeyProvider privateKeyProvider, ISftpClientFactory sftpClientFactory)
        {
            _privateKeyProvider = privateKeyProvider;
            _sftpClientFactory = sftpClientFactory;
        }

        public void UploadFileToSftp(string privateKeyString, string host, string username, string remoteDirectory, string localFilePath)
        {
            try
            {
                PrivateKeyFile key = _privateKeyProvider.GetPrivateKey(privateKeyString);

                using (SftpClient sftpClient = _sftpClientFactory.CreateSftpClient(host, username, key))
                {
                    sftpClient.Connect();
                    sftpClient.ChangeDirectory(remoteDirectory);
                    Log.Information(sftpClient.IsConnected.ToString());

                    using (FileStream fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        string fileToRemote = Path.GetFileName(localFilePath);
                        sftpClient.UploadFile(fileStream, fileToRemote);
                    }

                    sftpClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to connect to SFTP Server: {ex.Message}");
            }
        }
      }
}