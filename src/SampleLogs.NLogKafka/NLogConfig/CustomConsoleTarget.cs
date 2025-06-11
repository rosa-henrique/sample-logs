using Confluent.Kafka;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace SampleLogs.NLogKafka.NLogConfig;

[Target("CustomConsole")]
public class CustomConsoleTarget : Target
{
    private IProducer<Null, byte[]> _producer;

    public CustomConsoleTarget()
    { }

    [RequiredParameter]
    public Layout? Topic { get; set; }
    
    [RequiredParameter]
    public string? BootstrapServers { get; set; }
    
    [RequiredParameter]
    public string? Hostname { get; set; }
    
    [RequiredParameter]
    public string? Acronyms { get; set; }
    
    [RequiredParameter]
    public string? Application { get; set; }
    
    [RequiredParameter]
    public string? AspNetEnvironment { get; set; }
    
    [RequiredParameter]
    public Layout? RequestId { get; set; }

    public string? Test { get; set; }
    

    protected override void Write(LogEventInfo logEvent)
    {
        var onlyTesting = RenderLogEvent(Test, logEvent);
    }
}