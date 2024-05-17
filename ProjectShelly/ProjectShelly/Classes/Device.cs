using App.WorkerService;
using Coreflux.API.Networking.MQTT;
using Newtonsoft.Json;

namespace Classes;

public class Device
{
	public string Topic { get; set;} = "";
	public string Name {get; set;} = "";
	public bool Reference = false;
	public bool	TopicConnection = false;
	public List<DeviceDetails>? Messages = new List<DeviceDetails>();

	public double LastEnergyVal;
	public double TotalEnergyConsumed;
}