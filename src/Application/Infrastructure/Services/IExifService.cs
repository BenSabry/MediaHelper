namespace Application.Infrastructure.Services;
public interface IExifService : IDisposable
{
    public string ExifToolVersion { get; }
    public string ExifWatcherVersion { get; }

    string ClearBackupFiles(params string[] sources);

    bool IsSupportedMediaFile(FileInfo file);
    bool IsSupportedMediaFile(string fileName);

    DateTime[] ReadAllDates(string path);
    DateTime[] ReadAllDatesFromJson(FileInfo jsonFile);

    bool TryUpdateMediaTargetedDateTime(string path, DateTime dateTime);
}
