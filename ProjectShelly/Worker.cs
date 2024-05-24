using Coreflux.API.Networking.MQTT;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;
using Classes;
namespace App.WorkerService;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    public Company MyCompany { get; set; } = new Company(); // create Company Obj

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        LoadConfiguration("config.toml");
        MQTTController.OnConnect += onMqttConnect; //connect to Topic
        MQTTController.NewPayload += onMqttNewPayload; // Receiving new info
        MQTTController.StartAsync("209.38.44.94", 1883).Wait();
    }

    /// <summary>
    /// Loads the configuration from a TOML file.
    /// Set the attributes Name and Topic to a Company object.
    /// </summary>
    /// <param name="filepath">The path to the TOML configuration file.</param>
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
            string topic = company["topic"]?.ToString() ?? string.Empty; // Safely get the topic string, default to empty if null
            if (string.IsNullOrEmpty(topic))
            {
                _logger.LogWarning("Topic is null or empty.");
                return;
            }
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
    }

    /// <summary>
    /// Handles the MQTT connect event.
    /// Subscribes to the company's main topic.
    /// </summary>
    private void onMqttConnect()
    {
        MQTTController.GetDataAsync(MyCompany.Topic);
        _logger.LogInformation($"Subscribed to topic: {MyCompany.Topic} at {DateTimeOffset.Now}");
        MyCompany.Topic = MyCompany.Name + "/" + "CompanyEnergyConsumed";
    }

    /// <summary>
    /// Handles the arrival of new MQTT payloads.
    /// Validates the topic and processes the device details if valid.
    /// </summary>
    /// <param name="message">The MQTT payload message.</param>
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
                return;
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

    /// <summary>
    /// Validates if the topic received, it's in fact the one that needs to be handled.
    /// </summary>
    /// <param name="topic">The MQTT payload message topic to validate.</param>
    /// <returns>True if the topic is valid, otherwise false.</returns>
    public bool ValidateDeviceDetails(string topic)
    {
        var topicSegments = topic.Split('/');

        if (topicSegments.Length == 6 && topicSegments[5] == "switch:0")
            return true;
        return false;
    }

    /// <summary>
    /// Processes the device details from the MQTT message.
    /// Updates the energy consumption for the device, room, office, and company.
    /// </summary>
    /// <param name="msg">The device details from the MQTT message.</param>
    /// <param name="topic">The MQTT topic.</param>
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
            office.Topic = MyCompany.Name + "/" + office.Name + "/" + "OfficeEnergyConsumed";
            Console.WriteLine(office.Topic);
            MyCompany.Offices.Add(office);
            _logger.LogInformation($"Created new office: {officeName}");
        }

        var room = office.Rooms.FirstOrDefault(r => r.Name == roomName);
        if (room == null)
        {
            room = new Room { Name = roomName };
            room.Topic = MyCompany.Name + "/" + office.Name + "/" + room.Name + "/" + "RoomEnergyConsumed";
            Console.WriteLine(room.Topic);
            office.Rooms.Add(room);
            _logger.LogInformation($"Created new room: {roomName}");
        }

        var device = room.Devices!.FirstOrDefault(d => d.Name == deviceName);
        if (device == null)
        {
            device = new Device
            {
                Topic = topic,
                Name = deviceName
            };
            if (device.Topic.Contains("switch:0"))
                device.Topic = device.Topic.Replace("switch:0", "EnergyConsumed");
            Console.WriteLine(room.Topic);
            room.Devices!.Add(device);
            _logger.LogInformation($"Created new device: {deviceName}");
        }
        if (device.Messages == null)
            device.Messages = new List<DeviceDetails>();
        
        device.Messages.Add(msg);
        if (!device.Reference)
        {
            device.LastEnergyVal = msg.Aenergy!.Total;
            device.Reference = true;
            Console.WriteLine($"\nREFERENCE VALUE\n----------TOPIC {device.Topic}\nLast Energy Value: {device.LastEnergyVal}\n");
        }
        else
        {
            Console.WriteLine($"\n NEW ENERGY VALUES from the topic {device.Topic} at {DateTimeOffset.Now}");
            Console.WriteLine($"Last Energy Value: {device.LastEnergyVal:F3}");
            Console.WriteLine($"Actual Energy Value: {msg.Aenergy!.Total:F3}");
            double tempEnergyConsumed = 0;
            tempEnergyConsumed = Math.Round(msg.Aenergy.Total - device.LastEnergyVal, 3);
            device.TotalEnergyConsumed += tempEnergyConsumed;
            device.TotalEnergyConsumed = Math.Round(device.TotalEnergyConsumed, 3);
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

    /// <summary>
    /// Periodically publishes the energy consumption data for devices, rooms, offices, and the company.
    /// </summary>
    /// <param name="stoppingToken">Token to signal cancellation.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");
            await Task.Delay(10_000, stoppingToken);
            string jsonString = "";
            foreach (var office in MyCompany.Offices)
            {
                foreach (var room in office.Rooms)
                {
                    foreach (var device in room.Devices!)
                    {
                        device.UnixTimestamp = CalculateUnixTime();
                        jsonString = JsonConvert.SerializeObject(device);
                        await MQTTController.SetDataAsync(device.Topic, jsonString);
                        if(device.Messages != null)
                            device.Messages.Clear();
                        Console.WriteLine($"\nDevice {device.Name} Energy: {device.TotalEnergyConsumed:F3}");
                    }
                    room.UnixTimestamp = CalculateUnixTime();
                    jsonString = JsonConvert.SerializeObject(room);
                    await MQTTController.SetDataAsync(room.Topic, jsonString);
                    Console.WriteLine($"\n Room {room.Name} Energy: {room.TotalEnergy:F3}");
                }
                office.UnixTimestamp = CalculateUnixTime();
                jsonString = JsonConvert.SerializeObject(office);
                await MQTTController.SetDataAsync(office.Topic, jsonString);
                Console.WriteLine($"\n  Office {office.Name} Energy: {office.TotalEnergy:F3}");
            }
            MyCompany.UnixTimestamp = CalculateUnixTime();
            jsonString = JsonConvert.SerializeObject(MyCompany);
            await MQTTController.SetDataAsync(MyCompany.Topic, jsonString);
            Console.WriteLine($"\n   Company {MyCompany.Name} Energy: {MyCompany.TotalEnergy:F3}");
        }
    }

    public long CalculateUnixTime()
    {
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;
        long UnixTimestamp = currentTime.ToUnixTimeSeconds();
        return UnixTimestamp;
    }
}
