using Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Application;
public sealed class Settings : ISettings
{
    #region Fields
    private readonly IConfiguration Configuration;
    #endregion

    #region Properties
    public bool EnableSuperUserMode { get; private init; }
    public int LogSaveDelay => 5_000;

    public int TasksCount { get; private init; }
    public bool EnableLogAndResume { get; private init; }
    public bool AttemptToFixIncorrectOffsets { get; private init; }
    public bool ClearBackupFilesOnComplete { get; private init; }
    public bool DeleteEmptyDirectoriesOnComplete { get; private init; }
    public bool AutoFixArabicNumbersInFileName { get; private init; }

    public string Target { get; private init; }
    public string[] Sources { get; private init; }
    public string[] Ignores { get; private init; }

    public string RootDirectory { get; private init; }
    public string ToolsDirectory { get; private init; }
    public string ParentTempDirectory { get; private init; }
    public string TempLogPath { get; private init; }
    public string TempExifPath { get; private init; }
    public string TempZipPath { get; private init; }
    #endregion

    #region Constructors
    public Settings(IConfiguration configuration, string root)
    {
        RootDirectory = root;

        ToolsDirectory = Path.Combine(RootDirectory, "Tools");
        ParentTempDirectory = Path.Combine(RootDirectory, ".temp");
        TempLogPath = Path.Combine(ParentTempDirectory, "logs");
        TempExifPath = Path.Combine(ParentTempDirectory, ".exif");
        TempZipPath = Path.Combine(ParentTempDirectory, ".zip");

        Configuration = configuration;
        EnableSuperUserMode = Configuration.GetValue<bool>(nameof(EnableSuperUserMode));

        TasksCount = Configuration.GetValue<int>(nameof(TasksCount));
        EnableLogAndResume = Configuration.GetValue<bool>(nameof(EnableLogAndResume));
        AttemptToFixIncorrectOffsets = Configuration.GetValue<bool>(nameof(AttemptToFixIncorrectOffsets));
        ClearBackupFilesOnComplete = Configuration.GetValue<bool>(nameof(ClearBackupFilesOnComplete));
        DeleteEmptyDirectoriesOnComplete = Configuration.GetValue<bool>(nameof(DeleteEmptyDirectoriesOnComplete));
        AutoFixArabicNumbersInFileName = Configuration.GetValue<bool>(nameof(AutoFixArabicNumbersInFileName));

        Target = Configuration.GetValue<string>(nameof(Target)) ?? string.Empty;
        Sources = GetSectionValues(Configuration, nameof(Sources));
        Ignores = GetSectionValues(Configuration, nameof(Ignores));
        Ignores = FixUnEscapedChars(Ignores);
    }
    #endregion

    #region Behavior
    private static string[] GetSectionValues(IConfiguration configuration, string sectionName)
    {
        var section = configuration.GetSection(sectionName);

        return section is null
            ? Array.Empty<string>()
            : section.GetChildren()
                .Select(i => i.Value)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToArray() as string[];
    }
    private static string[] FixUnEscapedChars(string[] items)
    {
        const string c1 = "\\";
        var c2 = $"{c1}{c1}";
        var comp = StringComparison.Ordinal;

        for (var i = 0; i < items.Length; i++)
            while (items[i].Contains(c2, comp))
                items[i] = items[i].Replace(c2, c1, comp);

        return items;
    }
    #endregion
}
