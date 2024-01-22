namespace Domain.Interfaces;

public interface ISettings
{
    public bool EnableSuperUserMode { get; }
    public int LogSaveDelay { get; }

    public int TasksCount { get; }
    public bool EnableLogAndResume { get; }
    public bool AttemptToFixIncorrectOffsets { get; }
    public bool ClearBackupFilesOnComplete { get; }
    public bool DeleteEmptyDirectoriesOnComplete { get; }
    public string Target { get; }
    public string[] Sources { get; }
    public string[] Ignores { get; }

    public string RootDirectory { get; }
    public string ToolsDirectory { get; }
    public string ParentTempDirectory { get; }
    public string TempLogPath { get; }
    public string TempExifPath { get; }
    public string TempZipPath { get; }
}
