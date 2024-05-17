using App.WorkerService;
using Coreflux.API.Networking.MQTT;
using Newtonsoft.Json;

namespace Classes;
public class Aenergy
{
	[JsonProperty("total")]
    public double Total { get; set; }

	[JsonProperty("by_minute")]
    public List<double> By_minute { get; set; } = new List<double>();
    
	[JsonProperty("minute_ts")]
	public long Minute_ts { get; set; }
}