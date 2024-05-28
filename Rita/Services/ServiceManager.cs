
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
            ProcessTasks.StartProcess("systemctl", "daemon-reload").AssertZeroExitCode();
        }

        public void EnableService()
        {
            ProcessTasks.StartProcess("systemctl", $"enable {_serviceName}").AssertZeroExitCode();
        }

        public void StartService()
        {
            ProcessTasks.StartProcess("systemctl", $"start {_serviceName}").AssertZeroExitCode();
        }
    }
}