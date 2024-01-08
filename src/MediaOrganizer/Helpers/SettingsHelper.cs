using Microsoft.Extensions.Configuration;
using MediaOrganizer.Core;

namespace MediaOrganizer.Helpers;
public static class SettingsHelper
{
    public static string AppSettingsPath => Path.Combine(CommonHelper.BaseDirectory, "AppSettings.json");
    public static Settings GetSettings()
    {
        FixUnEscapedCharsInAppSettingsFile();

        var config = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsPath)
            .Build();

        var temp = new Settings(config);
        config.Bind(temp);

        return temp;
    }
    private static void FixUnEscapedCharsInAppSettingsFile()
    {
        const string one = "\\";
        const string two = "\\\\";

        if (File.Exists(AppSettingsPath))
            File.WriteAllText(AppSettingsPath,
                File.ReadAllText(AppSettingsPath)
                    .Replace(two, one)
                    .Replace(one, two));
    }
}
