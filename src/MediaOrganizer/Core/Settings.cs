using Microsoft.Extensions.Configuration;

namespace MediaOrganizer.Core;
public sealed class Settings
{
    #region Fields
    private readonly IConfiguration Configuration;
    #endregion

    #region Properties
    public bool EnableSuperUserMode { get; set; }

    public int TasksCount { get; set; }
    public bool EnableLogAndResume { get; set; }
    public bool AttemptToFixIncorrectOffsets { get; set; }
    public bool ClearBackupFilesOnComplete { get; set; }
    public bool DeleteEmptyDirectoriesOnComplete { get; set; }
    public string Target { get; set; }
    public string?[] Sources { get; set; }
    public string?[] Ignores { get; set; }
    #endregion

    #region Constructors
    public Settings(IConfiguration configuration)
    {
        Configuration = configuration;

        TasksCount = Configuration.GetValue<int>(nameof(TasksCount));
        EnableLogAndResume = Configuration.GetValue<bool>(nameof(EnableLogAndResume));
        AttemptToFixIncorrectOffsets = Configuration.GetValue<bool>(nameof(AttemptToFixIncorrectOffsets));
        ClearBackupFilesOnComplete = Configuration.GetValue<bool>(nameof(ClearBackupFilesOnComplete));
        DeleteEmptyDirectoriesOnComplete = Configuration.GetValue<bool>(nameof(DeleteEmptyDirectoriesOnComplete));

        EnableSuperUserMode = Configuration.GetValue<bool>(nameof(EnableSuperUserMode));
        Target = Configuration.GetValue<string>(nameof(Target)) ?? string.Empty;

        Sources = GetSectionValues(Configuration, nameof(Sources));
        Ignores = GetSectionValues(Configuration, nameof(Ignores));
    }

    private static string[] GetSectionValues(IConfiguration configuration, string sectionName)
    {
        return configuration.GetSection(sectionName)
            .GetChildren()
            .Select(i => i.Value)
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .ToArray();
    }
    #endregion
}

