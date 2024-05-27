using Renci.SshNet;

namespace Cloud.Interfaces
{
    public interface ISftpClientFactory
    {
        SftpClient CreateSftpClient(string host, string username, PrivateKeyFile key);
    }
}