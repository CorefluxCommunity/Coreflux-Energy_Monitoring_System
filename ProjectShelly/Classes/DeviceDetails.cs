using App.WorkerService;
using Coreflux.API.Networking.MQTT;
using Newtonsoft.Json;

namespace Classes;

public class DeviceDetails
{
	[JsonProperty("id")]
    public int Id { get; set; }
    
	[JsonProperty("source")]
	public string? Source { get; set; }
    
	[JsonProperty("output")]
	public bool Output { get; set; }
    
	[JsonProperty("apower")]
	public double Apower { get; set; }
    
	[JsonProperty("voltage")]
	public double Voltage { get; set; }
    
	[JsonProperty("freq")]
	public double Freq { get; set; }
    
	[JsonProperty("current")]
	public double Current { get; set; }
	
	[JsonProperty("aenergy")]
	public Aenergy? Aenergy { get; set; }
    
	[JsonProperty("temperature")]
	public Temperature? Temperature { get; set; }
}
