using Coreflux.API.Networking.MQTT;
using Tomlyn;
using Tomlyn.Model;
using Classes;
using AutomatismWorker.ClassesAuto;
using Newtonsoft.Json;

namespace AutomatismWorker;
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    public Info GeneralInfo {get; set;} = new Info();
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        LoadConfiguration("../ProjectShelly/config.toml");
        MQTTController.OnConnect += onMqttConnect; //connect to Topic
        MQTTController.NewPayload += onMqttNewPayload; // Receiving new info
        MQTTController.StartAsync("209.38.44.94", 1883).Wait();
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

        var model = Toml.ToModel(toml);
        if (model.TryGetValue("company", out var companyObj) && companyObj is TomlTable companyTable)
        {
            if (companyTable.TryGetValue("topic", out var topicObj) && topicObj is string topic)
            {
                GeneralInfo.MainTopic = topic;
            }
        }
    }

    private void onMqttConnect()
    {
        MQTTController.GetDataAsync(GeneralInfo.MainTopic);
        _logger.LogInformation($"Subscribed to topic: {GeneralInfo.MainTopic} at {DateTimeOffset.Now}");
    }

    private void onMqttNewPayload(MQTTNewPayload message)
    {
        try
        {
            if (ValidateDeviceDetails(message.topic))
            {
                Console.WriteLine($"NEW TOPIC RECEIVED: {message.topic}");
                var msg = JsonConvert.DeserializeObject<DeviceDetails>(message.payload);
                /* if (msg != null)
                    ProcessDeviceDetails(msg, message.topic); */
                Console.WriteLine($"MSG === {msg.Output}");
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

    public bool ValidateDeviceDetails(string topic)
    {
        var topicSegments = topic.Split('/');

        if (topicSegments.Length == 6 && topicSegments[5] == "switch:0")
            return true;
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                Console.WriteLine($"aquiiiiiiiii: {GeneralInfo.MainTopic}");
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
