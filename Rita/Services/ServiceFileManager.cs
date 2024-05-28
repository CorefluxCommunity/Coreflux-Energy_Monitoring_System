using Cloud.Interfaces;
using Renci.SshNet;
using System.IO;
using System.Text;
using Nuke.Common.Tooling;

namespace Cloud.Services
{
    public class ServiceFileManager : IServiceFileManager
    {

        public ServiceFileManager()
        {
            
        }

        public void CreateServiceFile()
        {   
            string systemServiceFilePath = $"/etc/systemd/system/ProjectShelly.service";
            if (File.Exists(systemServiceFilePath))
            {
                File.Delete(systemServiceFilePath);
            }

            string serviceFileContent = GetServiceFileContent();
            File.WriteAllText(systemServiceFilePath, serviceFileContent);
            
            
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


[Install]
WantedBy=multi-user.target
";
            
        }
    }
}