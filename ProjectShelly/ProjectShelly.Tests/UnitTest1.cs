using App.WorkerService;

namespace ProjectShelly.Tests;


public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        // Arrange
        string topic = "coreflux/porto/meetingRoom/lamp/test/switch:0";
        
        // Act
        bool result = ValidateDeviceDetails(topic);
        // Assert


        Assert.True(result);


    }

    public bool ValidateDeviceDetails(string topic)
    {
        var topicSegments = topic.Split('/');

        if (topicSegments.Length == 6 && topicSegments[5] == "switch:0")
            return true;
        return false;
    }
}