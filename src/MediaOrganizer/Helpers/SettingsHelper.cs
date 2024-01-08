using Microsoft.Extensions.Configuration;
using MediaOrganizer.Core;

namespace MediaOrganizer.Helpers;
public static class SettingsHelper
{
    public static Settings GetSettings()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(CommonHelper.BaseDirectory, "AppSettings.json"))
            .Build();

        var temp = new Settings(config);
        config.Bind(temp);

        return temp;
    }
}
