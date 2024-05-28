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
            _sshClient.RunCommand($"systemctl stop {_serviceName}");
            _sshClient.RunCommand($"systemctl disable {_serviceName}");   
        }

        public void RemoveServiceFile()
        {
            _sshClient.RunCommand($"rm /etc/systemd/system/{_serviceName}.service");
        }

        public void UploadServiceFile(string serviceContent)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(serviceContent)))
            {
                _sftpClient.UploadFile(stream, $"/etc/systemd/system/{_serviceName}");
            }
        }

        public void ReloadDaemon()
        {
            _sshClient.RunCommand("systemctl daemon-reload");
        }

        public void EnableAndStartService()
        {
            _sshClient.RunCommand($"systemctl enable {_serviceName}");
            _sshClient.RunCommand($"systemctl start {_serviceName}");
        }
    }
}