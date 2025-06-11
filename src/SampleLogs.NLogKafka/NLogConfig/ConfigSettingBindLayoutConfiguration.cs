using System.Text;
using System.Text.Json;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.LayoutRenderers;

namespace SampleLogs.NLogKafka.NLogConfig;

[LayoutRenderer("configSettingData")]
[ThreadAgnostic]
public class ConfigSettingBindLayoutConfiguration : LayoutRenderer
{
    private IConfiguration? _configuration;
    public static IConfiguration DefaultConfiguration { get; set; }

    [RequiredParameter]
    [DefaultParameter]
    public string Item { get; set; }

    protected override void InitializeLayoutRenderer()
    {
        try
        {
            if (ResolveService<IServiceProvider>() != (LoggingConfiguration?.LogFactory?.ServiceRepository ??
                                                       LogManager.LogFactory.ServiceRepository))
            {
                _configuration = ResolveService<IConfiguration>();
            }
        }
        catch(NLogConfigurationException ex)
        {
            InternalLogger.Debug("ConfigSetting =  Fallback to DefaultConfiguration: {0}", ex.Message);
        }
        base.InitializeLayoutRenderer();
        
    }

    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        if (string.IsNullOrEmpty(Item))
            return;

        string? text = null;
        var configuration = _configuration ?? DefaultConfiguration;

        if (configuration != null)
        {
            var configs = new LoggingConfig();
            
            configuration.GetSection(Item).Bind(configs);

            text = JsonSerializer.Serialize(configs);
        }
        else
        {
            InternalLogger.Debug("Missing DefaultConfiguration. Not config in appSettings.json");
        }

        builder.Append(text);
    }
}

public class LoggingConfig
{
    public string Test { get; set; }
}