

namespace Cloud.Interfaces
{

    public interface IServiceManager
    {
        void CreateServiceFile(string serviceName, string content);
        void ReloadSystem();
        void EnableService(string serviceName);
        void StartService(string serviceName);

    }


}