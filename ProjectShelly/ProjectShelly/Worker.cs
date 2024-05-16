using Coreflux.API.Networking.MQTT;
namespace App.WorkerService;
using Newtonsoft.Json;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    public bool FirstEntry { get; set; }
    public Room MyRoom { get; set; } = new Room(); // create Room Obj

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        LoadConfiguration("config.toml", MyRoom);
        MQTTController.OnConnect += onMqttConnect; //connect to Topic
        MQTTController.NewPayload += onMqttNewPayload; // Receiving new info
        MQTTController.StartAsync("iot.coreflux.cloud",1883).Wait();
    }

    private void LoadConfiguration(string filepath, Room MyRoom)
    {
        var toml = File.ReadAllText(filepath);
        var document = Toml.Parse(toml);

        //TODO Check if document was open and its well configured!!!

        var devices = document.ToModel()["devices"] as TomlTableArray;
    
        foreach (var device in devices!)
        {
            var deviceCreation = new Device
            {
                Topic = device["topic"].ToString()!
            };
           MyRoom.Devices.Add(deviceCreation);
        }
    }

    private void onMqttNewPayload(MQTTNewPayload message)
    {
        var msg = JsonConvert.DeserializeObject<DeviceDetails>(message.payload);

        var MyDevice= MyRoom.Devices.Find( p => p.Topic.Equals(message.topic));//TODO perceber melhor como funciona LINQ
  
        MyDevice.Messages.Add(msg);
        if(!MyDevice.Reference)
        {
            MyDevice.LastEnergyVal = msg.Aenergy.Total;
            MyDevice.Reference = true;
            Console.WriteLine($"\nREFERENCE VALUE\n----------TOPIC {MyDevice.Topic}\nLast Energy Value: {MyDevice.LastEnergyVal}\n");
        }
        else
        {
            Console.WriteLine($"\n NEW ENERGY VALUES from the topic {MyDevice.Topic} at {DateTimeOffset.Now}");
            Console.WriteLine($"Last Energy Value: {MyDevice.LastEnergyVal:F3}");
            Console.WriteLine($"Actual Energy Value: {msg.Aenergy.Total:F3}");
            double tempEnergyConsumed = 0;
            tempEnergyConsumed =  Math.Round(msg.Aenergy.Total - MyDevice.LastEnergyVal, 3);
            MyDevice.TotalEnergyConsumed += tempEnergyConsumed;
            MyDevice.LastEnergyVal = msg.Aenergy.Total;
            Console.WriteLine($"TOTAL ENERGY CONSUMED: {MyDevice.TotalEnergyConsumed:F3}\n");
            MyRoom.TotalEnergy += tempEnergyConsumed;
            MyRoom.TotalEnergy = Math.Round(MyRoom.TotalEnergy, 3);
        }
    }

    private void onMqttConnect()
    {
        foreach (var device in MyRoom.Devices)
        {
            device.CheckAndSubscribe(_logger);
        }
        _logger.LogInformation($"Subscribed to all topics: {DateTimeOffset.Now}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            
            await Task.Delay(10_000, stoppingToken);
            foreach(var MyDevice in MyRoom.Devices)
            {
                statusFeedback(MyDevice);
                MyDevice.Messages.Clear();
            }
            _logger.LogInformation($"Room Energy : {MyRoom.TotalEnergy:F3}");
        }
    }

    private void statusFeedback(Device device)
    {
        string originalTopic = device.Topic;
        string newTopic = originalTopic.Replace("switch:0", "EnergyConsumed");
        _logger.LogInformation("->>>>>>>Publishing message to TOPIC: {time}", DateTimeOffset.Now);
        MQTTController.SetDataAsync(newTopic, device.TotalEnergyConsumed.ToString());
    }
}
