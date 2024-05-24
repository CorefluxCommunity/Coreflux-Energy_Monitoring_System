using System;
using System.IO;
using System.Collections.Generic;
using Renci.SshNet;
using Cloud.Interfaces;
using Renci.SshNet.Sftp;
using Cloud.Services;
using Nuke.Common.IO;
using Cloud.Models;
using Serilog;



namespace Cloud.Deployment
{
    public class Deployer : IDeployer
    {
        private readonly ISftpService _sftpService;
        private readonly string _localDirectory;

        private readonly string _remoteDirectory;

        private readonly AbsolutePathList _paths;



        public Deployer(ISftpService sftpService, string localDirectory, string remoteDirectory)
        {
            _sftpService = sftpService;

            _localDirectory = localDirectory;

            _remoteDirectory = remoteDirectory;




        }

        public void Deploy(Runtime runtime)
        {
            string zipFilePath = Path.ChangeExtension(_paths.ProvidePath(runtime, Phase.Zip), ".zip");
            string targetPath = Path.Combine(_remoteDirectory, Path.GetFileName(zipFilePath)).Replace("\\", "/");

            Log.Information($"Deploying {zipFilePath} to {targetPath}");

            _sftpService.Connect();
            _sftpService.UploadFile(zipFilePath, targetPath);
            _sftpService.Disconnect();

            // var files = Directory.GetFiles(_localDirectory, "linux-x64.zip");

            // foreach (string filePath in files)
            // {
            //     string remotefilePath = Path.Combine(_remoteDirectory, Path.GetFileName(filePath).Replace("\\", "/"));
            //     _sftpService.UploadFile(filePath, remotefilePath);
            // }

            // foreach (ISftpFile file in _sftpService.ListDirectory(_remoteDirectory))
            // {
            //     Console.WriteLine($"{file.FullName} {file.LastWriteTime}");
            // }


        }
    }



}


