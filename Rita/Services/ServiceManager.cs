
using System.Diagnostics;
using Cloud.Interfaces;
using Nuke.Common.Tooling;

namespace Cloud.Services
{
    public class ServiceManager : IServiceManager
    {
        

        public ServiceManager()
        {
           
        }

        public void ReloadSystem()
        {
            ProcessTasks.StartProcess("sudo", "systemctl daemon-reload").AssertZeroExitCode();
        }

        public void EnableService()
        {
            ProcessTasks.StartProcess("sudo", $"systemctl enable ProjectShelly.service").AssertZeroExitCode();
        }

        public void StartService()
        {
            ProcessTasks.StartProcess("sudo", $"systemctl start ProjectShelly.service").AssertZeroExitCode();
        }
    }
}