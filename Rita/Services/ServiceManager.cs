
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
            ProcessTasks.StartProcess("sudo -S", "systemctl daemon-reload").AssertZeroExitCode();
        }

        public void EnableService()
        {
            ProcessTasks.StartProcess("sudo -S", $"systemctl enable {_serviceName}").AssertZeroExitCode();
        }

        public void StartService()
        {
            ProcessTasks.StartProcess("sudo -S", $"systemctl start {_serviceName}").AssertZeroExitCode();
        }
    }
}