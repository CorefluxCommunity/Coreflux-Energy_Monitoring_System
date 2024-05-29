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

        

        public ServiceFileManager(string serviceFilePath, string serviceName)
        {
            _serviceFilePath = serviceFilePath;
            _serviceName = serviceName;
            
 
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