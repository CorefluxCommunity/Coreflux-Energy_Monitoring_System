using Renci.SshNet;
using Cloud.Interfaces;

namespace Cloud.Models
{
    public class SftpClientFactory : ISftpClientFactory
    {
        public SftpClient CreateSftpClient(string host, string username, PrivateKeyFile key)
        {
            return new SftpClient(host, username, key);
        }
    }
}
