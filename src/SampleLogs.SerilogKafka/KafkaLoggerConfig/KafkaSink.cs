using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;
using Confluent.Kafka;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Sinks.PeriodicBatching;

namespace SampleLogs.SerilogKafka.KafkaLoggerConfig;

public class KafkaSink : IBatchedLogEventSink
{
    private const int FlushTimeoutSecs = 10;

    private readonly TopicPartition _globalTopicPartition;
    private readonly ITextFormatter _formatter;
    private readonly Func<LogEvent, string> _topicDecider;
    private IProducer<Null, byte[]> _producer;

    public KafkaSink(
        string bootstrapServers,
        SecurityProtocol securityProtocol,
        SaslMechanism saslMechanism,
        string saslUsername,
        string saslPassword,
        string sslCaLocation,
        string? topic = null,
        Func<LogEvent, string>? topicDecider = null,
        ITextFormatter? formatter = null)
    {
        ConfigureKafkaConnection(
            bootstrapServers,
            securityProtocol,
            saslMechanism,
            saslUsername,
            saslPassword,
            sslCaLocation);

        _formatter = formatter ?? new JsonFormatter(renderMessage: true);

        if (topic != null)
            _globalTopicPartition = new TopicPartition(topic, Partition.Any);

        if (topicDecider != null)
            _topicDecider = topicDecider;
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        foreach (var logEvent in batch)
        {
            Message<Null, byte[]> message;

            var topicPartition = _topicDecider == null
                ? _globalTopicPartition
                : new TopicPartition(_topicDecider(logEvent), Partition.Any);

            using (var render = new StringWriter(CultureInfo.InvariantCulture))
            {
                _formatter.Format(logEvent, render);

                message = new Message<Null, byte[]>
                {
                    Value = Encoding.UTF8.GetBytes(render.ToString())
                };
            }

            _producer.Produce(topicPartition, message);
        }

        _producer.Flush(TimeSpan.FromSeconds(FlushTimeoutSecs));

        return Task.CompletedTask;
    }

    private void ConfigureKafkaConnection(
        string bootstrapServers,
        SecurityProtocol securityProtocol,
        SaslMechanism saslMechanism,
        string saslUsername,
        string saslPassword,
        string sslCaLocation)
    {
        var config = new ProducerConfig()
            {
                BootstrapServers = bootstrapServers
            }
            .SetValue("ApiVersionFallbackMs", 0)
            .SetValue("EnableDeliveryReports", false)
            .LoadFromEnvironmentVariables()
            .SetValue("SecurityProtocol", securityProtocol)
            .SetValue("SaslMechanism", saslMechanism)
            .SetValue("SslCaLocation",
                string.IsNullOrEmpty(sslCaLocation)
                    ? null
                    : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), sslCaLocation))
            .SetValue("SaslUsername", saslUsername)
            .SetValue("SaslPassword", saslPassword);

        _producer = new ProducerBuilder<Null, byte[]>(config)
            .Build();
    }
}