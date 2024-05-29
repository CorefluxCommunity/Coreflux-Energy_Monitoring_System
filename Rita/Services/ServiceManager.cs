
using System.Diagnostics;
using Cloud.Deployment;
using Cloud.Interfaces;
using Nuke.Common.Tooling;
using Renci.SshNet;

namespace Cloud.Services
{
    public class ServiceManager : IServiceManager
    {

        private readonly ISftpService _stfpService;

        public ServiceManager(ISftpService sshClientService)
        {
            _stfpService = sshClientService;
        }

        public void CreateServiceFile(string serviceName, string serviceContent )
        {
            string deleteCommand = $"if [ -f /etc/systemd/system/{serviceName}]; then sudo rm /etc/systemd/system/{serviceName}; fi";
            _stfpService.ExecuteCommand(deleteCommand);

            string createCommand = $"echo '{serviceContent}' | sudo tee /etc/systemd/system/{serviceName}";
            _stfpService.ExecuteCommand(createCommand);
            
        }

        public void StartService(string serviceName)
        {
            string startCommand = $"sudo systemctl start {serviceName}";
            _stfpService.ExecuteCommand(startCommand);
        }

        public void EnableService(string serviceName)
        {
            string enableCommand = $"sudo systemctl enable {serviceName}";
            _stfpService.ExecuteCommand(enableCommand);
        }

        public void ReloadSystem()
        {
            string reloadCommand = $"sudo systemctl daemon-reload";
            _stfpService.ExecuteCommand(reloadCommand);
        }
    }
}