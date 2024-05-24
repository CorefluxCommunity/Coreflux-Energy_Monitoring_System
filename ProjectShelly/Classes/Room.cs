using App.WorkerService;
using Coreflux.API.Networking.MQTT;
using Newtonsoft.Json;

namespace Classes;

public class Room
{
	public string Name {get; set;} = "";

	public string Topic {get; set;} = "";
	public List<Device>? Devices;
	
	public double TotalEnergy;
	
	public Room()
	{
		Devices = new List<Device>();
	}

	public long UnixTimestamp;
}
