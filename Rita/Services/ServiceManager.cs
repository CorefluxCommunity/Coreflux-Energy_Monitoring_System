
using System.Diagnostics;
using Cloud.Interfaces;
using Nuke.Common.Tooling;

namespace Cloud.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly string _serviceName;

        public ServiceManager(string serviceName)
        {
            _serviceName = serviceName;
        }

        public void ReloadSystem()
        {
            ProcessTasks.StartProcess("sudo", "systemctl daemon-reload", workingDirectory: null).AssertZeroExitCode();
        }

        public void EnableService()
        {
            ProcessTasks.StartProcess("sudo", $"systemctl enable {_serviceName}", workingDirectory: null).AssertZeroExitCode();
        }

        public void StartService()
        {
            ProcessTasks.StartProcess("sudo", "systemctl start {_serviceName}", workingDirectory: null).AssertZeroExitCode();
        }
    }
}