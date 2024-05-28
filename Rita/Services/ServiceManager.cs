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

        public ServiceManager(string serviceName)
        {
            _serviceName = serviceName;

        }

        public void StopAndDisableService(SshClient sshClient)
        {
            sshClient.RunCommand($"systemctl stop {_serviceName}");
            sshClient.RunCommand($"systemctl disable {_serviceName}");   
        }

        public void RemoveServiceFile(SshClient sshClient)
        {
            sshClient.RunCommand($"rm /etc/systemd/system/{_serviceName}.service");
        }

        public void UploadServiceFile(SftpClient sftpClient, string serviceContent)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(serviceContent)))
            {
                sftpClient.UploadFile(stream, $"/etc/systemd/system/{_serviceName}");
            }
        }

        public void ReloadDaemon(SshClient sshClient)
        {
            sshClient.RunCommand("systemctl daemon-reload");
        }

        public void EnableAndStartService(SshClient sshClient)
        {
            sshClient.RunCommand($"systemctl enable {_serviceName}");
            sshClient.RunCommand($"systemctl start {_serviceName}");
        }

        public string CheckServiceStatus(SshClient sshClient)
        {
            var result = sshClient.RunCommand($"systemctl is-active {_serviceName}");
            return result.Result.Trim();
        }
    }
}