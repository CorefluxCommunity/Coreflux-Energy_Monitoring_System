using Renci.SshNet;


namespace Cloud.Interfaces
{
    public interface IPrivateKeyProvider
    {
        PrivateKeyFile GetPrivateKey(string privateKeyString);
    }
}