using Renci.SshNet;
using System.IO;
using System.Text;

namespace Cloud.Services
{
    public class ServiceManager
    {
        private readonly string _serviceName;
        private readonly SshClient _sshClient;
        private readonly SftpClient _sftpClient;

        public ServiceManager(string serviceName, SshClient sshClient, SftpClient sftpClient)
        {
            _serviceName = serviceName;
            _sshClient = sshClient;
            _sftpClient = sftpClient;
        }

        public void StopAndDisableService()
        {
            
        }
    }
}