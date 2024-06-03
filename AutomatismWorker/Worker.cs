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
        MQTTController.StartAsync("iot.coreflux.cloud", 1883).Wait();
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
                if (IsValidTime() && IsDeviceOn(message))
                    TurnOffDevice(message.topic);
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
            _logger.LogError($"TOPIC: {message.topic}   Unhandled exception: {ex.Message}");
        }
    }

    private bool ValidateDeviceDetails(string topic)
    {
        var topicSegments = topic.Split('/');

        if (topicSegments.Length == 6 && topicSegments[5] == "switch:0" && topicSegments[4] == "status")
            return true;
        return false;
    }

    private bool IsValidTime()
    {
        var currentTime = DateTime.Now.TimeOfDay;
        var start = new TimeSpan(20, 30, 0);
        var end = new TimeSpan(08, 30, 0);

        if (start < end)
            return currentTime >= start && currentTime <= end;
        else
            return currentTime >= start || currentTime <= end;
    }

    private bool IsDeviceOn(MQTTNewPayload message)
    {
        var payload = JsonConvert.DeserializeObject<DeviceDetails>(message.payload);
        if (payload != null && payload.Output == true)
            return true;
        return false;
    }

    public void TurnOffDevice(string topic)
    {
            if(topic.Contains("/status/"))
            {
                string actionTopic = topic.Replace("/status/", "/command/");
                MQTTController.SetDataAsync(actionTopic, "off");
                _logger.LogInformation($"Sent 'off' command to topic: {actionTopic} at {DateTimeOffset.Now}");
            }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(60000, stoppingToken);
        }
    }
}
