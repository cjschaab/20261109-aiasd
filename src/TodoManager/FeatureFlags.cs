using Microsoft.Extensions.Configuration;

namespace TodoManager;

public interface IFeatureFlags
{
    bool IsEnabled(string featureName);
}

public sealed class ConfigurationFeatureFlags(IConfiguration configuration) : IFeatureFlags
{
    public bool IsEnabled(string featureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        return configuration.GetValue<bool>($"FeatureFlags:{featureName}");
    }
}
