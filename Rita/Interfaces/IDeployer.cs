
using Cloud.Models;

namespace Cloud.Interfaces
{


    public interface IDeployer
    {
        void Deploy(Runtime runtime);
    }

}