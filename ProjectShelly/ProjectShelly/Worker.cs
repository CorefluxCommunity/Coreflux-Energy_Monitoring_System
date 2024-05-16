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
    public Company MyCompany{get; set;} = new Company{Name = "Coreflux"}; // create Company Obj

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        LoadConfiguration("config.toml");
        MQTTController.OnConnect += onMqttConnect; //connect to Topic
        MQTTController.NewPayload += onMqttNewPayload; // Receiving new info
        MQTTController.StartAsync("iot.coreflux.cloud",1883).Wait();
    }

    private void LoadConfiguration(string filepath)
    {
        var toml = File.ReadAllText(filepath);
        var document = Toml.Parse(toml);

        if (document.HasErrors)
        {
            foreach (var error in document.Diagnostics)
            {
                _logger.LogInformation($"{error}");
            }
            return;
        }

        var devices = document.ToModel()["devices"] as TomlTableArray;

        foreach (var device in devices!)
        {
            string topic = device["topic"].ToString()!;
            var topicSegments = topic.Split('/');

            if (topicSegments.Length < 6)
            {
                _logger.LogWarning($"Invalid topic format: {topic}");
                continue;
            }
            //TODO doing the same logic to the company name!!!
            string officeName = topicSegments[1];
            string roomName = topicSegments[2];
            string deviceName = topicSegments[3];

            //TODO doing the same logic to the company name and create a new object here!!


            var office = MyCompany.Offices.FirstOrDefault(o => o.Name == officeName); //TODO perceber LINQ !!!!
            if (office == null)
            {
                office = new Office { Name = officeName };
                MyCompany.Offices.Add(office);
            }

            var room = office.Rooms.FirstOrDefault(r => r.Name == roomName);
            if (room == null)
            {
                room = new Room { Name = roomName };
                office.Rooms.Add(room);
            }

            // Ensure Device does not already exist in the Room
            var existingDevice = room.Devices.FirstOrDefault(d => d.Name == deviceName);
            if (existingDevice == null)
            {
                var deviceCreation = new Device
                {
                    Topic = topic,
                    Name = deviceName // Set the device name
                };
                room.Devices.Add(deviceCreation);
            }
            else
            {
                _logger.LogWarning($"Device with name {deviceName} already exists in room {roomName}");
            }
        }
    }

    private void onMqttNewPayload(MQTTNewPayload message)
    {
        var msg = JsonConvert.DeserializeObject<DeviceDetails>(message.payload);

        foreach (var office in MyCompany.Offices)
        {
            foreach (var room in office.Rooms)
            {
                var MyDevice = room.Devices.Find(p => p.Topic.Equals(message.topic));//TODO perceber melhor como funciona LINQ
                if (MyDevice != null)
                {
                    MyDevice.Messages.Add(msg);
                    if (!MyDevice.Reference)
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
                        tempEnergyConsumed = Math.Round(msg.Aenergy.Total - MyDevice.LastEnergyVal, 3);
                        MyDevice.TotalEnergyConsumed += tempEnergyConsumed;
                        MyDevice.LastEnergyVal = msg.Aenergy.Total;
                        Console.WriteLine($"TOTAL ENERGY CONSUMED: {MyDevice.TotalEnergyConsumed:F3}\n");

                        room.TotalEnergy += tempEnergyConsumed;
                        room.TotalEnergy = Math.Round(room.TotalEnergy, 3);

                        office.TotalEnergy += tempEnergyConsumed;
                        office.TotalEnergy = Math.Round(office.TotalEnergy, 3);

                        MyCompany.TotalEnergy += tempEnergyConsumed;
                        MyCompany.TotalEnergy = Math.Round(MyCompany.TotalEnergy, 3);
                    }
                }
            }

        }
    }

    private void onMqttConnect()
    {
        foreach (var office in MyCompany.Offices)
        {
            foreach (var room in office.Rooms)
            {
                foreach (var device in room.Devices)
                {
                    device.CheckAndSubscribe(_logger);
                }
            }
        }
        _logger.LogInformation($"Subscribed to all topics: {DateTimeOffset.Now}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine($"Worker running at: {DateTimeOffset.Now}");

            await Task.Delay(10_000, stoppingToken);
            foreach (var office in MyCompany.Offices)
            {
                foreach (var room in office.Rooms)
                {
                    foreach (var MyDevice in room.Devices)
                    {
                        statusFeedback(MyDevice);
                        MyDevice.Messages.Clear();
                        Console.WriteLine($"\n                         Device {MyDevice.Name} Energy: {MyDevice.TotalEnergyConsumed:F3}");
                    }
                  Console.WriteLine($"\n                         Room {room.Name} Energy: {room.TotalEnergy:F3}");
                }
                await Task.Delay(2_000, stoppingToken);
                Console.WriteLine($"\n-------------------------Office {office.Name} Energy: {office.TotalEnergy:F3}");
            }
            await Task.Delay(2_000, stoppingToken);
            Console.WriteLine($"\n*************************Company {MyCompany.Name} Energy: {MyCompany.TotalEnergy:F3}");
        }
    }


    private void statusFeedback(Device device)
    {
        string originalTopic = device.Topic;
        string newTopic = originalTopic.Replace("switch:0", "EnergyConsumed");
        //Console.WriteLine($"->>>>>>>Publishing message to TOPIC: {DateTimeOffset.Now}");
        MQTTController.SetDataAsync(newTopic, device.TotalEnergyConsumed.ToString());
    }
}
