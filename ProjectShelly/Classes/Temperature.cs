using App.WorkerService;
using Coreflux.API.Networking.MQTT;
using Newtonsoft.Json;
namespace Classes;

public class Temperature
{
    [JsonProperty("tC")]
    public double Tc { get; set; }
    
    [JsonProperty("tF")]
    public double Tf { get; set; }
}
