using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace SampleLogs.NLogKafka.NLogConfig;

[Target("Kafka")]
internal sealed class KafkaTarget : Target
{
    public KafkaTarget()
    { }

    [RequiredParameter]
    public Layout Topic { get; set; } = null!;
    
    [RequiredParameter]
    public string BootstrapServers { get; set; } = null!;
    
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
    
    private static readonly JsonSerializer SecureSerializer = new JsonSerializer
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Include
    };

    protected override void Write(LogEventInfo logEvent)
    {
        try
        {
            var onlyTesting = RenderLogEvent(Test, logEvent);
            var bootstrapServers = RenderLogEvent(BootstrapServers, logEvent);
            var topic = Topic.Render(logEvent);
            KafkaProducerSingleton.Init(bootstrapServers, topic);

            var properties = logEvent.Properties;
            
            var json = new JObject();

            foreach (var propertie in properties)
            {
                var key =  propertie.Key?.ToString()?.ToLowerInvariant() ?? "null";
                
                if (propertie.Value is null)
                {
                    json[key] = null;
                    continue;
                }

                var value = propertie.Value switch
                {
                    Exception ex => new { ex.Message, ex.GetType().Name, ex.StackTrace },
                    Type => propertie.Value.ToString(),
                    System.Reflection.MemberInfo => null,
                    System.Reflection.Assembly => null,
                    RouteEndpoint => null,
                    _ => propertie.Value
                };
                
                if (value != null)
                {
                    json[key] = JToken.FromObject(value, SecureSerializer);
                }
            }

            KafkaProducerSingleton.Instance.Produce(json.ToString(Formatting.None));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}

internal sealed class KafkaProducerSingleton
{
    private static KafkaProducerSingleton? _instance; 
    
    public static KafkaProducerSingleton Instance
    {
        get
        {
            if (_instance is null)
                throw new InvalidOperationException("KafkaProducerSingleton not initialized.");
            
            return _instance;
        }
    }
    
    private IProducer<Null, byte[]>? _producer;
    private TopicPartition? _topicPartition;
    
    private static readonly object _lock = new();
    private bool _initialized;
    
    public static void Init(string bootstrapServers, string topic)
    {
        lock (_lock)
        {
            if (_instance is not null)
                return;

            var instancce = new KafkaProducerSingleton();

            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                // Adicione mais configs se quiser
            };
            
            var producerConfig = new ProducerConfig()
            {
                BootstrapServers = bootstrapServers
            };
            instancce._producer = new ProducerBuilder<Null, byte[]>(producerConfig).Build();
            instancce._topicPartition = new TopicPartition(topic, Partition.Any);
            instancce._initialized = true;

            _instance = instancce;
        }
    }

    public void Produce(string log)
    { 
        if(!_initialized)
            throw new InvalidOperationException("KafkaProducerSingleton não foi inicializado.");
        
        var message = new Message<Null, byte[]>
        {
            Value = Encoding.UTF8.GetBytes(log)
        };

        _producer!.Produce(_topicPartition, message);
        
        _producer.Flush(TimeSpan.FromSeconds(10));
    }
}