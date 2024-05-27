using App.WorkerService;
using Coreflux.API.Networking.MQTT;
using Newtonsoft.Json;
namespace Classes;

public class Office
{
 public string Name { get; set; } = "";
	public string Topic { get; set; } = "";
	public double TotalEnergy;
    public List<Room> Rooms { get; set; } = new List<Room>();

	public long UnixTimestamp;
}