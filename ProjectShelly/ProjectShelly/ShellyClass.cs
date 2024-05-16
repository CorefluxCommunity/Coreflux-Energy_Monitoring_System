using App.WorkerService;
using Coreflux.API.Networking.MQTT;
using Newtonsoft.Json;

public class Aenergy
{
	[JsonProperty("total")]
    public double Total { get; set; }

	[JsonProperty("by_minute")]
    public List<double> By_minute { get; set; } = new List<double>();
    
	[JsonProperty("minute_ts")]
	public long Minute_ts { get; set; }
}

public class Temperature
{
    [JsonProperty("tC")]
    public double Tc { get; set; }
    
    [JsonProperty("tF")]
    public double Tf { get; set; }
}
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

public class Device
{
	public string Topic { get; set;} = "";
	public string Name {get; set;} = "";
	public bool Reference = false;
	public bool	TopicConnection = false;
	public List<DeviceDetails>? Messages = new List<DeviceDetails>();

	public double LastEnergyVal;
	public double TotalEnergyConsumed;

	/* public Device()
	{
	} */

	public bool CheckAndSubscribe(ILogger<Worker> log)
	{
		if (!this.TopicConnection)
		{
			MQTTController.GetDataAsync(this.Topic);
			this.TopicConnection = true;
			log.LogInformation($"Subscribing to {this.Topic} at: {DateTimeOffset.Now}");
		}
		else
			this.TopicConnection=false;

		
		return this.TopicConnection;
	}
}


public class Room
{
	public string Name = "";
	public List<Device>? Devices;
	
	public double TotalEnergy;
	
	public Room()
	{
		Devices = new List<Device>();
	}
}

public class Office
{
    public string Name { get; set; } = "";
	public string Topic { get; set; } = "";
	public double TotalEnergy;
    public List<Room> Rooms { get; set; } = new List<Room>();
}

public class Company
{
    public string Name { get; set; } = "";

	public string Topic{get; set;} = "";
	public double TotalEnergy;
    public List<Office> Offices { get; set; } = new List<Office>();
}
