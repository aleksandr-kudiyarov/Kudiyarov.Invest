namespace Kudiyarov.Invest.Extensions;

public static class ConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string key)
    {
        var options = configuration
            .GetSection(key)
            .Get<T>();

        ArgumentNullException.ThrowIfNull(options);

        return options;
    }
}