using App.WorkerService;
using Coreflux.API.Networking.MQTT;
using Newtonsoft.Json;
namespace Classes;

public class Company
{
    public string Name { get; set; } = "";

	public string Topic{get; set;} = "";
	public double TotalEnergy;
    public List<Office> Offices { get; set; } = new List<Office>();
}
