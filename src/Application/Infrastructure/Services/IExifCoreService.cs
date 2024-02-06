namespace Application.Infrastructure.Services;

public interface IExifCoreService
{
    public string ToolVersion { get; }
    public string WatcherVersion { get; }

    string ClearBackupFiles(params string[] sources);

    bool IsSupportedMediaFile(FileInfo file);
    bool IsSupportedMediaFile(string fileName);

    Dictionary<string, string> FilterValues(Dictionary<string, string> tags);

    bool TryUpdateCreationDateTagsWithMinAcceptableValue(ref Dictionary<string, string> tags, DateTime[] others, out DateTime min);

    string DateTimeFormat(DateTime dateTime);
}
