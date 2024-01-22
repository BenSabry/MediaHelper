using Application;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CLI;

public static class DependencyInjection
{
    public static IServiceCollection AddSettings(this IServiceCollection services)
    {
        services.AddSingleton<ISettings>(BuildSettingsInstance());

        return services;
    }

    private static Settings BuildSettingsInstance()
    {
        var root = AppContext.BaseDirectory;
        var path = Path.Combine(root, "AppSettings.json");
        FixUnEscapedCharsInAppSettingsFile(path);

        var config = new ConfigurationBuilder()
            .AddJsonFile(path)
            .Build();

        return new Settings(config, root);
    }
    private static void FixUnEscapedCharsInAppSettingsFile(string path)
    {
        if (File.Exists(path))
        {
            const string dq = "\"";
            const string c1 = "\\";
            var c2 = $"{c1}{c1}";
            var c4 = $"{c2}{c2}";
            var c8 = $"{c4}{c4}";

            var lines = File.ReadAllLines(path);
            for (var lIndex = 0; lIndex < lines.Length; lIndex++)
                if (lines[lIndex].Contains(dq))
                {
                    var parts = lines[lIndex].Split(dq);
                    for (var pIndex = 0; pIndex < parts.Length; pIndex++)
                        if (parts[pIndex].Contains(c1) && !parts[pIndex].Contains(c2)) parts[pIndex] = parts[pIndex].Replace(c1, c2);
                        else if (!parts[pIndex].StartsWith(c4) && parts[pIndex].StartsWith(c2)) parts[pIndex] = parts[pIndex].Replace(c1, c2);
                        else if (parts[pIndex].StartsWith(c8)) parts[pIndex] = parts[pIndex].Replace(c2, c1);

                    lines[lIndex] = string.Join(dq, parts);
                }

            File.WriteAllLines(path, lines);
        }
    }
}
