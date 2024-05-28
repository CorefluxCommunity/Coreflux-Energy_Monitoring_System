using Cloud.Interfaces;
using Renci.SshNet;
using System.IO;
using System.Text;
using Nuke.Common.Tooling;

namespace Cloud.Services
{
    public class ServiceFileManager : IServiceFileManager
    {
        private readonly string _serviceFilePath;
        private readonly string _serviceName;
        private readonly string _serviceDirectory;

        public ServiceFileManager(string serviceFilePath, string serviceName, string serviceDirectory)
        {
            _serviceFilePath = serviceFilePath;
            _serviceName = serviceName;
            _serviceDirectory = serviceDirectory;
        }

        public void CreateServiceFile()
        {   
            string systemServiceFilePath = $"/etc/systemd/system/{_serviceName}";
            if (File.Exists(systemServiceFilePath))
            {
                File.Delete(systemServiceFilePath);
            }

            string serviceFileContent = GetServiceFileContent();
            File.WriteAllText(_serviceFilePath, serviceFileContent);
            
            ProcessTasks.StartProcess("sudo", $"cp {_serviceFilePath} /etc/systemd/system/{_serviceName}").AssertZeroExitCode();
        }

        private string GetServiceFileContent()
        {
            return $@"
[Unit]
Description=Project Shelly Service
After=network.target

[Service]
ExecStart=/root/aggregator/ProjectShelly
Restart=always
User=root
Group=root
Environment=PATH=/usr/bin:/usr/local/bin
WorkingDirectory=/root/aggregator

[Install]
WantedBy=multi-user.target
";
            
        }
    }
}