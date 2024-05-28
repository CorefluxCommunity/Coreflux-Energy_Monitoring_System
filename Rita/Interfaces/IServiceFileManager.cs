

namespace Cloud.Interfaces
{


    public interface IServiceFileManager
    {
        void CreateServiceFile();
    }

    public interface IServiceManager
    {  
        void ReloadSystem();
        void EnableService();
        void StartService();

    }
    

}