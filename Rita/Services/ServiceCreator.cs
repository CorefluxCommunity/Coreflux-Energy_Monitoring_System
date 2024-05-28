
namespace Cloud.Services
{
    public class ServiceCreator
    {

        private readonly string _serviceContent;

        public ServiceCreator(string serviceContent)
        {
            _serviceContent = serviceContent;
        }

        public string GetServiceContent()
        {
            return _serviceContent;
        }
    }
}