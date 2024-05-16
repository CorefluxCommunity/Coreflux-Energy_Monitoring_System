using Coreflux.API.Networking.MQTT;
namespace App.WorkerService;

using System.Linq.Expressions;
using Newtonsoft.Json;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    public bool FirstEntry { get; set; }
    public Company MyCompany{get; set;} = new Company(); // create Company Obj

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

        var company = document.ToModel()["company"] as TomlTable;
        if (company != null)
        {
            string topic = company["topic"].ToString();
            var topicSegments = topic.Split('/');
            if (topicSegments.Length != 2)
            {
                _logger.LogWarning($"Invalid topic format: {topic}");
                return;
            }
            string companyName = topicSegments[0];
            MyCompany.Name = companyName;
            MyCompany.Topic = topic;
        }

        /* var toml = File.ReadAllText(filepath);
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
        } */
    }

    private void onMqttNewPayload(MQTTNewPayload message)
    {
        try
        {
            if (ValidateDeviceDetails(message.topic))
            {
                var msg = JsonConvert.DeserializeObject<DeviceDetails>(message.payload);
                if (msg != null)
                    ProcessDeviceDetails(msg, message.topic);
            }
            else
                _logger.LogWarning($"Invalid payload or strucute: {message.payload}");
        }
        catch (JsonSerializationException ex)
        {
            _logger.LogError($"Error deserializing payload: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unhandled exception: {ex.Message}");
        }
    }

    public bool ValidateDeviceDetails(string topic)
    {
        var topicSegments = topic.Split('/');

        if (topicSegments.Length == 6 && topicSegments[5] == "switch:0")
            return true;
        return false;
    }

    private void ProcessDeviceDetails(DeviceDetails msg, string topic)
    {
        var topicSegments = topic.Split('/');

        if (topicSegments.Length < 6)
        {
            _logger.LogWarning($"Invalid topic format: {topic}");
            return;
        }

        string officeName = topicSegments[1];
        string roomName = topicSegments[2];
        string deviceName = topicSegments[3];

        _logger.LogInformation($"Processing topic: {topic} - Office: {officeName}, Room: {roomName}, Device: {deviceName}");

        var office = MyCompany.Offices.FirstOrDefault(o => o.Name == officeName);
        if (office == null)
        {
            office = new Office { Name = officeName };
            MyCompany.Offices.Add(office);
            _logger.LogInformation($"Created new office: {officeName}");
        }

        var room = office.Rooms.FirstOrDefault(r => r.Name == roomName);
        if (room == null)
        {
            room = new Room { Name = roomName };
            office.Rooms.Add(room);
            _logger.LogInformation($"Created new room: {roomName}");
        }

        var device = room.Devices.FirstOrDefault(d => d.Name == deviceName);
        if (device == null)
        {
            device = new Device
            {
                Topic = topic,
                Name = deviceName
            };
            room.Devices.Add(device);
            _logger.LogInformation($"Created new device: {deviceName}");
        }

        // Ensure device.Messages is not null
        if (device.Messages == null)
        {
            device.Messages = new List<DeviceDetails>();
        }
        if (device.Messages == null)
        {
            device.Messages = new List<DeviceDetails>();
        }
        device.Messages.Add(msg);
        if (!device.Reference)
        {
            device.LastEnergyVal = msg.Aenergy.Total;
            device.Reference = true;
            Console.WriteLine($"\nREFERENCE VALUE\n----------TOPIC {device.Topic}\nLast Energy Value: {device.LastEnergyVal}\n");
        }
        else
        {
            Console.WriteLine($"\n NEW ENERGY VALUES from the topic {device.Topic} at {DateTimeOffset.Now}");
            Console.WriteLine($"Last Energy Value: {device.LastEnergyVal:F3}");
            Console.WriteLine($"Actual Energy Value: {msg.Aenergy.Total:F3}");
            double tempEnergyConsumed = 0;
            tempEnergyConsumed = Math.Round(msg.Aenergy.Total - device.LastEnergyVal, 3);
            device.TotalEnergyConsumed += tempEnergyConsumed;
            device.LastEnergyVal = msg.Aenergy.Total;
            Console.WriteLine($"TOTAL ENERGY CONSUMED: {device.TotalEnergyConsumed:F3}\n");

            room.TotalEnergy += tempEnergyConsumed;
            room.TotalEnergy = Math.Round(room.TotalEnergy, 3);

            office.TotalEnergy += tempEnergyConsumed;
            office.TotalEnergy = Math.Round(office.TotalEnergy, 3);

            MyCompany.TotalEnergy += tempEnergyConsumed;
            MyCompany.TotalEnergy = Math.Round(MyCompany.TotalEnergy, 3);
        }
    }


    private void onMqttConnect()
    {
        MQTTController.GetDataAsync(MyCompany.Topic);
        _logger.LogInformation($"Subscribed to topic: {MyCompany.Topic} at {DateTimeOffset.Now}");
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
                    foreach (var device in room.Devices)
                    {
                        statusFeedback(device);
                        device.Messages.Clear();
                        Console.WriteLine($"\n                         Device {device.Name} Energy: {device.TotalEnergyConsumed:F3}");
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
